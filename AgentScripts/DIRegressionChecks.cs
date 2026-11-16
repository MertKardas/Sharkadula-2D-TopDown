using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public static class DIRegressionChecks {
    public static string JsonConstructorIsDataOnly() {
        var model = new InventoryItem("__di_unknown_item__", 3);
        if (model.item != "__di_unknown_item__" || model.quantity != 3 || model.itemData != null)
            throw new Exception("InventoryItem JSON constructor must only store ID and quantity.");
        return "PASS: InventoryItem constructor stores data without global services.";
    }

    public static string CheckScene() {
        Check(Application.isPlaying, "Run scene checks in Play Mode.");
        var scope = UnityEngine.Object.FindAnyObjectByType<SceneLifetimeScope>();
        Check(scope != null && scope.Parent is AppLifetimeScope && scope.Container != null, "Scene scope must inherit the app scope.");
        Check(UnityEngine.Object.FindObjectsByType<AppLifetimeScope>().Length == 1, "Exactly one root must exist.");
        var services = new[] { typeof(GameService), typeof(InputService), typeof(AudioService), typeof(GameSaveService), typeof(ItemCatalog) };
        foreach (var type in services) {
            Check(!typeof(MonoBehaviour).IsAssignableFrom(type), "Service must be a plain C# class: " + type.Name);
            Check(ReferenceEquals(scope.Container.Resolve(type), scope.Parent.Container.Resolve(type)), "Scene must share root instance: " + type.Name);
        }
        foreach (var root in scope.gameObject.scene.GetRootGameObjects()) {
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true)) {
                Check(component != null, "Scene has a missing script.");
                foreach (var field in component.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                    if (services.Contains(field.FieldType))
                        Check(ReferenceEquals(field.GetValue(component), scope.Container.Resolve(field.FieldType)), "Missing/wrong injection: " + component.GetType().Name + "." + field.Name);
                }
            }
        }
        return "PASS: " + scope.gameObject.scene.name + " has one root, a child scene scope and injected services, including inactive objects.";
    }

    public static async Task<string> RunLifetimes() {
        CheckScene();
        var scene = UnityEngine.Object.FindAnyObjectByType<SceneLifetimeScope>();
        var sceneContext = scene.Container.Resolve<SceneSaveContext>();
        var sceneFactory = scene.Container.Resolve<IDynamicObjectFactory>();
        var root = scene.Parent;
        var resolver = root.Container;
        var game = resolver.Resolve<GameService>();
        var input = resolver.Resolve<InputService>();
        var audio = resolver.Resolve<AudioService>();
        var saves = resolver.Resolve<GameSaveService>();
        var catalog = resolver.Resolve<ItemCatalog>();
        game.StartNewGame("Episode(01)");
        await WaitForScene("Episode(01)");
        CheckScene();
        Check(scene == null, "Previous scene scope must be destroyed.");
        var next = UnityEngine.Object.FindAnyObjectByType<SceneLifetimeScope>();
        Check(!ReferenceEquals(next.Container.Resolve<SceneSaveContext>(), sceneContext)
            && !ReferenceEquals(next.Container.Resolve<IDynamicObjectFactory>(), sceneFactory),
            "Each scene must own a new save context and dynamic factory.");
        Check(next.Parent == root && ReferenceEquals(next.Container.Resolve<GameService>(), game), "GameService must survive a scene change.");
        Check(ReferenceEquals(next.Container.Resolve<InputService>(), input) && ReferenceEquals(next.Container.Resolve<AudioService>(), audio)
            && ReferenceEquals(next.Container.Resolve<GameSaveService>(), saves) && ReferenceEquals(next.Container.Resolve<ItemCatalog>(), catalog), "All global services must survive.");
        var sound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioData>("Assets/Audio/Audios/HoverSound.asset");
        var source = audio.PlaySound(sound);
        Check(source.transform.IsChildOf(root.transform), "Pooled sources belong to the root scope.");
        await Task.Delay((int)(source.clip.length / source.pitch * 1000f) + 250);
        Check(!source.gameObject.activeSelf, "AudioService ITickable must return completed sounds to its pool.");
        game.EndGame();
        await WaitForScene("EndGame");
        CheckScene();
        Check(UnityEngine.Object.FindAnyObjectByType<SceneLifetimeScope>().Parent == root, "EndGame must share the same root.");
        game.ReturnToMainMenu();
        await WaitForScene("MainMenu");
        CheckScene();
        return "PASS: global service identity across three scenes, scene-scope destruction, injected inactive objects and audio Tick cleanup.";
    }

    public static async Task<string> RunLegacySaveAndDynamicItem() {
        CheckScene();
        var scope = UnityEngine.Object.FindAnyObjectByType<SceneLifetimeScope>();
        var game = scope.Container.Resolve<GameService>();
        game.StartNewGame("Episode(01)");
        await WaitForScene("Episode(01)");
        scope = UnityEngine.Object.FindAnyObjectByType<SceneLifetimeScope>();
        var catalog = scope.Container.Resolve<ItemCatalog>();
        var saves = scope.Container.Resolve<GameSaveService>();
        var context = scope.Container.Resolve<SceneSaveContext>();
        var previousData = GetSessionDataForTest(saves);
        var spawner = scope.Container.Resolve<DynamicObjectSpawner>();
        var item = UnityEditor.AssetDatabase.FindAssets("t:ItemData")
            .Select(g => UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(UnityEditor.AssetDatabase.GUIDToAssetPath(g)))
            .First(i => i.Prefab != null && i.Prefab.GetComponent<DropObject>() != null);
        var settings = new SaveSystemSettings(null);
        string json = "{\"$type\":\"InventoryItem, Assembly-CSharp\",\"item\":\"" + item.ID + "\",\"quantity\":2}";
        var legacy = JsonConvert.DeserializeObject<InventoryItem>(json, settings.jsonSettings);
        Check(legacy.itemData == null, "Old JSON must deserialize without service lookup.");
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        var original = player.Inventory.GetInventory();
        GameObject spawned = null;
        GameObject restored = null;
        string id = "di-regression-" + Guid.NewGuid().ToString("N");
        try {
            player.Inventory.LoadInventory(new List<InventoryItem> { legacy });
            Check(legacy.itemData == null && ReferenceEquals(player.Inventory.GetInventory()[0].itemData, catalog.GetItem(item.ID)),
                "Live inventory must bind to the catalog without mutating loaded data.");
            spawned = scope.Container.Resolve<IDynamicObjectFactory>().Create(item.ID, id, new Vector3(10000f, 10000f)).gameObject;
            var drop = spawned.GetComponent<DropObject>();
            drop.Initialize(item, 2);

            CheckInjectedDrop(drop, scope.Container);
            await Task.Delay(100);
            var data = new GameSaveData { ResumeSceneName = "Episode(01)", Objects = CaptureSceneForTest(context) };
            var decoded = JsonConvert.DeserializeObject<GameSaveData>(JsonConvert.SerializeObject(data, settings.jsonSettings), settings.jsonSettings);
            Check(decoded.Objects.Values.Select(record => record.State).OfType<PlayerSaveData>().Any(), "Existing PlayerSaveData type identity must survive round-trip.");
            UnityEngine.Object.Destroy(spawned);
            await Task.Delay(100);
            var instances = (Dictionary<string, SaveableEntity>)typeof(DynamicObjectSpawner).GetField("_instances", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(spawner);
            instances.Remove(id);
            SetSessionDataForTest(saves, decoded);
            spawner.Spawn();
            spawner.Spawn();
            await Task.Delay(100);
            var entity = UnityEngine.Object.FindObjectsByType<SaveableEntity>(FindObjectsInactive.Include).Single(e => e.UniqueId == id);
            restored = entity.gameObject;
            Check(restored.scene == scope.gameObject.scene, "Restored world items must belong to the gameplay scene, not the persistent root.");
            CheckInjectedDrop(restored.GetComponent<DropObject>(), scope.Container);
            Check(restored.GetComponent<DropObject>().Collect(player).quantity == 2, "Restored dynamic item must keep its quantity.");
            Check(player.Inventory.GetInventory()[0].itemData == item, "Restored inventory must be usable.");
            Check(CaptureSceneForTest(context).ContainsKey(id), "Restored world items must participate in the next save.");
            game.ReturnToMainMenu();
            await WaitForScene("MainMenu");
            Check(restored == null, "Restored world items must be destroyed with the gameplay scene.");
        } finally {
            SetSessionDataForTest(saves, previousData);
            if (player != null) player.Inventory.LoadInventory(original);
            if (spawned != null) UnityEngine.Object.Destroy(spawned);
            if (restored != null) UnityEngine.Object.Destroy(restored);
        }
        return "PASS: legacy InventoryItem JSON, catalog rebinding, save type round-trip and injected dynamic item restore; no real files written.";
    }

    private static void CheckInjectedDrop(DropObject drop, IObjectResolver resolver) {
        foreach (var field in typeof(DropObject).GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            if (field.FieldType == typeof(AudioService) || field.FieldType == typeof(ItemCatalog))
                Check(ReferenceEquals(field.GetValue(drop), resolver.Resolve(field.FieldType)), "Dynamic drop injection: " + field.Name);
    }

    public static string RunInvalidSaveRegression() {
        Check(Application.isPlaying, "Run validation checks in Play Mode.");
        var resolver = UnityEngine.Object.FindAnyObjectByType<SceneLifetimeScope>().Container;
        string tempRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Temp/DIWork/Validation"));
        string folder = System.IO.Path.Combine(tempRoot, Guid.NewGuid().ToString("N"));
        try {
            var disk = new EasySaveStorage(folder);
            var coordinator = new GameSaveService(disk, resolver.Resolve<WorldSaveValidator>());
            var previous = new GameSaveData();
            SetSessionDataForTest(coordinator, previous);
            var data = new GameSaveData { ResumeSceneName = "Episode(01)" };
            data.Objects[GameSaveService.PlayerId] = new EntitySaveData { State = new PlayerSaveData { health = 10, maxHealth = 10, items = new List<InventoryItem> { new InventoryItem("__unknown_item__", 1) } } };
            Check(disk.Save(data, new MetaData("invalid", "DI test", DateTime.UtcNow, "test")).Success, "Validation fixture must save.");
            Check(!coordinator.LoadGame("invalid").Success && ReferenceEquals(GetSessionDataForTest(coordinator), previous), "Unknown items must fail without replacing current data.");
            data.Objects[GameSaveService.PlayerId] = null;
            Check(disk.Save(data, new MetaData("invalid", "DI test", DateTime.UtcNow, "test")).Success, "Invalid scene fixture must save.");
            Check(!coordinator.LoadGame("invalid").Success && ReferenceEquals(GetSessionDataForTest(coordinator), previous), "Invalid scene data must fail without replacing current data.");
        } finally {
            string resolved = System.IO.Path.GetFullPath(folder);
            if (resolved.StartsWith(tempRoot + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && System.IO.Directory.Exists(resolved))
                System.IO.Directory.Delete(resolved, true);
        }
        return "PASS: unknown item and invalid scene data return failure without overwriting loaded game; real saves untouched.";
    }

    private static async Task WaitForScene(string name) {
        for (int i = 0; i < 100; i++) {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == name) {
                await Task.Delay(150);
                return;
            }
            await Task.Delay(100);
        }
        throw new Exception("DI regression: scene transition timeout: " + name);
    }

    private static void Check(bool condition, string message) {
        if (!condition) throw new Exception("DI regression: " + message);
    }

    // Fixtures inspect the private session; production callers use LoadGame/Clear.
    private static GameSaveData GetSessionDataForTest(GameSaveService saves) {
        return (GameSaveData)typeof(GameSaveService).GetField("_currentData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(saves);
    }

    private static void SetSessionDataForTest(GameSaveService saves, GameSaveData data) {
        typeof(GameSaveService).GetField("_currentData", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(saves, data);
    }

    private static Dictionary<string, EntitySaveData> CaptureSceneForTest(SceneSaveContext context) {
        return (Dictionary<string, EntitySaveData>)typeof(SceneSaveContext).GetMethod("Capture", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(context, null);
    }
}
