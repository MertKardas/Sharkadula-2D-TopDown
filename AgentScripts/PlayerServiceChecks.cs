using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VContainer;

public static class PlayerServiceChecks {
    private static void Check(bool condition, string message) {
        if (!condition) throw new Exception(message);
    }

    public static string Architecture() {
        var assembly = typeof(PlayerController).Assembly;
        var config = assembly.GetType("PlayerConfiguration");
        Check(config != null && typeof(ScriptableObject).IsAssignableFrom(config), "Player defaults must be configurable with a ScriptableObject.");
        foreach (var name in new[] { "PlayerHealthService", "PlayerMovementService", "PlayerInventoryService", "PlayerInteractionService", "PlayerServices" }) {
            var type = assembly.GetType(name);
            Check(type != null && !typeof(MonoBehaviour).IsAssignableFrom(type), "Player logic must be a plain service: " + name);
        }
        Check(assembly.GetType("PlayerServiceFactory") == null, "Player must own its services without a container factory.");
        Check(assembly.GetType("InventoryComponent") == null, "Inventory logic must have no redundant Unity host.");
        return "PASS: SO configuration and player-owned plain services, without a factory.";
    }

    public static string ConfigurationAndIsolation() {
        var config = ScriptableObject.CreateInstance<PlayerConfiguration>();
        try {
            var serialized = new SerializedObject(config);
            serialized.FindProperty("walkSpeed").floatValue = 4;
            serialized.FindProperty("runSpeed").floatValue = 7;
            serialized.FindProperty("acceleration").floatValue = 20;
            serialized.FindProperty("deceleration").floatValue = 30;
            serialized.FindProperty("maxHealth").intValue = 75;
            serialized.FindProperty("inventorySlots").intValue = 2;
            serialized.FindProperty("startingCoin").intValue = 9;
            serialized.FindProperty("inputDeadzone").floatValue = 0.001f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            string before = EditorJsonUtility.ToJson(config);
            using (var catalog = new ItemCatalog()) {
                var a = new PlayerServices(config, catalog);
                var b = new PlayerServices(config, catalog);
                Check(!ReferenceEquals(a.Health, b.Health) && !ReferenceEquals(a.Inventory, b.Inventory)
                    && !ReferenceEquals(a.Movement, b.Movement) && !ReferenceEquals(a.Interaction, b.Interaction), "Each owner must get separate services.");
                Check(a.Health.Health == 75 && a.Inventory.SlotNumber == 2 && a.Inventory.Coin == 9,
                    "Health and inventory defaults must come from SO.");
                Check(a.Movement.CalculateVelocity(Vector2.zero, Vector2.right, false, 1) == Vector2.right * 4
                    && a.Movement.CalculateVelocity(Vector2.zero, Vector2.right, true, 1) == Vector2.right * 7,
                    "Movement speeds must come from SO.");
                Check(a.Movement.CalculateVelocity(Vector2.zero, Vector2.right * 0.005f, false, 1).x > 0,
                    "Small analog input accepted by configured deadzone must move the body.");
                a.Health.TakeDamage(10);
                a.Inventory.AddCoin(3);
                a.Movement.MaxSpeed = 99;
                Check(a.Health.Health == 65 && b.Health.Health == 75 && a.Inventory.Coin == 12 && b.Inventory.Coin == 9
                    && b.Movement.MaxSpeed == 4, "Runtime state must not leak between owners.");
                var data = AssetDatabase.FindAssets("t:ItemData").Select(g => AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(g)))
                    .First(i => catalog.GetItem(i.ID) != null);
                a.Inventory.AddItem(new InventoryItem(data, 1));
                Check(b.Inventory.GetInventory().Count == 0, "Inventory contents must not be shared.");
                a.Health.Restore(12, 120);
                Check(a.Health.MaxHealth == 120 && config.MaxHealth == 75 && EditorJsonUtility.ToJson(config) == before,
                    "Save restore and gameplay must never write to SO.");
                try { a.Health.Restore(200, 100); throw new Exception("Invalid health must fail."); }
                catch (ArgumentOutOfRangeException) { }
                Check(a.Health.Health == 12 && a.Health.MaxHealth == 120, "Invalid restore must preserve state.");
                serialized.Update();
                serialized.FindProperty("maxHealth").intValue = 0;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                try { new PlayerServices(config, catalog); throw new Exception("Invalid configuration must fail."); }
                catch (InvalidOperationException) { }
            }
            return "PASS: configured speeds/health/inventory, owner isolation, immutable SO and invalid-config/restore rejection.";
        } finally { UnityEngine.Object.DestroyImmediate(config); }
    }

    public static string InvalidConfigurationStartup() {
        Check(Application.isPlaying, "Play Mode required.");
        var scope = UnityEngine.Object.FindAnyObjectByType<SceneLifetimeScope>();
        var context = new SceneSaveContext(scope.gameObject.scene, scope.Container.Resolve<GameSaveService>(), scope.Container.Resolve<GameService>());
        var config = ScriptableObject.CreateInstance<PlayerConfiguration>();
        var go = new GameObject("Invalid player configuration regression");
        go.SetActive(false);
        try {
            typeof(PlayerConfiguration).GetField("maxHealth", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(config, 0);
            var player = go.AddComponent<PlayerController>();
            typeof(PlayerController).GetField("configuration", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, config);
            player.Construct(scope.Container.Resolve<InputService>(), context, scope.Container.Resolve<GameService>(), scope.Container.Resolve<ItemCatalog>());
            go.SetActive(true);
            Check(!player.enabled, "Invalid configuration must disable the player before callbacks run.");
            var result = context.ValidateStartup();
            Check(!result.Success && result.ErrorMessage.Contains("PlayerConfiguration"), "Invalid configuration must fail scene startup with its cause.");
            return "PASS: invalid configuration disables player and reports scene startup failure without callback null errors.";
        } finally {
            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(config);
        }
    }
}
