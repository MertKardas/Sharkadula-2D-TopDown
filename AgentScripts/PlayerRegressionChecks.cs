using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using VContainer;

public static class PlayerRegressionChecks {
    public static string SceneReferences() {
        Check(!Application.isPlaying, "Check serialized scenes outside Play Mode.");
        var original = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var opened = new System.Collections.Generic.List<UnityEngine.SceneManagement.Scene>();
        int players = 0;
        try {
            foreach (var entry in UnityEditor.EditorBuildSettings.scenes.Where(s => s.enabled)) {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(entry.path);
                if (!scene.isLoaded) {
                    scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(entry.path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
                    opened.Add(scene);
                }
                foreach (var player in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerController>(true))) {
                    players++;
                    Check(player.Configuration != null, "Player configuration must be assigned: " + entry.path);
                    player.Configuration.Validate();
                    var serialized = new UnityEditor.SerializedObject(player);
                    foreach (var name in new[] { "AttackComponent", "InteractionComponent", "HealthComponent", "MovementComponent",
                        "VisualComponent", "Rigidbody" }) {
                        var value = typeof(PlayerController).GetProperty(name).GetValue(player) as UnityEngine.Object;
                        Check(value != null, "Lost serialized player reference: " + entry.path + "/" + name);
                        Check(serialized.FindProperty("<" + name + ">k__BackingField").objectReferenceValue == value,
                            "Former field name must deserialize to the new property: " + name);
                    }
                }
            }
            Check(players > 0, "Build scenes must contain a player.");
            return "PASS: serialized component references in all build scenes, " + players + " player(s); no scenes saved.";
        } finally {
            if (original.isLoaded) UnityEngine.SceneManagement.SceneManager.SetActiveScene(original);
            foreach (var scene in opened) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        }
    }

    public static string Architecture() {
        var assembly = typeof(PlayerController).Assembly;
        Check(assembly.GetType("PlayerSaveAdapter") != null, "Player save mapping must have its own adapter.");
        Check(assembly.GetType("IComponent") == null, "Blanket Initialize(PlayerController) contract must be removed.");
        foreach (var type in new[] { typeof(IdleState), typeof(MoveState), typeof(RunState), typeof(LocomotionState), typeof(DeathState) }) {
            Check(type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .All(f => f.FieldType != typeof(InputService) && f.FieldType != typeof(SceneSaveContext)
                    && f.FieldType != typeof(AudioService) && f.FieldType != typeof(GameService)),
                "Player states must not own global/scene services: " + type.Name);
        }
        Check(typeof(RunState).BaseType == typeof(MoveState), "Walk/run must share movement lifecycle.");
        foreach (var name in new[] { "HealthComponent", "MovementComponent", "VisualComponent", "Rigidbody" }) {
            var property = typeof(PlayerController).GetProperty(name);
            Check(property != null && property.GetSetMethod(true)?.IsPrivate == true,
                "Component references must have private setters: " + name);
        }
        return "PASS: separate persistence, explicit initialization, service-free states and protected references.";
    }

    public static string LocomotionTransitions() {
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        Check(Application.isPlaying && player != null, "OpenGame first.");
        var move = typeof(PlayerController).GetProperty("MoveInput");
        var run = typeof(PlayerController).GetProperty("RunRequested");
        Check(move != null && run != null, "Controller must provide one input snapshot to the states.");
        var original = player.Machine.GetCurrentStateData();
        var velocity = player.Rigidbody.linearVelocity;
        try {
            player.Machine.SetState(player.Machine.GetState<LocomotionState>());
            move.SetValue(player, Vector2.right);
            run.SetValue(player, true);
            player.Machine.CheckTransitions();
            Check(player.Machine.GetCurrentStatePath().Contains(nameof(RunState)), "Sprint input chooses Run directly from Idle.");
            var saved = player.Machine.GetCurrentStateData();
            player.Machine.RestoreState(saved);
            Check(player.Machine.GetCurrentStatePath().Contains(nameof(RunState)), "Saved Run restores with its original type name.");
            run.SetValue(player, false);
            player.Machine.CheckTransitions();
            Check(player.Machine.GetCurrentStatePath().Contains(nameof(MoveState)), "Releasing sprint chooses Move.");
            player.Machine.FixedUpdate();
            Check(player.Rigidbody.linearVelocity.x > 0, "Selected movement state performs physics.");
            move.SetValue(player, Vector2.zero);
            player.Rigidbody.linearVelocity = Vector2.right;
            player.Machine.CheckTransitions();
            Check(player.Machine.GetCurrentStatePath().Contains(nameof(MoveState)), "Moving body decelerates before Idle.");
            player.Rigidbody.linearVelocity = Vector2.zero;
            player.Machine.CheckTransitions();
            Check(player.Machine.GetCurrentStatePath().Contains(nameof(IdleState)), "Stopped body chooses Idle.");
            return "PASS: centralized Idle/Move/Run selection, deceleration and nested save restore.";
        } finally {
            move.SetValue(player, Vector2.zero);
            run.SetValue(player, false);
            player.Machine.RestoreState(original);
            player.Rigidbody.linearVelocity = velocity;
        }
    }

