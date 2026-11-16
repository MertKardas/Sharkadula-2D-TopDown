using PrimeTween;
using UnityEngine;

public class SquareHighlight : MonoBehaviour {
    [Header("Highlight Settings")]
    [SerializeField] private float scaleMultiplier = 1.1f;
    [SerializeField, Min(0f)] private float duration = 0.5f;
    [SerializeField] private Ease easeType = Ease.InOutSine;
    private Vector3 _originalScale;
    private Tween _highlightTween;

    private void Awake() {
        _originalScale = transform.localScale;
    }

    private void OnEnable() {
        _highlightTween = Tween.Scale(transform, _originalScale, _originalScale * scaleMultiplier,
            duration, easeType, cycles: -1, cycleMode: CycleMode.Yoyo, useUnscaledTime: true);
    }

    private void OnDisable() {
        _highlightTween.Stop();
        transform.localScale = _originalScale;
    }
}
