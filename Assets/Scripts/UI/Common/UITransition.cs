using PrimeTween;
using UnityEngine;

[CreateAssetMenu(fileName = "UITransition", menuName = "UI/Transitions/New UI Transition")]
public class UITransition : ScriptableObject, IPanelTransition {
    [SerializeField, Min(0f)] float time = 0.5f;
    [SerializeField] Ease fromEaseType = Ease.InOutQuad;
    [SerializeField] Ease toEaseType = Ease.InOutQuad;
    [SerializeField] bool useUnscaledTime = true;

    public Sequence Transition(GameObject from, GameObject to) {
        if (from == to) return default;
        var fromGroup = from != null ? from.GetComponent<CanvasGroup>() : null;
        var toGroup = to != null ? to.GetComponent<CanvasGroup>() : null;
        if ((from != null && fromGroup == null) || (to != null && toGroup == null)) {
            Debug.LogError("UITransition requires a CanvasGroup on each panel.", this);
            return default;
        }

        var sequence = Sequence.Create(useUnscaledTime: useUnscaledTime);
        if (fromGroup != null) {
            fromGroup.interactable = false;
            fromGroup.blocksRaycasts = false;
            sequence.Chain(Tween.Alpha(fromGroup, 0f, Mathf.Max(0f, time), fromEaseType)
                .OnComplete(fromGroup, group => group.gameObject.SetActive(false)));
        }
        if (toGroup != null) {
            toGroup.alpha = 0f;
            toGroup.interactable = false;
            toGroup.blocksRaycasts = false;
            to.SetActive(true);
            sequence.Chain(Tween.Alpha(toGroup, 1f, Mathf.Max(0f, time), toEaseType)
                .OnComplete(toGroup, group => {
                    group.interactable = true;
                    group.blocksRaycasts = true;
                }));
        }
        return sequence;
    }
}