    public static async Task<string> Run() {
        await OpenGame();
        Architecture();
        LocomotionTransitions();
        var results = new[] { HealthInputs(), Lifecycle(), InventoryBounds(), MovementAndSave(),
            DisabledRestore(), DisabledDeath(), Facing(), InteractionTargets() };
        await DeathAnimation();
        return "PASS: 11 player regressions, including architecture, locomotion and the real death animation. " + string.Join(" ", results);
    }

    private static void Check(bool value, string message) {
        if (!value) throw new Exception(message);
    }

    public static async Task<string> OpenGame() {
        Check(Application.isPlaying, "Play Mode required.");
        UnityEngine.Object.FindAnyObjectByType<SceneLifetimeScope>().Container.Resolve<GameService>().StartNewGame("Episode(01)");
        for (int i = 0; i < 100; i++) {
            await Task.Delay(100);
            if (UnityEngine.Object.FindAnyObjectByType<PlayerController>()?.Machine.CurrentState != null)
                return "PASS: player ready.";
        }
        throw new Exception("Player startup timed out.");
    }

    public static string HealthInputs() {
        {
            var health = new PlayerHealthService(100);
            health.Restore(40, health.MaxHealth);
            int changed = 0, hurt = 0, died = 0;
            health.OnHealthChanged += _ => changed++;
            health.OnTakingDamage += () => hurt++;
            health.OnDeath += () => died++;
            health.TakeDamage(0);
            health.TakeDamage(-5);
            Check(health.Health == 40 && changed == 0 && hurt == 0, "Zero/negative damage must not heal or emit events.");
            health.TakeDamage(10);
            Check(health.Health == 30 && changed == 1 && hurt == 1, "Positive damage emits health and hurt once.");
            health.TakeDamage(int.MaxValue);
            health.TakeDamage(1);
            Check(health.Health == 0 && changed == 2 && hurt == 1 && died == 1, "Fatal damage emits death once, without hurt or controller dependency.");
            return "PASS: damage bounds, event counts, standalone health and death.";
        }
    }

