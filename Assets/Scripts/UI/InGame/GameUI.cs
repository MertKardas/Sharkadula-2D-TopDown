using VContainer;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameUI : MonoBehaviour {
    [Inject]
    public void Construct(GameService game, InputService input) {
        _gameManager = game;
        _inputManager = input;
    }

    public StatsPanelUI StatsPanel;
    public DeathMenuUI PlayerDeathPanel;
    public PauseMenu PauseMenu;
    public SettingsMenu SettingsPanel;
    public GameObject backgroundPanel;
    public GameObject EndgGamePanel;
    public UITransition panelTransition;

    private GameService _gameManager;
    private InputService _inputManager;
    private GameObject _currentPanel;
    private Sequence _panelSequence;
    private Tween _sceneFade;
    private bool _started;
    private bool _ending;

    private void Awake() {
        if (StatsPanel == null || PlayerDeathPanel == null || PauseMenu == null ||
            SettingsPanel == null || backgroundPanel == null || EndgGamePanel == null) {
            Debug.LogError("GameUI panel references are incomplete.", this);
            enabled = false;
            return;
        }
        ((IGamePanel)PauseMenu).SetPanelController(this);
        ((IGamePanel)SettingsPanel).SetPanelController(this);
    }

    private void OnEnable() {

        _gameManager.OnGameover += OnGameover;
        _gameManager.OnGameEnded += OnGameEnded;
        _inputManager.Subscribe(InputType.Cancel, OnCancel, InputActionPhase.Started);
        if (_started) InitializePanels();
    }

    private void Start() {
        _started = true;
        InitializePanels();
    }

    private void InitializePanels() {
        _ending = false;
        _currentPanel = _gameManager.CurrentGameState == GameState.Gameover
            ? PlayerDeathPanel.gameObject
            : _gameManager.CurrentGameState == GameState.Paused ? PauseMenu.gameObject : StatsPanel.gameObject;
        NormalizePanels(_currentPanel);
        SetVisible(EndgGamePanel, false);
        backgroundPanel.SetActive(_currentPanel != StatsPanel.gameObject);
        SelectPanel(_currentPanel);
    }

    private void OnDisable() {
        _panelSequence.Stop();
        _sceneFade.Stop();
        if (_gameManager != null) {
            _gameManager.OnGameover -= OnGameover;
            _gameManager.OnGameEnded -= OnGameEnded;
        }
        if (_inputManager != null)
            _inputManager.Unsubscribe(InputType.Cancel, OnCancel, InputActionPhase.Started);
    }

    private void OnCancel(InputAction.CallbackContext ctx) {
        if (!ctx.started || _ending || _panelSequence.isAlive) return;
        if (_gameManager.CurrentGameState == GameState.Gameover || _gameManager.CurrentGameState == GameState.EndGame) return;
        if (_currentPanel == StatsPanel.gameObject) OpenPauseMenu();
        else if (_currentPanel == PauseMenu.gameObject) ResumeGame();
        else if (_currentPanel == SettingsPanel.gameObject) ReturnToPauseMenu();
    }

    public void OpenPauseMenu() {
        if (_ending || _panelSequence.isAlive || _currentPanel != StatsPanel.gameObject ||
            _gameManager.CurrentGameState != GameState.Playing) return;
        if (!_gameManager.PauseGame()) return;
        ShowPanel(PauseMenu.gameObject);
    }

    public void ResumeGame() {
        if (_ending || _panelSequence.isAlive || _currentPanel != PauseMenu.gameObject ||
            _gameManager.CurrentGameState != GameState.Paused) return;
        ShowPanel(StatsPanel.gameObject);
    }

    public void OpenSettings() {
        if (_ending || _panelSequence.isAlive || _currentPanel != PauseMenu.gameObject) return;
        ShowPanel(SettingsPanel.gameObject);
    }

    public void ReturnToPauseMenu() {
        if (_ending || _panelSequence.isAlive || _currentPanel != SettingsPanel.gameObject) return;
        ShowPanel(PauseMenu.gameObject);
    }

    private void OnGameover() {
        if (!_ending) ShowPanel(PlayerDeathPanel.gameObject);
    }

    private void ShowPanel(GameObject target) {
        _panelSequence.Stop();
        NormalizePanels(_currentPanel);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        backgroundPanel.SetActive(true);
        var previous = _currentPanel;
        _currentPanel = target;
        _panelSequence = panelTransition != null ? panelTransition.Transition(previous, target) : default;
        if (_panelSequence.isAlive) _panelSequence.OnComplete(this, ui => ui.CompletePanelTransition());
        else CompletePanelTransition();
    }

    private void CompletePanelTransition() {
        NormalizePanels(_currentPanel);
        bool playing = _currentPanel == StatsPanel.gameObject;
        backgroundPanel.SetActive(!playing);
        if (playing && _gameManager.CurrentGameState == GameState.Paused) _gameManager.UnpauseGame();
        SelectPanel(_currentPanel);
    }

    private void NormalizePanels(GameObject visiblePanel) {
        SetVisible(StatsPanel.gameObject, visiblePanel == StatsPanel.gameObject);
        SetVisible(PauseMenu.gameObject, visiblePanel == PauseMenu.gameObject);
        SetVisible(SettingsPanel.gameObject, visiblePanel == SettingsPanel.gameObject);
        SetVisible(PlayerDeathPanel.gameObject, visiblePanel == PlayerDeathPanel.gameObject);
    }

    private static void SetVisible(GameObject panel, bool visible) {
        var group = panel.GetComponent<CanvasGroup>();
        if (group != null) {
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
        panel.SetActive(visible);
    }

    private void SelectPanel(GameObject panel) {
        Selectable selection = panel == PauseMenu.gameObject ? PauseMenu.SelectedButton : null;
        UISelection.Select(panel, selection);
    }

    private void OnGameEnded() {
        var fade = FadeOutScene();
        if (fade.isAlive) fade.OnComplete(_gameManager, game => game.CompleteEndGame());
        else _gameManager.CompleteEndGame();
    }

    public Tween FadeOutScene() {
        _ending = true;
        _panelSequence.Stop();
        _sceneFade.Stop();
        NormalizePanels(null);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        backgroundPanel.SetActive(true);
        SetVisible(EndgGamePanel, true);
        var group = EndgGamePanel.GetComponent<CanvasGroup>();
        if (group == null) {
            Debug.LogError("FadeOutPanel requires a CanvasGroup.", this);
            return default;
        }
        group.alpha = 0f;
        _sceneFade = Tween.Alpha(group, 1f, 1f, Ease.InOutSine, useUnscaledTime: true);
        return _sceneFade;
    }
}
