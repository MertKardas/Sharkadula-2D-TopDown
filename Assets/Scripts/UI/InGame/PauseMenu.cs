using VContainer;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour, IGamePanel {
    private GameService _game;

    [Inject]
    public void Construct(GameService game) {
        _game = game;
    }

    [SerializeField] public string mainMenuSceneName;
    [SerializeField] Button selectedButton;
    private GameUI gameUI;
    public Button SelectedButton => selectedButton;

    public void OpenSocialLink(string url) {
        try {
            Application.OpenURL(url);
        } catch (System.Exception e) {
            Debug.LogError("Failed to open URL: " + e.Message);
        }
    }

    public void ReturnMainMenu() {
        _game.ReturnToMainMenu();
    }

    public void QuitGame() { _game.QuitGame(); }

    public void ResumeGame() {
        gameUI.ResumeGame();
    }

    public void PauseToSettings() {
        gameUI.OpenSettings();
    }

    void IGamePanel.SetPanelController(GameUI controller) {
        gameUI = controller;
    }
}