    public static string Lifecycle() {
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        Check(Application.isPlaying && player != null, "OpenGame first.");
        var health = player.Health;
        int originalHealth = health.Health;
        var state = player.Machine.GetCurrentStateData();
        var velocity = player.Rigidbody.linearVelocity;
        var scale = Time.timeScale;
        Time.timeScale = 0;
        try {
            player.Machine.SetState(player.Machine.GetState<LocomotionState>());
            health.Restore(50, health.MaxHealth);
            health.TakeDamage(1);
            Check(player.Machine.CurrentState is TakingDamageState, "Nonfatal hit enters TakingDamageState.");
            Check(typeof(TakingDamageState).GetMethod("FixedUpdate").DeclaringType == typeof(TakingDamageState), "Damage deceleration belongs in FixedUpdate.");
            health.TakeDamage(100);
            Check(player.Machine.CurrentState is DeathState, "A hit during damage animation must enter DeathState.");
            Check(ListenerCount(player.VisualComponent, "OnFinishDeathAnimation") == 1, "Death must be entered exactly once.");
            player.enabled = false;
            Check(ListenerCount(player.VisualComponent, "OnFinishDeathAnimation") == 0, "Disabled controller releases state callbacks.");
            health.Restore(50, health.MaxHealth);
            player.enabled = true;
            player.Machine.SetState(player.Machine.GetState<LocomotionState>());
            for (int i = 0; i < 3; i++) { player.enabled = false; player.enabled = true; }
            Check(ListenerCount(health, "OnTakingDamage") == 1 && ListenerCount(health, "OnDeath") == 1,
                "Enable/disable must keep one health subscription.");
            health.TakeDamage(1);
            player.VisualComponent.FinishTakingDamage();
            Check(player.Machine.CurrentState is LocomotionState, "Damage completion returns to locomotion.");
            player.enabled = false;
            Check(ListenerCount(health, "OnTakingDamage") == 0 && ListenerCount(health, "OnDeath") == 0,
                "Disabled player must not receive damage transitions.");
            return "PASS: fatal/nonfatal states, physics phase and enable/disable subscriptions.";
        } finally {
            health.Restore(originalHealth, health.MaxHealth);
            player.enabled = true;
            player.Machine.RestoreState(state);
            player.Rigidbody.linearVelocity = velocity;
            Time.timeScale = scale;
        }
    }

    public static string InventoryBounds() {
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        var inventory = player.Inventory;
        int coin = inventory.Coin;
        var original = inventory.CaptureInventory();
        try {
            inventory.SetCoin(10);
            inventory.SpendMoney(-5);
            Check(inventory.Coin == 10, "Negative spending must not increase balance.");
            inventory.AddCoin(-5);
            Check(inventory.Coin == 10, "Negative spending/addition must not change balance.");
            inventory.SpendMoney(4);
            Check(inventory.Coin == 6, "Valid spending must work.");
            Check(!inventory.HasItem("missing", 0) && !inventory.RemoveItem("missing", -1), "Nonpositive item requests fail.");
            Check(inventory.AddItem(null) == 0, "Null item must be ignored.");
            inventory.SetCoin(int.MaxValue);
            try { inventory.AddCoin(1); throw new Exception("Overflow must be rejected."); }
            catch (OverflowException) { }
            Check(inventory.Coin == int.MaxValue, "Overflow leaves balance intact.");
            var catalog = UnityEngine.Object.FindAnyObjectByType<SceneLifetimeScope>().Container.Resolve<ItemCatalog>();
            var item = catalog.GetItem("key");
            if (item == null) item = UnityEditor.AssetDatabase.FindAssets("t:ItemData")
                .Select(id => UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(UnityEditor.AssetDatabase.GUIDToAssetPath(id))).First();
            inventory.LoadInventory(null);
            Check(inventory.AddItem(new InventoryItem(item, inventory.SlotNumber * (item.IsStackable ? item.StackSize : 1) + 3))
                == inventory.SlotNumber * (item.IsStackable ? item.StackSize : 1), "Slot/stack capacity must be respected.");
            Check(inventory.AddItem(new InventoryItem(item, 1)) == 0, "Full inventory rejects additional items.");
            var snapshot = inventory.CaptureInventory();
            Check(inventory.RemoveItem(item.ID, 1) && snapshot.Sum(i => i.quantity) == inventory.GetInventory().Sum(i => i.quantity) + 1,
                "Removal works and captured quantities are independent.");
            return "PASS: coin/item bounds, full capacity, removal and snapshot isolation.";
        } finally { inventory.LoadInventory(original); inventory.SetCoin(coin); }
    }

    public static string DisabledRestore() {
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        var original = (PlayerSaveData)player.CaptureState();
        var restore = (PlayerSaveData)player.CaptureState();
        restore.StateData = new StateData { stateTypeName = typeof(TakingDamageState).FullName };
        try {
            player.enabled = false;
            player.RestoreState(restore);
            Check(ListenerCount(player.VisualComponent, "OnFinishTakeDamage") == 0, "Disabled restore must not enter a state.");
            Check(((PlayerSaveData)player.CaptureState()).StateData.stateTypeName == typeof(TakingDamageState).FullName,
                "Capture must preserve a deferred restored state.");
            player.enabled = true;
            Check(ListenerCount(player.VisualComponent, "OnFinishTakeDamage") == 1, "Enabling restored state subscribes once.");
            player.enabled = false;
            Check(ListenerCount(player.VisualComponent, "OnFinishTakeDamage") == 0, "Disabling restored state releases its callback.");
            return "PASS: disabled restore is deferred, captured and entered exactly once.";
        } finally {
            player.enabled = true;
            player.RestoreState(original);
        }
    }

