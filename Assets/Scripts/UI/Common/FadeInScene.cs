using PrimeTween;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class FadeInScene : MonoBehaviour {
    private Tween _fade;

    private void OnEnable() {
        var canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        _fade = Tween.Alpha(canvasGroup, 0f, 1f, Ease.InOutQuad, useUnscaledTime: true)
            .OnComplete(this, fade => fade.gameObject.SetActive(false));
    }

    private void OnDisable() {
        _fade.Stop();
    }
}
