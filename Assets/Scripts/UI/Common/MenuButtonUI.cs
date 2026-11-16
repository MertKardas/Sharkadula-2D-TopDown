using VContainer;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MenuButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler {
    private AudioService _audio;

    [Inject]
    public void Construct(AudioService audio) {
        _audio = audio;
    }

    [Header("Sounds")]
    [SerializeField] AudioData _hoverSound;
    [SerializeField] AudioData _pressedSound;
    [Header("Settings")]
    [SerializeField] private float _hoverScale = 1.1f;
    [SerializeField, Min(0f)] private float _tweenDuration = 0.2f;

    private Vector3 _originalScale;
    private Tween _scaleTween;
    protected Button _button;

    protected void Awake() {
        _button = GetComponent<Button>();
        _originalScale = transform.localScale;
    }

    protected virtual void OnEnable() {
        transform.localScale = _originalScale;
        _button.onClick.AddListener(ButtonOnClick);
    }

    protected virtual void OnDisable() {
        _scaleTween.Stop();
        transform.localScale = _originalScale;
        _button.onClick.RemoveListener(ButtonOnClick);
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (!_button.IsInteractable()) return;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void OnSelect(BaseEventData eventData) {
        if (!_button.IsInteractable()) return;
        AnimateScale(_originalScale * _hoverScale);
        if (_hoverSound != null)
            _audio.PlaySound(_hoverSound);
    }

    public void OnDeselect(BaseEventData eventData) {
        if (isActiveAndEnabled) AnimateScale(_originalScale);
    }

    private void AnimateScale(Vector3 scale) {
        _scaleTween.Stop();
        if (transform.localScale == scale) return;
        _scaleTween = Tween.Scale(transform, scale, _tweenDuration, Ease.OutBack, useUnscaledTime: true);
    }

    public virtual void ButtonOnClick() {
        if (_button.IsInteractable() && _pressedSound != null)
            _audio.PlaySound(_pressedSound);
    }
}
