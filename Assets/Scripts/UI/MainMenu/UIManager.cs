using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class UIManager : MonoBehaviour {
    [SerializeField] private string firstLevel;
    [SerializeField] private Button resumeGameButton;
    [SerializeField] private Button firstSelectedButton;
    private GameService _game;
    private GameSaveService _saves;
    private bool _started;

    [Inject]
    public void Construct(GameService game, GameSaveService saves) {
        _game = game;
        _saves = saves;
    }

    private void OnEnable() {
        var result = _saves.LoadAllMetadata(out var metadatas);
        if (!result.Success) Debug.LogError(result.ErrorMessage);
        resumeGameButton.interactable = result.Success && metadatas.Length > 0;
        if (_started) UISelection.Select(gameObject, firstSelectedButton);
    }

    private void Start() {
        _started = true;
        UISelection.Select(gameObject, firstSelectedButton);
    }

    public void LoadNewgameScene() { _game.StartNewGame(firstLevel); }

    public void ResumeGame() {
        var result = _game.ContinueGame();
        if (!result.Success) Debug.LogError("Failed to load save: " + result.ErrorMessage);
    }

    public void QuitGame() { _game.QuitGame(); }

    public void OpenURL(string url) {
        try {
            Application.OpenURL(url);
        } catch (System.Exception e) {
            Debug.LogError("Failed to open URL: " + e.Message);
        }
    }
}
