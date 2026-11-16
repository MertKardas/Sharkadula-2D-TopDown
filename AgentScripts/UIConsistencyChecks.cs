using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

public static class UIConsistencyChecks {
    public static async Task<string> StartupFocus() {
        Check(Application.isPlaying, "Run in Play Mode in MainMenu.");
        var original = UnityEngine.Object.FindAnyObjectByType<UIManager>();
        Check(original != null, "MainMenu must be open.");
        var system = EventSystem.current;
        Check(system != null, "An EventSystem must exist.");
        var first = system.firstSelectedGameObject;
        GameObject clone = null;
        try {
            original.gameObject.SetActive(false);
            clone = UnityEngine.Object.Instantiate(original.gameObject, original.transform.parent);
            UnityEngine.Object.FindAnyObjectByType<SceneLifetimeScope>().Container.InjectGameObject(clone);
            system.gameObject.SetActive(false);
            system.firstSelectedGameObject = null;
            clone.SetActive(true);
            system.gameObject.SetActive(true);
            await Task.Delay(150);
            var selected = system.currentSelectedGameObject;
            Check(selected != null && selected.transform.IsChildOf(clone.transform),
                "Initial menu focus must survive EventSystem enabling after UIManager.");
            Check(selected.GetComponent<Selectable>().IsInteractable(), "Initial focus must be usable.");
        } finally {
            if (clone != null) {
                clone.SetActive(false);
                UnityEngine.Object.Destroy(clone);
            }
            system.firstSelectedGameObject = first;
            system.gameObject.SetActive(true);
            original.gameObject.SetActive(true);
        }
        return "PASS: initial MainMenu focus survives late EventSystem enable.";
    }

