using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

public static class SaveReviewRegressionChecks {
    private static void Check(bool condition, string message) {
        if (!condition) throw new Exception(message);
    }

    private static async Task<PlayerController> StartGame() {
        Check(Application.isPlaying, "Run in Play Mode.");
        var scope = UnityEngine.Object.FindAnyObjectByType<SceneLifetimeScope>();
        var game = scope.Container.Resolve<GameService>();
        var loading = typeof(GameService).GetField("_isLoadingScene", BindingFlags.Instance | BindingFlags.NonPublic);
        await WaitUntil(() => !(bool)loading.GetValue(game));
        var previous = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        game.StartNewGame("Episode(01)");
        await WaitUntil(() => previous == null && SceneManager.GetActiveScene().name == "Episode(01)"
            && UnityEngine.Object.FindAnyObjectByType<PlayerController>() != null && !(bool)loading.GetValue(game));
        return UnityEngine.Object.FindAnyObjectByType<PlayerController>();
    }

    private static ItemData Item() {
        return UnityEditor.AssetDatabase.FindAssets("t:ItemData")
            .Select(g => UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(UnityEditor.AssetDatabase.GUIDToAssetPath(g))).First();
    }

    public static async Task<string> InventoryCaptureIsolation() {
        var player = await StartGame();
        var inventory = player.Inventory;
        var previous = inventory.GetInventory();
        try {
            var item = Item();
            inventory.LoadInventory(new List<InventoryItem> { new InventoryItem(item.ID, 2) });
            var captured = (PlayerSaveData)player.CaptureState();
            inventory.RemoveItem(item.ID, 1);
            Check(captured.items.Count == 1 && captured.items[0].quantity == 2,
                "Consuming an item must not mutate a captured snapshot.");
            inventory.RemoveItem(item.ID, 1);
            Check(captured.items.Count == 1, "Removing an inventory slot must not remove the captured slot.");
            return "PASS: captured inventory list and entries are independent of live inventory.";
        } finally { inventory.LoadInventory(previous); }
    }

    public static async Task<string> InventoryRestoreIsolation() {
        var player = await StartGame();
        var inventory = player.Inventory;
        var previous = inventory.GetInventory();
        try {
            var item = Item();
            var loaded = new List<InventoryItem> { new InventoryItem(item.ID, 2) };
            inventory.LoadInventory(loaded);
            inventory.RemoveItem(item.ID, 1);
            Check(loaded.Count == 1 && loaded[0].quantity == 2 && loaded[0].itemData == null,
                "Restore and later gameplay must not mutate loaded data or bind assets into it.");
            loaded[0].quantity = 10;
            loaded.Clear();
            Check(inventory.GetInventory().Count == 1 && inventory.GetInventory()[0].quantity == 1
                && inventory.GetInventory()[0].itemData != null, "Loaded source changes must not mutate live inventory.");
            return "PASS: restored inventory has its own list, entries and bound item assets.";
        } finally { inventory.LoadInventory(previous); }
    }