    public static string DisabledDeath() {
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        var state = player.Machine.GetCurrentStateData();
        int health = player.Health.Health;
        try {
            player.Machine.SetState(player.Machine.GetState<LocomotionState>());
            player.Health.Restore(50, player.Health.MaxHealth);
            player.enabled = false;
            player.Health.TakeDamage(100);
            player.enabled = true;
            Check(player.Machine.CurrentState is DeathState, "Enabling a player killed while disabled must enter DeathState.");
            return "PASS: death while controller is disabled is reconciled on enable.";
        } finally {
            player.Health.Restore(health, player.Health.MaxHealth);
            player.enabled = true;
            player.Machine.RestoreState(state);
        }
    }

    public static string MovementAndSave() {
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        var movement = player.MovementComponent;
        var oldVelocity = player.Rigidbody.linearVelocity;
        try {
            player.Rigidbody.linearVelocity = Vector2.zero;
            movement.MoveCharacter(Vector2.right);
            float walk = player.Rigidbody.linearVelocity.x;
            Check(walk > 0 && walk <= player.Movement.MaxSpeed, "Walk accelerates toward its limit.");
            for (int i = 0; i < 100; i++) movement.RunCharacter(Vector2.right);
            Check(Mathf.Approximately(player.Rigidbody.linearVelocity.x, player.Movement.RunSpeed), "Run uses RunSpeed.");
            for (int i = 0; i < 100; i++) movement.MoveCharacter(Vector2.zero);
            Check(player.Rigidbody.linearVelocity == Vector2.zero, "Zero input decelerates to rest.");
            player.Rigidbody.linearVelocity = new Vector2(1.25f, -0.25f);
            Check(((PlayerSaveData)player.CaptureState()).velocity == player.Rigidbody.linearVelocity,
                "Save must capture actual Rigidbody velocity, including external changes.");
            return "PASS: walk, run, deceleration and actual saved velocity.";
        } finally { player.Rigidbody.linearVelocity = oldVelocity; }
    }

