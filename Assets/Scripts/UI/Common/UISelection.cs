using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class UISelection {
    public static void Select(GameObject panel, Selectable preferred = null) {
        if (EventSystem.current == null) return;
        Selectable selection = preferred;
        if (panel == null || selection == null || !selection.transform.IsChildOf(panel.transform) ||
            !selection.IsActive() || !selection.IsInteractable()) {
            selection = null;
            if (panel != null) {
                foreach (var candidate in panel.GetComponentsInChildren<Selectable>()) {
                    if (candidate.IsActive() && candidate.IsInteractable()) {
                        selection = candidate;
                        break;
                    }
                }
            }
        }
        EventSystem.current.SetSelectedGameObject(selection != null ? selection.gameObject : null);
    }
}
