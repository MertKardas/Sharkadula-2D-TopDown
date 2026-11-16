using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsPanelUI : MonoBehaviour {
    [SerializeField] private PlayerController playerController;
    [Header("UI References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI coinText;

    private PlayerHealthService _healthComp;
    private PlayerInventoryService _inventoryComp;
    private Tween _healthTween;

    private void Start() {
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
        if (playerController == null || healthSlider == null || healthText == null || coinText == null) {
            Debug.LogError("StatsPanelUI references are incomplete.", this);
            enabled = false;
            return;
        }
        _healthComp = playerController.Health;
        _inventoryComp = playerController.Inventory;
        BindAndRefresh();
    }

    private void OnEnable() {
        if (_healthComp != null) BindAndRefresh();
    }

    private void BindAndRefresh() {
        _healthComp.OnHealthChanged += OnHealthChangedHandler;
        _inventoryComp.OnCoinChanged += UpdateCoinUI;
        healthSlider.maxValue = _healthComp.MaxHealth;
        UpdateHealthUI(_healthComp.Health, false);
        UpdateCoinUI(_inventoryComp.Coin);
    }

    private void OnDisable() {
        _healthTween.Stop();
        if (_healthComp != null) _healthComp.OnHealthChanged -= OnHealthChangedHandler;
        if (_inventoryComp != null) _inventoryComp.OnCoinChanged -= UpdateCoinUI;
    }

    private void OnHealthChangedHandler(int currentHealth) {
        UpdateHealthUI(currentHealth, true);
    }

    private void UpdateHealthUI(int currentHealth, bool animate) {
        _healthTween.Stop();
        healthSlider.maxValue = _healthComp.MaxHealth;
        healthText.SetText($"{currentHealth} / {_healthComp.MaxHealth}");
        if (animate) {
            _healthTween = Tween.Custom(healthSlider, healthSlider.value, currentHealth, 0.5f,
                (slider, value) => slider.value = value, Ease.OutCubic, useUnscaledTime: true);
        } else {
            healthSlider.value = currentHealth;
        }
    }

    private void UpdateCoinUI(int currentCoin) {
        coinText.SetText($"{currentCoin}");
    }
}
