using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using VContainer;
using VContainer.Unity;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class SaveRegressionChecks {
    private static void Check(bool condition, string message) {
        if (!condition) throw new Exception(message);
    }

    public static string DataContract() {
        Check(typeof(GameSaveData).GetField("Entries") == null && typeof(GameSaveData).GetMethod("Set") == null,
            "The generic wrapper must be removed.");
        var data = new GameSaveData { ResumeSceneName = "Episode(01)" };
        data.Objects["static"] = new EntitySaveData { IsRemoved = true };
        Check(data.Objects["static"].IsRemoved && typeof(GameSaveData).GetField("Scenes") == null, "One ID table must hold removal markers.");
        return "PASS: direct ID-table data contract.";
    }

    public static string StorageRoundTrip() {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/EasySaveChecks"));
        var folder = Path.Combine(root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try {
            var storage = new EasySaveStorage(folder);
            var data = new GameSaveData { ResumeSceneName = "Episode(01)" };
            data.Objects[GameSaveService.PlayerId] = new EntitySaveData { State = new PlayerSaveData {
                health = 77, maxHealth = 100, position = new Vector2(3, 4),
                items = new List<InventoryItem> { new InventoryItem("test-item", 2) },
                StateData = new StateData { stateTypeName = typeof(LocomotionState).FullName,
                    customData = new Dictionary<string, object> { { "timer", 0.25f } },
                    nestedStateData = new StateData { stateTypeName = typeof(IdleState).FullName } }
            } };
            data.Objects["drop"] = new EntitySaveData { PrefabId = "test-item", SpawnPosition = new Vector3(1, 2, 0), State = new DropObjectSaveData { itemID = "test-item", quantity = 2 } };
            data.Objects["spawner"] = new EntitySaveData { State = new SpawnerSaveData { DynamicIds = new List<string> { "drop" } } };
            data.Objects["removed-static"] = new EntitySaveData { IsRemoved = true };
            var metadata = new MetaData("test", "Test", DateTime.UtcNow, "test-version");
            var saved = storage.Save(data, metadata);
            Check(saved.Success, "Easy Save must save: " + saved.ErrorMessage);
            Check(File.Exists(Path.Combine(folder, "test.es3")), "One slot must create one es3 file.");
            var load = storage.Load("test", out var loaded);
            Check(load.Success, "Easy Save must read its own file: " + load.ErrorMessage);
            var player = loaded.Objects[GameSaveService.PlayerId].State as PlayerSaveData;
            Check(player != null && player.health == 77 && player.position == new Vector2(3, 4), "Player data must survive.");
            Check(player.items.Single().item == "test-item" && player.items[0].quantity == 2 && player.items[0].itemData == null, "Inventory data must remain data-only.");
            Check(player.StateData.customData["timer"] is float timer && timer == 0.25f && player.StateData.nestedStateData.stateTypeName == typeof(IdleState).FullName, "Nested states must survive.");
            Check(loaded.Objects["drop"].State is DropObjectSaveData drop && drop.quantity == 2 && loaded.Objects["drop"].SpawnPosition.y == 2f
                && ((SpawnerSaveData)loaded.Objects["spawner"].State).DynamicIds.Single() == "drop" && loaded.Objects["removed-static"].IsRemoved, "Unified records, membership and removal marker must survive.");
            Check(storage.GetAllMetaData(out var listed).Success && listed.Count == 1 && listed[0].SaveTime == metadata.SaveTime && listed[0].SaveTime.Kind == DateTimeKind.Utc, "Metadata must preserve UTC.");
            ((PlayerSaveData)data.Objects[GameSaveService.PlayerId].State).coin = 99;
            Check(storage.Save(data, metadata).Success && storage.Load("test", out loaded).Success && ((PlayerSaveData)loaded.Objects[GameSaveService.PlayerId].State).coin == 99, "Overwrite must replace values.");
            foreach (var invalid in new[] { "../escape", "..\\escape", "", ".", "..", Path.GetFullPath(folder) })
                Check(!storage.Save(data, new MetaData(invalid, "bad", DateTime.UtcNow, "test")).Success && !storage.Load(invalid, out _) .Success && !storage.DeleteSave(invalid).Success, "Unsafe slots must be rejected.");
            Check(!storage.Save(data, new MetaData("old", "old", DateTime.UtcNow, "test") { FormatVersion = 1 }).Success, "V1 data must not be written as the new format.");
            File.WriteAllText(Path.Combine(folder, "corrupt.es3"), "invalid-save");
            Check(!storage.Load("corrupt", out var broken).Success && broken == null, "Corrupt files must fail.");
            Check(storage.GetAllMetaData(out listed).Success && listed.Count == 1, "One bad file must not hide valid metadata.");
            Check(storage.DeleteSave("test").Success && !storage.Load("test", out _).Success && File.Exists(Path.Combine(folder, "corrupt.es3")), "Delete must affect only its slot.");
            return "PASS: ES3 unified records, spawner IDs, markers, nested states, UTC, overwrite, invalid paths and version rejection.";
        } finally {
            Check(Path.GetFullPath(folder).StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), "Cleanup must stay under the test root.");
            Directory.Delete(folder, true);
        }
    }

    private static async Task WaitForScene(string sceneName) {
        for (int i = 0; i < 100; i++) {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == sceneName) {
                await Task.Delay(200);
                return;
            }
            await Task.Delay(100);
        }
        throw new Exception("Scene transition timed out: " + sceneName);
    }

    public static async Task<string> QuickSaveInput() {
        Check(Application.isPlaying, "Input check requires Play Mode.");
        var scope = UnityEngine.Object.FindAnyObjectByType<SceneLifetimeScope>();
        scope.Container.Resolve<GameService>().StartNewGame("Episode(01)");
        await WaitForScene("Episode(01)");
        scope = UnityEngine.Object.FindAnyObjectByType<SceneLifetimeScope>();
        var saves = scope.Container.Resolve<GameSaveService>();
        var context = scope.Container.Resolve<SceneSaveContext>();
        var storageField = typeof(GameSaveService).GetField("_storage", BindingFlags.Instance | BindingFlags.NonPublic);
        var oldStorage = storageField.GetValue(saves);
        var oldData = GetSessionDataForTest(saves);
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/EasySaveChecks"));
        var folder = Path.Combine(root, Guid.NewGuid().ToString("N"));
        var keyboard = Keyboard.current;
        Check(keyboard != null, "A keyboard is required for this integration check.");
        var oldBackground = InputSystem.settings.backgroundBehavior;
        var oldEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        int savesCount = 0;
        Action<Result> listener = result => { Check(result.Success, "Quick save must succeed: " + result.ErrorMessage); savesCount++; };
        try {
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            storageField.SetValue(saves, new EasySaveStorage(folder));
            saves.OnSaveAfter += listener;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F5));
            await Task.Delay(350);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            await Task.Delay(100);
            Check(savesCount == 1, "Holding F5 must produce exactly one save, got: " + savesCount);
            Check(new EasySaveStorage(folder).Load("quickSave", out _).Success, "F5 must write a loadable save.");
            return "PASS: held F5 produces one successful Easy Save write.";
        } finally {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.settings.backgroundBehavior = oldBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorInput;
            saves.OnSaveAfter -= listener;
            storageField.SetValue(saves, oldStorage);
            SetSessionDataForTest(saves, oldData);
            Check(Path.GetFullPath(folder).StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), "Cleanup must stay under the test root.");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }

    public static string CompilePlayerScripts() {
        Check(!Application.isPlaying, "Compile player scripts outside Play Mode.");
        var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/EasySavePlayerScripts"));
        Directory.CreateDirectory(output);
        var settings = new UnityEditor.Build.Player.ScriptCompilationSettings {
            target = UnityEditor.BuildTarget.StandaloneWindows64,
            group = UnityEditor.BuildTargetGroup.Standalone
        };
        var result = UnityEditor.Build.Player.PlayerBuildInterface.CompilePlayerScripts(settings, output);
        Check(result.assemblies != null && result.assemblies.Any(path => Path.GetFileName(path) == "Assembly-CSharp.dll"),
            "Player compilation must produce Assembly-CSharp.dll.");
        return "PASS: Windows player C# compilation, " + result.assemblies.Count + " assemblies; not a full executable build.";
    }

    // Fixtures inspect the private session; production callers use LoadGame/Clear.
    private static GameSaveData GetSessionDataForTest(GameSaveService saves) {
        return (GameSaveData)typeof(GameSaveService).GetField("_currentData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(saves);
    }

    private static void SetSessionDataForTest(GameSaveService saves, GameSaveData data) {
        typeof(GameSaveService).GetField("_currentData", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(saves, data);
    }

}
