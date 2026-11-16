using VContainer;
using VContainer.Unity;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class UIRegressionChecks {
    public static string RunAssets() {
        Check(!Application.isPlaying, "Run asset checks outside Play Mode.");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            Check(!UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty, "Save open scene changes before asset checks.");
        string original = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        int buttons = 0;
        try {
            foreach (string path in new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Episode(01).unity", "Assets/Scenes/EndGame.unity" }) {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
                foreach (var root in scene.GetRootGameObjects()) {
                    foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                        Check(UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "Missing script: " + transform.name);
                    foreach (var button in root.GetComponentsInChildren<Button>(true)) {
                        buttons++;
                        Check(button.GetComponent<MenuButtonUI>() != null, "Missing shared button behaviour: " + button.name);
                        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++) {
                            var target = button.onClick.GetPersistentTarget(i);
                            string method = button.onClick.GetPersistentMethodName(i);
                            Check(target != null && target.GetType().GetMethods().Any(m => m.Name == method), "Broken click handler: " + button.name + " / " + method);
                            if (scene.name == "MainMenu" && (button.name == "Github" || button.name == "Linkedin"))
                                Check(method == "OpenURL", "Social buttons must open their URL, not quit.");
                        }
                        var so = new UnityEditor.SerializedObject(button.GetComponent<MenuButtonUI>());
                        Check(so.FindProperty("_hoverSound").objectReferenceValue is AudioData, "Button sound must reference AudioData: " + button.name);
                    }
                }
                if (scene.name == "EndGame") {
                    var system = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
                    Check(system.firstSelectedGameObject != null, "EndGame requires initial keyboard focus.");
                }
            }
            var transition = new UnityEditor.SerializedObject(UnityEditor.AssetDatabase.LoadAssetAtPath<UITransition>("Assets/UI/StandardTransition.asset"));
            Check(transition.FindProperty("fromEaseType").intValue == (int)PrimeTween.Ease.InOutQuad &&
                transition.FindProperty("toEaseType").intValue == (int)PrimeTween.Ease.InOutQuad, "Transition easing must preserve InOutQuad.");
            var door = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Door .prefab");
            var highlight = new UnityEditor.SerializedObject(door.GetComponentInChildren<SquareHighlight>(true));
            Check(highlight.FindProperty("easeType").intValue == (int)PrimeTween.Ease.InOutSine, "Highlight easing must preserve InOutSine.");
        } finally {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(original);
        }
        return "PASS: three scenes, " + buttons + " shared buttons, no missing scripts/click targets, correct AudioData and easing references, EndGame focus.";
    }

    public static async Task<string> Run() {
        if (!Application.isPlaying) throw new InvalidOperationException("Run in Play Mode.");
        float previousTimeScale = Time.timeScale;
        var from = new GameObject("UIRegressionFrom", typeof(RectTransform), typeof(CanvasGroup));
        var to = new GameObject("UIRegressionTo", typeof(RectTransform), typeof(CanvasGroup));
        var transition = UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UITransition>("Assets/UI/StandardTransition.asset"));
        try {
            Time.timeScale = 0f;
            to.SetActive(false);
            _ = transition.Transition(from, to);
            await Task.Delay(700);
            Check(!from.activeSelf, "Closed panel must be inactive at timeScale=0.");
            Check(!from.GetComponent<CanvasGroup>().blocksRaycasts, "Closed panel must release raycasts.");
            Check(to.activeSelf && to.GetComponent<CanvasGroup>().interactable, "Opened panel must accept input.");
            Check(Mathf.Approximately(to.GetComponent<CanvasGroup>().alpha, 1f), "Opened panel must be visible.");
        } finally {
            Time.timeScale = previousTimeScale;
            UnityEngine.Object.Destroy(from);
            UnityEngine.Object.Destroy(to);
            UnityEngine.Object.Destroy(transition);
        }

        var button = new GameObject("UIRegressionButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var behaviour = button.AddComponent<MenuButtonUI>();
        Resolver().InjectGameObject(button);
        try {
            var eventSystem = EventSystem.current;
            Check(eventSystem != null, "An EventSystem must be available.");
            eventSystem.SetSelectedGameObject(button);
            await Task.Delay(300);
            Check(button.transform.localScale.x > 1f, "Selected button must be highlighted.");
            eventSystem.SetSelectedGameObject(null);
            await Task.Delay(300);
            Check(Vector3.Distance(button.transform.localScale, Vector3.one) < 0.001f, "Deselected button must return to normal scale.");
            eventSystem.SetSelectedGameObject(button);
            eventSystem.SetSelectedGameObject(null);
            var noOpTween = (PrimeTween.Tween)typeof(MenuButtonUI).GetField("_scaleTween", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(behaviour);
            Check(!noOpTween.isAlive, "Immediate deselect must not start a redundant scale tween.");
            eventSystem.SetSelectedGameObject(button);
            await Task.Delay(300);
            behaviour.enabled = false;
            Check(Vector3.Distance(button.transform.localScale, Vector3.one) < 0.001f, "Disabled button must reset its scale.");
        } finally {
            UnityEngine.Object.Destroy(button);
        }
        return "PASS: unscaled transition, raycast release, button deselect and disable reset.";
    }

    public static async Task<string> RunPanels() {
        Check(Application.isPlaying, "Run panel checks in Play Mode.");
        var ui = UnityEngine.Object.FindAnyObjectByType<GameUI>();
        Check(ui != null && Services<GameService>().CurrentGameState == GameState.Playing, "Start in the gameplay scene.");
        int firstFrame = Time.frameCount;
        await Task.Delay(150);
        Check(Time.frameCount > firstFrame, "Play Mode must advance frames.");
        PressEscape();
        Check(Time.timeScale == 0f && Services<GameService>().CurrentGameState == GameState.Paused, "Esc must pause immediately.");
        Check(!PlayerInputEnabled(), "Player input must stop while paused.");
        ui.ResumeGame();
        await Task.Delay(600);
        CheckOnlyPanel(ui, ui.PauseMenu.gameObject);
        Check(Time.timeScale == 0f, "Repeated input during the transition must not resume gameplay.");
        ui.OpenSettings();
        ui.OpenSettings();
        ui.ReturnToPauseMenu();
        await Task.Delay(600);
        CheckOnlyPanel(ui, ui.SettingsPanel.gameObject);
        Check(Time.timeScale == 0f, "Settings must preserve pause.");
        PressEscape();
        await Task.Delay(600);
        CheckOnlyPanel(ui, ui.PauseMenu.gameObject);
        PressEscape();
        await Task.Delay(600);
        CheckOnlyPanel(ui, ui.StatsPanel.gameObject);
        Check(Time.timeScale == 1f && PlayerInputEnabled(), "Resume must restore time and player input.");
        Check(!ui.backgroundPanel.activeSelf, "Resume must remove the blocking background.");

        for (int i = 0; i < 3; i++) {
            ui.enabled = false;
            ui.enabled = true;
        }
        var callbacks = (Delegate)typeof(GameService).GetField("OnGameover", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Services<GameService>());
        int count = 0;
        foreach (var callback in callbacks.GetInvocationList()) if (ReferenceEquals(callback.Target, ui)) count++;
        Check(count == 1, "Re-enabling UI must keep exactly one death subscription.");

        ui.OpenPauseMenu();
        await Task.Delay(600);
        ui.OpenSettings();
        Services<GameService>().Gameover();
        await Task.Delay(600);
        CheckOnlyPanel(ui, ui.PlayerDeathPanel.gameObject);
        PressEscape();
        await Task.Delay(600);
        CheckOnlyPanel(ui, ui.PlayerDeathPanel.gameObject);
        Check(Time.timeScale == 0f && !PlayerInputEnabled(), "Death must keep gameplay disabled.");
        return "PASS: real Esc, pause/input, settings/back, repeated requests, subscription lifecycle, death during transition.";
    }

    public static async Task<string> RunStartupPause() {
        Check(Application.isPlaying, "Run startup pause checks in Play Mode.");
        var game = Services<GameService>();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var loading = typeof(GameService).GetField("_isLoadingScene", flags);
        await WaitUntil(() => !(bool)loading.GetValue(game));
        game.StartNewGame("Episode(01)");
        await WaitUntil(() => UnityEngine.Object.FindAnyObjectByType<GameUI>() != null && !(bool)loading.GetValue(game));
        var ui = UnityEngine.Object.FindAnyObjectByType<GameUI>();
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var complete = typeof(GameService).GetMethod("CompleteScene", flags);
        bool completed = false;
        try {
            typeof(GameService).GetMethod("BeginScene", flags).Invoke(game, new object[] { scene });
            PressEscape();
            Check(game.CurrentGameState != GameState.Paused && !ui.PauseMenu.gameObject.activeSelf,
                "Esc during startup must not open a pause panel that completion would leave running.");
            Check(Time.timeScale == 0f && !PlayerInputEnabled(), "Startup must keep time and player input disabled.");
            complete.Invoke(game, new object[] { scene, Result.Ok() });
            completed = true;
            Check(game.CurrentGameState == GameState.Playing && Time.timeScale == 1f && PlayerInputEnabled(),
                "Successful startup must enable gameplay.");
            PressEscape();
            await Task.Delay(600);
            CheckOnlyPanel(ui, ui.PauseMenu.gameObject);
            Check(game.CurrentGameState == GameState.Paused && Time.timeScale == 0f, "Esc must pause after startup completes.");
            PressEscape();
            await Task.Delay(600);
            CheckOnlyPanel(ui, ui.StatsPanel.gameObject);
            Check(game.CurrentGameState == GameState.Playing && Time.timeScale == 1f, "Normal resume must still work.");
            return "PASS: startup Esc is rejected; completion and normal Esc pause/resume remain consistent.";
        } finally {
            if (!completed) complete.Invoke(game, new object[] { scene, Result.Ok() });
            game.ReturnToMainMenu();
            await WaitUntil(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu"
                && !(bool)loading.GetValue(game));
        }
    }

    public static async Task<string> RunRestartAndHud() {
        Check(Application.isPlaying && UnityEngine.Object.FindAnyObjectByType<GameUI>() != null, "Run restart checks in the gameplay scene.");
        var manager = Services<GameSaveService>();
        var serviceField = typeof(GameSaveService).GetField("_storage", BindingFlags.Instance | BindingFlags.NonPublic);
        var oldService = serviceField.GetValue(manager);
        var oldData = GetSessionDataForTest(manager);
        string tempRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Temp/UIRegressionSaves"));
        string saveDirectory = System.IO.Path.Combine(tempRoot, Guid.NewGuid().ToString("N"));
        var service = new EasySaveStorage(saveDirectory);
        try {
            var data = new GameSaveData { ResumeSceneName = "Episode(01)", Objects = CaptureSceneForTest(Services<SceneSaveContext>()) };
            foreach (var playerData in data.Objects.Values.Select(record => record.State).OfType<PlayerSaveData>()) {
                playerData.health = playerData.maxHealth;
                playerData.StateData = new StateData { stateTypeName = typeof(LocomotionState).FullName,
                    nestedStateData = new StateData { stateTypeName = typeof(IdleState).FullName } };
            }
            Check(service.Save(data, new MetaData("restart", "UI regression", DateTime.UtcNow, "test")).Success, "Isolated restart fixture must save.");
            serviceField.SetValue(manager, service);
            var oldUI = UnityEngine.Object.FindAnyObjectByType<GameUI>();
            Services<GameService>().RestartGame();
            Services<GameService>().RestartGame();
            await WaitUntil(() => oldUI == null && UnityEngine.Object.FindAnyObjectByType<GameUI>() != null);
            await Task.Delay(300);
            var ui = UnityEngine.Object.FindAnyObjectByType<GameUI>();
            Check(Services<GameService>().CurrentGameState == GameState.Playing && Time.timeScale == 1f, "Restart must reload the scene and clear Gameover.");
            CheckOnlyPanel(ui, ui.StatsPanel.gameObject);
            Check(PlayerInputEnabled(), "Restart must re-enable player input.");

            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            ui.OpenPauseMenu();
            await Task.Delay(600);
            int health = player.Health.Health;
            int coin = player.Inventory.Coin;
            var inventory = player.Inventory.GetInventory();
            try {
                player.Health.Restore(health - 1, player.Health.MaxHealth);
                player.Inventory.SetCoin(coin + 7);
                var itemGuid = UnityEditor.AssetDatabase.FindAssets("t:ItemData")[0];
                var item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(UnityEditor.AssetDatabase.GUIDToAssetPath(itemGuid));
                player.Inventory.LoadInventory(new System.Collections.Generic.List<InventoryItem> { new InventoryItem(item, 2) });
                ui.ResumeGame();
                await Task.Delay(600);
                CheckOnlyPanel(ui, ui.StatsPanel.gameObject);
                var stats = ui.StatsPanel;
                var slider = (Slider)typeof(StatsPanelUI).GetField("healthSlider", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(stats);
                var coinText = (TMPro.TMP_Text)typeof(StatsPanelUI).GetField("coinText", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(stats);
                Check(slider.value == health - 1, "HUD must refresh health after being hidden.");
                Check(coinText.text == (coin + 7).ToString(), "HUD must refresh coins after being hidden.");
                var slots = ui.StatsPanel.GetComponentsInChildren<InventorySlotUI>();
                Check(slots.Length == player.Inventory.SlotNumber, "Reopening inventory must not duplicate slots.");
                var icon = (Image)typeof(InventorySlotUI).GetField("_icon", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(slots[0]);
                Check(icon.enabled && icon.sprite == item.Icon, "Inventory must refresh items loaded while hidden.");
            } finally {
                player.Health.Restore(health, player.Health.MaxHealth);
                player.Inventory.SetCoin(coin);
                player.Inventory.LoadInventory(inventory);
                ui.StatsPanel.gameObject.SetActive(false);
                ui.StatsPanel.gameObject.SetActive(true);
            }

            serviceField.SetValue(manager, new EasySaveStorage(System.IO.Path.Combine(saveDirectory, "empty")));
            Services<GameService>().Gameover();
            Services<GameService>().RestartGame();
            await WaitUntil(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu" && Time.timeScale == 1f);
            Check(Time.timeScale == 1f && !manager.HasLoadedGame, "Missing-save restart must return to a usable main menu.");
        } finally {
            serviceField.SetValue(manager, oldService);
            SetSessionDataForTest(manager, oldData);
            string resolved = System.IO.Path.GetFullPath(saveDirectory);
            if (resolved.StartsWith(tempRoot + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && System.IO.Directory.Exists(resolved))
                System.IO.Directory.Delete(resolved, true);
        }
        return "PASS: isolated successful restart, repeated restart, health/coin/inventory refresh, missing-save fallback; real saves untouched.";
    }

    private static async Task WaitUntil(Func<bool> predicate) {
        for (int i = 0; i < 100; i++) {
            if (predicate()) return;
            await Task.Delay(100);
        }
        throw new Exception("UI regression: scene transition timed out.");
    }

    public static async Task<string> RunSceneFlow() {
        Check(Application.isPlaying, "Run scene-flow checks in Play Mode.");
        var menu = UnityEngine.Object.FindAnyObjectByType<UIManager>();
        Check(menu != null, "Start scene-flow checks in MainMenu.");
        SetSessionDataForTest(Services<GameSaveService>(), new GameSaveData());
        menu.LoadNewgameScene();
        menu.LoadNewgameScene();
        await WaitUntil(() => UnityEngine.Object.FindAnyObjectByType<GameUI>() != null);
        await Task.Delay(300);
        Check(!Services<GameSaveService>().HasLoadedGame, "New Game must discard the in-memory previous save.");
        var ui = UnityEngine.Object.FindAnyObjectByType<GameUI>();
        CheckOnlyPanel(ui, ui.StatsPanel.gameObject);

        var notification = new GameObject("UIRegressionNotification", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        notification.SetActive(false);
        notification.AddComponent<GameSaveTextUI>();
        Resolver().InjectGameObject(notification);
        notification.SetActive(true);
        float originalScale = Time.timeScale;
        try {
            Time.timeScale = 0f;
            var text = notification.GetComponent<TMPro.TMP_Text>();
            var component = notification.GetComponent<GameSaveTextUI>();
            var update = typeof(GameSaveTextUI).GetMethod("UpdateSaveText", BindingFlags.Instance | BindingFlags.NonPublic);
            update.Invoke(component, new object[] { Result.Ok() });
            await Task.Delay(600);
            Check(text.maxVisibleCharacters == "Game saved successfully.".Length, "Save text must animate while paused.");
            update.Invoke(component, new object[] { Result.Fail("expected regression error") });
            await Task.Delay(600);
            Check(text.text.Contains("expected regression error"), "An old success delay must not clear a new failure.");
            update.Invoke(component, new object[] { Result.Ok() });
            await Task.Delay(100);
            component.enabled = false;
            component.enabled = true;
            update.Invoke(component, new object[] { Result.Fail("new message") });
            await Task.Delay(1100);
            Check(text.text.Contains("new message"), "Disabling a notification must cancel its old callbacks.");
        } finally {
            Time.timeScale = originalScale;
            UnityEngine.Object.Destroy(notification);
        }

        var fade = new GameObject("UIRegressionFade", typeof(RectTransform), typeof(CanvasGroup));
        var highlight = new GameObject("UIRegressionHighlight");
        try {
            Time.timeScale = 0f;
            fade.AddComponent<FadeInScene>();
            var h = highlight.AddComponent<SquareHighlight>();
            await Task.Delay(200);
            Check(highlight.transform.localScale.x > 1f, "Highlight must animate while paused.");
            h.enabled = false;
            Check(Vector3.Distance(highlight.transform.localScale, Vector3.one) < 0.001f, "Highlight must reset on disable.");
            await Task.Delay(1000);
            Check(!fade.activeSelf && Mathf.Approximately(fade.GetComponent<CanvasGroup>().alpha, 0f), "Scene entry fade must finish while paused.");
        } finally {
            Time.timeScale = originalScale;
            UnityEngine.Object.Destroy(fade);
            UnityEngine.Object.Destroy(highlight);
        }

        ui.OpenPauseMenu();
        await Task.Delay(600);
        Services<GameService>().EndGame();
        ui.ResumeGame();
        await WaitUntil(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "EndGame");
        await Task.Delay(200);
        Check(Services<GameService>().CurrentGameState == GameState.EndGame && !PlayerInputEnabled(), "EndGame must keep gameplay input disabled.");
        var endUI = UnityEngine.Object.FindAnyObjectByType<EndGameUI>();
        Check(endUI != null, "EndGame must have its UI controller.");
        var returnButton = endUI.GetComponentInChildren<Button>();
        Check(EventSystem.current.currentSelectedGameObject == returnButton.gameObject, "EndGame must focus its return button.");
        ExecuteEvents.Execute(returnButton.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        await WaitUntil(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu");
        return "PASS: New Game double click, paused notification replacement/cancellation, scene entry fade, looping highlight, EndGame fade and MainMenu return.";
    }

    private static bool PlayerInputEnabled() {
        var actions = (InputSystem_Actions)typeof(InputService).GetField("_inputActions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Services<InputService>());
        return actions.Player.enabled;
    }

    private static void PressEscape() {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        Check(keyboard != null, "Keyboard must be available.");
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Escape));
        UnityEngine.InputSystem.InputSystem.Update();
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
        UnityEngine.InputSystem.InputSystem.Update();
    }

    private static void CheckOnlyPanel(GameUI ui, GameObject visible) {
        foreach (var panel in new[] { ui.StatsPanel.gameObject, ui.PauseMenu.gameObject, ui.SettingsPanel.gameObject, ui.PlayerDeathPanel.gameObject }) {
            bool expected = panel == visible;
            var group = panel.GetComponent<CanvasGroup>();
            Check(panel.activeSelf == expected, "Panel active state: " + panel.name);
            Check(group.interactable == expected && group.blocksRaycasts == expected, "Panel input state: " + panel.name);
            Check(Mathf.Approximately(group.alpha, expected ? 1f : 0f), "Panel alpha: " + panel.name);
        }
    }

    private static IObjectResolver Resolver() {
        return UnityEngine.Object.FindAnyObjectByType<SceneLifetimeScope>().Container;
    }

    private static T Services<T>() { return Resolver().Resolve<T>(); }

    private static void Check(bool condition, string message) {
        if (!condition) throw new Exception("UI regression: " + message);
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
