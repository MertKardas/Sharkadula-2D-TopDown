using VContainer;
using PrimeTween;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class GameSaveTextUI : MonoBehaviour {
    [Inject]
    public void Construct(GameSaveService saves) {
        _saveManager = saves;
    }

    private TMP_Text _saveText;
    private GameSaveService _saveManager;
    private Sequence _notification;

    private void Awake() {
        _saveText = GetComponent<TMP_Text>();
    }

    private void OnEnable() {

        _saveManager.OnSaveAfter += UpdateSaveText;
    }

    private void OnDisable() {
        _notification.Stop();
        _saveText.text = "";
        if (_saveManager != null) _saveManager.OnSaveAfter -= UpdateSaveText;
    }

    private void UpdateSaveText(Result result) {
        _notification.Stop();
        if (!result.Success) {
            _saveText.maxVisibleCharacters = int.MaxValue;
            _saveText.text = "Game save failed: " + result.ErrorMessage;
            return;
        }
        const string fullText = "Game saved successfully.";
        _saveText.text = fullText;
        _saveText.maxVisibleCharacters = 0;
        _notification = Sequence.Create(useUnscaledTime: true)
            .Chain(Tween.Custom(_saveText, 0f, fullText.Length, 0.5f,
                (text, value) => text.maxVisibleCharacters = Mathf.FloorToInt(value), Ease.Linear))
            .ChainDelay(0.5f)
            .ChainCallback(_saveText, text => text.text = "");
    }
}
