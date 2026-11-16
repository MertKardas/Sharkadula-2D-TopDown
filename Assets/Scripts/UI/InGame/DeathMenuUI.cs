using VContainer;
using UnityEngine;
using UnityEngine.UI;

public class DeathMenuUI : MonoBehaviour
{
    private GameService _game;
    private GameSaveService _saves;

    [Inject]
    public void Construct(GameService game, GameSaveService saves) {
        _game = game;
        _saves = saves;
    }

    [SerializeField] private Button restartButton;

    private void OnEnable() {
        if (restartButton == null) return;
        var result = _saves.LoadAllMetadata(out var metadata);
        if (!result.Success) Debug.LogError(result.ErrorMessage);
        restartButton.gameObject.SetActive(result.Success && metadata.Length > 0);
    }

    public void RestartLevel()
    {
        _game.RestartGame();
    }
    public void ReturnToMainMenu()
    {
        _game.ReturnToMainMenu();
    }
    public void QuitGame() { _game.QuitGame(); }

}