    public static async Task<string> MovementInput() {
        Check(Application.isPlaying, "Play Mode required.");
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        var keyboard = Keyboard.current;
        Check(player != null && keyboard != null, "OpenGame and keyboard required.");
        var position = player.transform.position;
        var background = InputSystem.settings.backgroundBehavior;
        var routing = InputSystem.settings.editorInputBehaviorInPlayMode;
        try {
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            await Task.Delay(200);
            var input = UnityEngine.Object.FindAnyObjectByType<SceneLifetimeScope>().Container.Resolve<InputService>();
            Check(player.Machine.GetCurrentStatePath().Contains(nameof(MoveState)),
                "Move input enters MoveState: " + player.Machine.GetCurrentStatePath() + ", key=" + keyboard.wKey.isPressed + ", input=" + input.ReadInput<Vector2>(InputType.Move));
            Check(player.Rigidbody.linearVelocity.y > 0, "Move input reaches physics.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.LeftShift));
            await Task.Delay(200);
            Check(player.Machine.GetCurrentStatePath().Contains(nameof(RunState)), "Sprint enters RunState.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            await Task.Delay(150);
            Check(player.Machine.GetCurrentStatePath().Contains(nameof(MoveState)), "Releasing sprint returns to MoveState.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            await Task.Delay(200);
            Check(player.Machine.GetCurrentStatePath().Contains(nameof(IdleState)) && player.Rigidbody.linearVelocity.magnitude < 0.1f,
                "Releasing movement stops and enters IdleState.");
            return "PASS: real input walks, sprints, returns to walk and stops.";
        } finally {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.settings.backgroundBehavior = background;
            InputSystem.settings.editorInputBehaviorInPlayMode = routing;
            player.transform.position = position;
            player.Rigidbody.linearVelocity = Vector2.zero;
        }
    }

    public static string Facing() {
        var visual = UnityEngine.Object.FindAnyObjectByType<PlayerVisualComponent>();
        var oldDirection = visual.LastDirection;
        try {
            visual.AnimationDirection(Vector2.right);
            visual.AnimationDirection(Vector2.one);
            Check(visual.LastDirection == Vector2.right, "Diagonal input preserves horizontal facing.");
            visual.AnimationDirection(Vector2.down);
            visual.AnimationDirection(Vector2.one);
            Check(visual.LastDirection == Vector2.up, "Diagonal input preserves vertical facing.");
            visual.AnimationDirection(Vector2.zero);
            Check(visual.LastDirection == Vector2.up, "Idle preserves facing.");
            return "PASS: four-direction facing and diagonal axis preference preserved.";
        } finally { visual.AnimationDirection(oldDirection); }
    }

    public static async Task<string> DeathAnimation() {
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        Check(Application.isPlaying && player != null, "OpenGame first.");
        var game = UnityEngine.Object.FindAnyObjectByType<SceneLifetimeScope>().Container.Resolve<GameService>();
        player.Health.TakeDamage(int.MaxValue);
        for (int i = 0; i < 100; i++) {
            await Task.Delay(50);
            if (game.CurrentGameState == GameState.Gameover) {
                Check(player.Machine.CurrentState is DeathState, "Animation completion retains DeathState.");
                Check(!player.VisualComponent.GetComponent<SpriteRenderer>().enabled, "Death animation hides the sprite.");
                Check(Time.timeScale == 0, "Gameover pauses the game.");
                return "PASS: fatal damage, real Animator death event and Gameover UI flow.";
            }
        }
        throw new Exception("Death animation did not complete Gameover.");
    }

    private static int ListenerCount(object target, string eventName) {
        var field = target.GetType().GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic);
        return ((Delegate)field.GetValue(target))?.GetInvocationList().Length ?? 0;
    }

    public static string InteractionTargets() {
        Check(Application.isPlaying, "Interaction lifecycle requires Play Mode.");
        var go = new GameObject("Interaction regression");
        var near = new GameObject("Near target");
        var far = new GameObject("Far target");
        try {
            var interaction = go.AddComponent<InteractionComponent>();
            interaction.Initialize(new PlayerInteractionService(), null, 0.75f);
            var a = near.AddComponent<PlayerInteractionProbe>();
            var b = far.AddComponent<PlayerInteractionProbe>();
            near.transform.position = Vector3.right;
            far.transform.position = Vector3.right * 2;
            var a1 = near.AddComponent<BoxCollider2D>();
            var a2 = near.AddComponent<CircleCollider2D>();
            var b1 = far.AddComponent<BoxCollider2D>();
            go.SendMessage("OnTriggerEnter2D", a1);
            go.SendMessage("OnTriggerEnter2D", a2);
            go.SendMessage("OnTriggerEnter2D", b1);
            go.SendMessage("OnTriggerExit2D", a1);
            interaction.InteractAction();
            Check(a.Calls == 1 && b.Calls == 0, "One collider exiting must not remove an overlapping target.");
            a.enabled = false;
            interaction.InteractAction();
            Check(b.Calls == 1, "Disabled targets must be skipped.");
            UnityEngine.Object.DestroyImmediate(near);
            interaction.InteractAction();
            Check(b.Calls == 2, "Destroyed targets must be skipped.");
            go.SetActive(false);
            Check(!b.CanInteract, "Closing the interaction sensor clears range flags.");
            return "PASS: closest target, multiple colliders, disabled/destroyed targets and range cleanup.";
        } finally {
            UnityEngine.Object.DestroyImmediate(go);
            if (near != null) UnityEngine.Object.DestroyImmediate(near);
            UnityEngine.Object.DestroyImmediate(far);
        }
    }
}

public class PlayerInteractionProbe : MonoBehaviour, IInteractable {
    public int Calls;
    public bool CanInteract { get; private set; }
    public string InteractionPrompt => "Test";
    public void SetInRange(bool value) { CanInteract = value; }
    public void Interact(PlayerController player) { Calls++; }
}
