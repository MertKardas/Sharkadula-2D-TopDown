using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using VContainer;
using VContainer.Unity;

public static class IdSaveRegressionChecks {
    public static string Contract() {
        Check(typeof(GameSaveData).GetField("Entries") == null, "The generic Entries wrapper must be removed.");
        Check(typeof(GameSaveData).GetField("Objects") != null, "One ID table must hold all records.");
        Check(typeof(SaveableEntity).GetProperty("IsRemoved")?.PropertyType == typeof(bool), "One entity needs an explicit removal flag.");
        Check(typeof(SaveableEntity).GetMethod("MarkRemoved") != null, "Removal must be explicit before Destroy.");
        Check(typeof(DynamicObjectSpawner).GetProperty("SaveId") != null, "The existing spawner must own ID membership.");
        Check(typeof(SceneSaveContext).GetMethod("Collect") != null, "Collect must be separate from disk Save.");
        Check(typeof(SceneSaveContext).GetMethod("ChangeScene") != null, "Scene transitions must collect before unloading.");
        Check(typeof(GameSaveService).Assembly.GetType("DynamicSaveableEntity") == null, "Static and dynamic objects share one entity.");
        return "PASS: one ID table, one entity, explicit removal and collect contracts.";
    }

    public static async Task<string> Lifecycle() {
        Check(Application.isPlaying, "Run in Play Mode.");
        await Ready();
        var game = Scope().Container.Resolve<GameService>();
        game.StartNewGame("Episode(01)");
        await Ready("Episode(01)");
        var saves = Scope().Container.Resolve<GameSaveService>();
        var oldStorage = StorageField.GetValue(saves);
        var oldData = Session(saves);
        var folder = Path.GetFullPath(Path.Combine("Temp/IdSaveChecks", Guid.NewGuid().ToString("N")));
        var disk = new EasySaveStorage(folder);
        GameObject pending = null;
        try {
            StorageField.SetValue(saves, disk);
            var context = Scope().Container.Resolve<SceneSaveContext>();
            var spawner = Scope().Container.Resolve<DynamicObjectSpawner>();
            var item = AssetDatabase.FindAssets("t:ItemData").Select(g => AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(g)))
                .First(i => Scope().Container.Resolve<ItemCatalog>().GetDropPrefab(i.ID) != null);
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            player.transform.position = new Vector3(10002, 10002, 0);
            player.Inventory.SetCoin(51);
            var chest = UnityEngine.Object.FindAnyObjectByType<Chest>();
            var staticId = chest.GetComponent<SaveableEntity>().UniqueId;
            var savedDoor = UnityEngine.Object.FindAnyObjectByType<Door>();
            var doorId = savedDoor.GetComponent<SaveableEntity>().UniqueId;
            ((ISavable)savedDoor).RestoreState(new DoorSaveData { IsLocked = false });
            ((ISavable)chest).RestoreState(new ChestSaveData { isOpen = true });

            var drop = spawner.Create(item.ID, new Vector3(10003, 10003, 0));
            var dynamicId = drop.UniqueId;
            drop.GetComponent<DropObject>().Initialize(item, 3);
            await Task.Delay(150);
            drop.GetComponent<DropObject>().OnItemsCollected(1);
            var collect = context.Collect();
            Check(collect.Success, "Collect must succeed: " + collect.ErrorMessage);
            Check(!Directory.Exists(folder), "Collect must not write to disk.");
            Check(Membership(saves, spawner.SaveId).Contains(dynamicId), "Existing spawner must save ID membership.");

            var removed = spawner.Create(item.ID, new Vector3(10004, 10004, 0));
            await Task.Delay(100);
            var removedId = removed.UniqueId;
            Check(context.Collect().Success, "Removal fixture must collect first.");
            removed.GetComponent<DropObject>().OnItemsCollected(1);
            UnityEngine.Object.Destroy(removed.gameObject);
            var before = Session(saves);
            StorageField.SetValue(saves, new FailingStorage());
            Check(!context.QuickSave().Success && ReferenceEquals(before, Session(saves)), "Failed write must retain the old session.");
            Check(Session(saves).Objects.ContainsKey(removedId), "Failed write must not apply deletion.");
            await Task.Delay(100);
            StorageField.SetValue(saves, disk);
            var retry = context.Collect();
            Check(retry.Success && !Session(saves).Objects.ContainsKey(removedId)
                && !Membership(saves, spawner.SaveId).Contains(removedId), "Removal must survive Destroy and failed Save: " + retry.ErrorMessage);

            var sameFrame = spawner.Create(item.ID, new Vector3(10004, 10004, 0));
            var sameFrameId = sameFrame.UniqueId;
            sameFrame.MarkRemoved();
            UnityEngine.Object.Destroy(sameFrame.gameObject);
            Check(context.Collect().Success && !Membership(saves, spawner.SaveId).Contains(sameFrameId), "Same-frame marked removal must not wait for Destroy.");
            chest.GetComponent<SaveableEntity>().MarkRemoved();
            UnityEngine.Object.Destroy(chest.gameObject);
            Check(context.Collect().Success && Session(saves).Objects[staticId].IsRemoved, "Destroyed static entity needs a removal marker.");
            await Task.Delay(100);
            Check(context.Collect().Success && Session(saves).Objects[staticId].IsRemoved, "Static removal marker must survive another collect.");

            var source = UnityEngine.Object.FindObjectsByType<Door>(FindObjectsInactive.Include).First();
            pending = Scope().Container.Instantiate(source.gameObject, new Vector3(10010, 10010), Quaternion.identity, null);
            pending.SetActive(false);
            var pendingEntity = pending.GetComponent<SaveableEntity>();
            pendingEntity.SetUniqueId("pending-" + Guid.NewGuid().ToString("N"));
            Session(saves).Objects[pendingEntity.UniqueId] = new EntitySaveData { State = new DoorSaveData { IsLocked = false } };
            Check(!pendingEntity.HasStarted && context.Collect().Success
                && Session(saves).Objects[pendingEntity.UniqueId].State is DoorSaveData door && !door.IsLocked, "Pending inactive data must survive collect.");
            pendingEntity.MarkRemoved();
            UnityEngine.Object.Destroy(pending);
            await Task.Delay(100);

            var unknown = spawner.Create(item.ID, new Vector3(10011, 10011, 0));
            await Task.Delay(100);
            var unknownId = unknown.UniqueId;
            UnityEngine.Object.Destroy(unknown.gameObject);
            await Task.Delay(100);
            before = Session(saves);
            Check(!context.Collect().Success && ReferenceEquals(before, Session(saves)), "Unreported Destroy must fail instead of silently deleting data.");
            var failedTransition = context.ChangeScene("_IdSaveRegressionB");
            Check(!failedTransition.Success && SceneManager.GetActiveScene().name == "Episode(01)" && Time.timeScale == 1f,
                "Failed collect must leave the source scene and game time usable.");
            // Test-only recovery: the production API requires MarkRemoved before Destroy.
            var removedMap = (Dictionary<string, bool>)typeof(SceneSaveContext).GetField("_removed", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(context);
            removedMap[unknownId] = true;
            Check(context.Collect().Success, "Explicit removal recovery must collect.");

            var transition = context.ChangeScene("_IdSaveRegressionB");
            Check(transition.Success, "A to B transition must start: " + transition.ErrorMessage);
            await Ready("_IdSaveRegressionB");
            player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            Check(player.Inventory.Coin == 51 && Vector2.Distance(player.transform.position, new Vector2(10000, 10000)) < 0.1f,
                "Normal transition must carry player progress and use target entrance position.");
            Check(!UnityEngine.Object.FindObjectsByType<SaveableEntity>(FindObjectsInactive.Include).Any(e => e.UniqueId == dynamicId), "A's dynamic object must not spawn in B.");
            var bSpawner = Scope().Container.Resolve<DynamicObjectSpawner>();
            var bDrop = bSpawner.Create(item.ID, new Vector3(10020, 10020, 0));
            var bId = bDrop.UniqueId;
            await Task.Delay(100);
            player.Inventory.SetCoin(82);
            Check(Scope().Container.Resolve<SceneSaveContext>().ChangeScene("Episode(01)").Success, "B to A transition must start.");
            await Ready("Episode(01)");
            player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            Check(player.Inventory.Coin == 82, "Shared player progress must not rewind on returning to A.");
            var restored = UnityEngine.Object.FindObjectsByType<SaveableEntity>(FindObjectsInactive.Include).Single(e => e.UniqueId == dynamicId);
            Check(restored.GetComponent<DropObject>().Collect(player).quantity == 2 && restored.gameObject.scene == Scope().gameObject.scene, "A partially collected dynamic object must restore quantity, ID and scene ownership.");
            Check(!UnityEngine.Object.FindObjectsByType<SaveableEntity>(FindObjectsInactive.Include).Single(e => e.UniqueId == staticId).gameObject.activeSelf,
                "Removed static object must remain inactive on A return.");
            var removedStatic = UnityEngine.Object.FindObjectsByType<SaveableEntity>(FindObjectsInactive.Include).Single(e => e.UniqueId == staticId);
            removedStatic.gameObject.SetActive(true);
            Check(!removedStatic.gameObject.activeSelf, "A removed static entity must not be reactivated.");
            Check(((DoorSaveData)((ISavable)UnityEngine.Object.FindObjectsByType<Door>(FindObjectsInactive.Include).Single(d => d.GetComponent<SaveableEntity>().UniqueId == doorId)).CaptureState()).IsLocked == false,
                "A's unlocked door must survive A-B-A independently of removal markers.");
            var savedPosition = new Vector3(10030, 10030, 0);
            player.transform.position = savedPosition;
            var save = Scope().Container.Resolve<SceneSaveContext>().QuickSave();
            Check(save.Success, "F5-equivalent save must write: " + save.ErrorMessage);
            Check(disk.Load("quickSave", out var loaded).Success && loaded.Objects.ContainsKey(bId), "Disk save must preserve B's records too.");
            player.Inventory.SetCoin(999);
            var oldContext = Scope().Container.Resolve<SceneSaveContext>();
            game.RestartGame();
            Check(!oldContext.QuickSave().Success, "Outgoing context must not overwrite a disk snapshot after restart begins.");
            await Ready("Episode(01)");
            player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            Check(player.Inventory.Coin == 82 && Vector2.Distance(player.transform.position, savedPosition) < 0.1f,
                "Restart must restore the disk snapshot and exact saved position.");
            Check(Scope().Container.Resolve<SceneSaveContext>().ChangeScene("_IdSaveRegressionB").Success, "Reloaded session must transition to B.");
            await Ready("_IdSaveRegressionB");
            Check(UnityEngine.Object.FindObjectsByType<SaveableEntity>(FindObjectsInactive.Include).Any(e => e.UniqueId == bId), "B's dynamic object must survive disk reload.");
            game.ReturnToMainMenu();
            await Ready("MainMenu");
            return "PASS: collect-only memory, flag/Destroy timing, failed Save retry, static markers, inactive data, A-B-A, player transfer and disk restart.";
        } finally {
            StorageField.SetValue(saves, oldStorage);
            SessionField.SetValue(saves, oldData);
            if (pending != null) UnityEngine.Object.Destroy(pending);
            var root = Path.GetFullPath("Temp/IdSaveChecks") + Path.DirectorySeparatorChar;
            Check(folder.StartsWith(root, StringComparison.OrdinalIgnoreCase), "Cleanup must stay in the isolated test root.");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }

    public static string DelayedPlayerPolicy() {
        var saves = new GameSaveService(new FailingStorage(), new WorldSaveValidator(null));
        typeof(GameSaveService).GetMethod("BeginTransition", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(saves, null);
        var context = new SceneSaveContext(default, saves, null);
        typeof(GameSaveService).GetMethod("CompleteTransition", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(saves, new object[] { "Episode(01)" });
        Check((bool)typeof(SceneSaveContext).GetProperty("RestorePlayerAtEntrance", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(context),
            "The target context must retain transition policy for a player activated after startup.");
        return "PASS: delayed player transition policy remains scene-scoped.";
    }

    public static string ReservedPlayerRecord() {
        var data = new GameSaveData { ResumeSceneName = "Episode(01)" };
        data.Objects["valid-chest"] = new EntitySaveData { State = new ChestSaveData() };
        Check(new WorldSaveValidator(null).Validate(data).Success, "The baseline save must validate; run this check in Play Mode.");
        data.Objects[GameSaveService.PlayerId] = new EntitySaveData { State = new ChestSaveData() };
        Check(!new WorldSaveValidator(null).Validate(data).Success, "A reserved player ID must reject a known wrong state type before loading.");
        return "PASS: reserved player record rejects known wrong state types.";
    }

    public static string InvalidRecords() {
        Check(Application.isPlaying, "Run in Play Mode.");
        var catalog = Scope().Container.Resolve<ItemCatalog>();
        var validator = new WorldSaveValidator(catalog);
        var item = AssetDatabase.FindAssets("t:ItemData").Select(g => AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(g)))
            .First(i => catalog.GetDropPrefab(i.ID) != null);
        var folder = Path.GetFullPath(Path.Combine("Temp/IdSaveChecks", Guid.NewGuid().ToString("N")));
        try {
            var disk = new EasySaveStorage(folder);
            var saves = new GameSaveService(disk, validator);
            var data = new GameSaveData { ResumeSceneName = "Episode(01)" };
            data.Objects["spawner"] = new EntitySaveData { State = new SpawnerSaveData { DynamicIds = new List<string> { "drop" } } };
            data.Objects["drop"] = new EntitySaveData { PrefabId = item.ID, State = new DropObjectSaveData { itemID = item.ID, quantity = 1 } };
            Check(disk.Save(data, new MetaData("valid", "valid", DateTime.UtcNow, "test")).Success && saves.LoadGame("valid").Success, "The baseline dynamic record must load.");
            var before = Session(saves);
            Action<string> reject = name => {
                Check(disk.Save(data, new MetaData("invalid", name, DateTime.UtcNow, "test")).Success, "Invalid fixture must serialize.");
                Check(!saves.LoadGame("invalid").Success && ReferenceEquals(before, Session(saves)), name + " must fail without replacing the session.");
            };
            data.Objects[GameSaveService.PlayerId] = new EntitySaveData { State = new ChestSaveData() };
            reject("reserved player type");
            data.Objects.Remove(GameSaveService.PlayerId);
            data.Objects["other-spawner"] = new EntitySaveData { State = new SpawnerSaveData { DynamicIds = new List<string> { "drop" } } };
            reject("multiple owners");
            data.Objects.Remove("other-spawner");
            var membership = (SpawnerSaveData)data.Objects["spawner"].State;
            membership.DynamicIds.Clear();
            reject("unowned dynamic record");
            membership.DynamicIds.Add("missing");
            reject("missing dynamic record");
            membership.DynamicIds[0] = "drop";
            data.Objects["drop"].SpawnPosition = new Vector3(float.NaN, 0, 0);
            reject("non-finite spawn position");
            data.Objects["drop"].SpawnPosition = Vector3.zero;
            data.Objects["drop"].State = new ChestSaveData();
            reject("wrong dynamic state type");
            return "PASS: reserved IDs, duplicate owners, missing/unowned dynamic records, invalid position/type preserve the loaded session.";
        } finally {
            Check(folder.StartsWith(Path.GetFullPath("Temp/IdSaveChecks") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), "Cleanup stays in test root.");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }

    public static async Task<string> LoadingGuard() {
        await Ready();
        var game = Scope().Container.Resolve<GameService>();
        var saves = Scope().Container.Resolve<GameSaveService>();
        var context = Scope().Container.Resolve<SceneSaveContext>();
        var loading = typeof(GameService).GetField("_isLoadingScene", BindingFlags.Instance | BindingFlags.NonPublic);
        var oldStorage = StorageField.GetValue(saves);
        var oldData = Session(saves);
        try {
            StorageField.SetValue(saves, new SuccessfulStorage());
            loading.SetValue(game, true);
            Check(!context.QuickSave().Success && ReferenceEquals(oldData, Session(saves)), "An outgoing context must reject Save while any scene load is in progress.");
            Check(!context.Collect().Success, "An outgoing context must reject external Collect while loading.");
            return "PASS: outgoing Save and Collect are blocked during scene loading.";
        } finally {
            loading.SetValue(game, false);
            StorageField.SetValue(saves, oldStorage);
            SessionField.SetValue(saves, oldData);
        }
    }

    public static async Task<string> RemovedEntityReactivation() {
        await Ready();
        Scope().Container.Resolve<GameService>().StartNewGame("Episode(01)");
        await Ready("Episode(01)");
        var source = UnityEngine.Object.FindAnyObjectByType<Chest>();
        var obj = Scope().Container.Instantiate(source.gameObject, new Vector3(10000, 10000), Quaternion.identity, null);
        try {
            var entity = obj.GetComponent<SaveableEntity>();
            entity.SetUniqueId(Guid.NewGuid().ToString());
            entity.MarkRemoved();
            obj.SetActive(true);
            Check(!obj.activeSelf, "A removed entity must stay inactive even when re-enabled before its first Start.");
            return "PASS: removed entities cannot be reactivated.";
        } finally { UnityEngine.Object.Destroy(obj); }
    }

    private static readonly FieldInfo SessionField = typeof(GameSaveService).GetField("_currentData", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo StorageField = typeof(GameSaveService).GetField("_storage", BindingFlags.Instance | BindingFlags.NonPublic);
    private static GameSaveData Session(GameSaveService saves) => (GameSaveData)SessionField.GetValue(saves);
    private static SceneLifetimeScope Scope() => UnityEngine.Object.FindAnyObjectByType<SceneLifetimeScope>();
    private static List<string> Membership(GameSaveService saves, string id) => ((SpawnerSaveData)Session(saves).Objects[id].State).DynamicIds;

    private static async Task Ready(string scene = null) {
        await Task.Delay(200);
        var field = typeof(GameService).GetField("_isLoadingScene", BindingFlags.Instance | BindingFlags.NonPublic);
        for (int i = 0; i < 150; i++) {
            var scope = Scope();
            if (scope != null && scope.Container != null && (scene == null || SceneManager.GetActiveScene().name == scene)
                && !(bool)field.GetValue(scope.Container.Resolve<GameService>())) return;
            await Task.Delay(100);
        }
        throw new Exception("Timed out waiting for completed scene startup: " + scene);
    }

    private sealed class FailingStorage : ISaveStorage {
        public Result Save(GameSaveData data, MetaData metadata) => Result.Fail("Intentional write failure");
        public Result Load(string slotName, out GameSaveData data) { data = null; return Result.Fail("unused"); }
        public Result DeleteSave(string slotName) => Result.Fail("unused");
        public Result GetAllMetaData(out List<MetaData> metadata) { metadata = new(); return Result.Ok(); }
    }

    private sealed class SuccessfulStorage : ISaveStorage {
        public Result Save(GameSaveData data, MetaData metadata) => Result.Ok();
        public Result Load(string slotName, out GameSaveData data) { data = null; return Result.Fail("unused"); }
        public Result DeleteSave(string slotName) => Result.Fail("unused");
        public Result GetAllMetaData(out List<MetaData> metadata) { metadata = new(); return Result.Ok(); }
    }

    private static void Check(bool condition, string message) {
        if (!condition) throw new Exception(message);
    }
}