    public static string MetadataErrors() {
        var method = typeof(ISaveStorage).GetMethod("GetAllMetaData", new[] { typeof(List<MetaData>).MakeByRefType() });
        Check(method != null && method.ReturnType == typeof(Result), "Metadata listing must report Result independently of its list.");
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/SaveReviewChecks"));
        var folder = Path.Combine(root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try {
            var blocked = Path.Combine(folder, "not-a-directory");
            File.WriteAllText(blocked, "keep");
            object[] args = { null };
            var result = (Result)method.Invoke(new EasySaveStorage(blocked), args);
            Check(!result.Success && ((List<MetaData>)args[0]).Count == 0, "An invalid storage root must fail, not report no saves.");
            var legacy = (ISaveStorage)new SaveService(new SaveSystemSettings(null) { SaveDirectory = blocked });
            Check(!legacy.GetAllMetaData(out var legacyMetadata).Success && legacyMetadata.Count == 0,
                "The legacy compatibility adapter must honor metadata errors without changing its serializer.");
            var invalidChild = Path.Combine(blocked, "SavesV2");
            args = new object[] { null };
            result = (Result)method.Invoke(new EasySaveStorage(invalidChild), args);
            Check(!result.Success, "A save root below a file must fail, not report an empty directory.");
            legacy = new SaveService(new SaveSystemSettings(null) { SaveDirectory = invalidChild });
            Check(!legacy.GetAllMetaData(out legacyMetadata).Success, "Legacy metadata adapter must reject a file ancestor too.");
            var coordinator = new GameSaveService(new EasySaveStorage(blocked), null);
            Check(!coordinator.LoadAllMetadata(out var metadata).Success && metadata.Length == 0
                && !coordinator.QuickLoad().Success, "Listing failure must propagate through the coordinator.");
            args = new object[] { null };
            result = (Result)method.Invoke(new EasySaveStorage(Path.Combine(folder, "missing")), args);
            Check(result.Success && ((List<MetaData>)args[0]).Count == 0, "A missing save directory must be a successful empty list.");
            return "PASS: metadata listing distinguishes an unavailable root from an empty save directory.";
        } finally {
            Check(Path.GetFullPath(folder).StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), "Cleanup stays under test root.");
            Directory.Delete(folder, true);
        }
    }

    public static async Task<string> RestoreFailureStopsStartup() {
        var player = await StartGame();
        var resolver = UnityEngine.Object.FindAnyObjectByType<SceneLifetimeScope>().Container;
        var saves = resolver.Resolve<GameSaveService>();
        var game = resolver.Resolve<GameService>();
        var storageField = typeof(GameSaveService).GetField("_storage", BindingFlags.Instance | BindingFlags.NonPublic);
        var oldStorage = storageField.GetValue(saves);
        var oldData = GetSessionDataForTest(saves);
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/SaveReviewChecks"));
        var folder = Path.Combine(root, Guid.NewGuid().ToString("N"));
        int started = 0;
        Action listener = () => started++;
        try {
            var disk = new EasySaveStorage(folder);
            var scene = CaptureSceneForTest(resolver.Resolve<SceneSaveContext>());
            var trap = UnityEngine.Object.FindAnyObjectByType<Trap>();
            Check(trap != null, "Scene must have a trap.");
            scene[trap.GetComponent<SaveableEntity>().UniqueId] = new EntitySaveData { State = new ChestSaveData { isOpen = true } };
            var data = new GameSaveData { ResumeSceneName = "Episode(01)", Objects = scene };
            Check(disk.Save(data, new MetaData("broken-startup", "test", DateTime.UtcNow, "test")).Success, "Fixture must save.");
            storageField.SetValue(saves, disk);
            game.OnGameStarted += listener;
            game.RestartGame();
            await WaitUntil(() => player == null);
            var loading = typeof(GameService).GetField("_isLoadingScene", BindingFlags.Instance | BindingFlags.NonPublic);
            await WaitUntil(() => SceneManager.GetActiveScene().name == "MainMenu" && !(bool)loading.GetValue(game));
            Check(SceneManager.GetActiveScene().name == "MainMenu" && started == 0,
                "A failed Restore must return to MainMenu without a gameplay startup notification.");
            Check(!saves.HasLoadedGame && Time.timeScale == 1f, "Recovery must clear bad data and leave the menu usable.");
            return "PASS: failed object Restore prevents gameplay startup and recovers to a usable main menu.";
        } finally {
            game.OnGameStarted -= listener;
            storageField.SetValue(saves, oldStorage);
            SetSessionDataForTest(saves, oldData);
            Check(Path.GetFullPath(folder).StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), "Cleanup stays under test root.");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }

    private static async Task WaitUntil(Func<bool> predicate) {
        for (int i = 0; i < 100; i++) {
            if (predicate()) return;
            await Task.Delay(100);
        }
        throw new Exception("Timed out waiting for scene transition.");
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