    public static string MainMenuRefresh() {
        Check(Application.isPlaying, "Run in Play Mode in MainMenu.");
        var menu = UnityEngine.Object.FindAnyObjectByType<UIManager>();
        Check(menu != null, "MainMenu must be open.");
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var game = (GameService)typeof(UIManager).GetField("_game", flags).GetValue(menu);
        var originalSaves = (GameSaveService)typeof(UIManager).GetField("_saves", flags).GetValue(menu);
        var resume = (Button)typeof(UIManager).GetField("resumeGameButton", flags).GetValue(menu);
        var preferredField = typeof(UIManager).GetField("firstSelectedButton", flags);
        var preferred = (Button)preferredField.GetValue(menu);
        string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/UIConsistency", Guid.NewGuid().ToString("N")));
        var storage = new EasySaveStorage(directory);
        bool enabled = menu.enabled;
        try {
            menu.enabled = false;
            var validator = (WorldSaveValidator)typeof(GameSaveService).GetField("_validator", flags).GetValue(originalSaves);
            menu.Construct(game, new GameSaveService(storage, validator));
            resume.interactable = true;
            menu.enabled = true;
            Check(!resume.interactable, "Reopened MainMenu must disable Continue when no save exists.");
            menu.enabled = false;
            var result = storage.Save(new GameSaveData(), new MetaData("test", "Test", DateTime.UtcNow, Application.version));
            Check(result.Success, result.ErrorMessage);
            preferredField.SetValue(menu, resume);
            menu.enabled = true;
            Check(resume.interactable, "Reopened MainMenu must enable Continue after a save is created.");
            Check(EventSystem.current.currentSelectedGameObject == resume.gameObject, "Reopened MainMenu must restore its preferred focus.");
            menu.enabled = false;
            Check(storage.DeleteSave("test").Success, "Delete isolated test save.");
            menu.enabled = true;
            Check(!resume.interactable, "Reopened MainMenu must reflect a deleted save.");
            var selected = EventSystem.current.currentSelectedGameObject;
            Check(selected != null && selected != resume.gameObject && selected.GetComponent<Selectable>().IsInteractable(), "Unavailable preferred button must fall back to an interactable button.");
        } finally {
            menu.enabled = false;
            preferredField.SetValue(menu, preferred);
            menu.Construct(game, originalSaves);
            menu.enabled = enabled;
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/UIConsistency")) + Path.DirectorySeparatorChar;
            if (directory.StartsWith(root, StringComparison.OrdinalIgnoreCase) && Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
        return "PASS: MainMenu refreshes save availability and focus on reopen; unavailable preferred button falls back.";
    }

    public static string Focus() {
        Check(Application.isPlaying, "Run in Play Mode.");
        var type = typeof(GameUI).Assembly.GetType("UISelection");
        Check(type != null, "Shared UISelection must exist.");
        var select = type.GetMethod("Select", BindingFlags.Public | BindingFlags.Static);
        var panel = new GameObject("FocusRegression", typeof(RectTransform));
        var unavailable = new GameObject("Unavailable", typeof(RectTransform), typeof(Button));
        var available = new GameObject("Available", typeof(RectTransform), typeof(Button));
        var previous = EventSystem.current.currentSelectedGameObject;
        try {
            unavailable.transform.SetParent(panel.transform);
            available.transform.SetParent(panel.transform);
            unavailable.GetComponent<Button>().interactable = false;
            select.Invoke(null, new object[] { panel, unavailable.GetComponent<Button>() });
            Check(EventSystem.current.currentSelectedGameObject == available, "Focus must skip disabled buttons.");
            available.SetActive(false);
            select.Invoke(null, new object[] { panel, available.GetComponent<Button>() });
            Check(EventSystem.current.currentSelectedGameObject == null, "No usable selection must clear stale focus.");
            select.Invoke(null, new object[] { null, null });
        } finally {
            EventSystem.current.SetSelectedGameObject(previous);
            UnityEngine.Object.Destroy(panel);
        }
        return "PASS: shared focus skips unavailable controls and clears stale selection.";
    }

    public static string ButtonLabelsFit() {
        Check(Application.isPlaying, "Run in Play Mode with a menu visible.");
        Canvas.ForceUpdateCanvases();
        int count = 0;
        foreach (var button in UnityEngine.Object.FindObjectsByType<Button>()) {
            var text = button.GetComponentInChildren<TMPro.TMP_Text>();
            if (text == null) continue;
            text.ForceMeshUpdate();
            var bounds = text.textBounds;
            var rect = (RectTransform)button.transform;
            var min = rect.InverseTransformPoint(text.transform.TransformPoint(bounds.min));
            var max = rect.InverseTransformPoint(text.transform.TransformPoint(bounds.max));
            Check(min.x >= rect.rect.xMin - 3 && max.x <= rect.rect.xMax + 3,
                "Rendered label must fit its button: " + button.name);
            count++;
        }
        Check(count > 0, "At least one labelled button must be visible.");
        return "PASS: " + count + " rendered labels fit their buttons at " + Screen.width + "x" + Screen.height + ".";
    }

    public static string ControlsFitScreen() {
        Check(Application.isPlaying, "Run in Play Mode with a menu visible.");
        Canvas.ForceUpdateCanvases();
        int count = 0;
        foreach (var control in UnityEngine.Object.FindObjectsByType<Selectable>()) {
            var canvas = control.GetComponentInParent<Canvas>();
            if (canvas == null) continue;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var corners = new Vector3[4];
            ((RectTransform)control.transform).GetWorldCorners(corners);
            foreach (var corner in corners) {
                var p = RectTransformUtility.WorldToScreenPoint(camera, corner);
                Check(p.x >= -1 && p.y >= -1 && p.x <= Screen.width + 1 && p.y <= Screen.height + 1,
                    "Control must fit the screen: " + control.name);
            }
            count++;
        }
        Check(count > 0, "At least one control must be visible.");
        return "PASS: " + count + " controls fit " + Screen.width + "x" + Screen.height + ".";
    }

    private static void Check(bool condition, string message) {
        if (!condition) throw new Exception("UI consistency: " + message);
    }
}
