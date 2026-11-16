using System;

using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameService : IDisposable {
    public event Action OnGamePaused;
    public event Action OnGameStarted;
    public event Action OnGameEnded;
    public event Action OnGameover;
    public GameState CurrentGameState { get; private set; }
    private readonly InputService _input;
    private readonly AudioService _audio;
    private readonly GameSaveService _saves;

    private bool _isLoadingScene;
    internal bool IsLoadingScene => _isLoadingScene;
    private bool _disposed;

    public GameService(InputService input, AudioService audio, GameSaveService saves) {
        _input = input;
        _audio = audio;
        _saves = saves;


    }

    public bool PauseGame() {
        if (_isLoadingScene) return false;
        _input.DisablePlayerInput();
        Time.timeScale = 0f;
        CurrentGameState = GameState.Paused;
        OnGamePaused?.Invoke();
        return true;
    }

    public void UnpauseGame() {
        _input.EnablePlayerInput();
        Time.timeScale = 1f;
        CurrentGameState = GameState.Playing;
        OnGameStarted?.Invoke();
    }

    public void Gameover() {
        if (CurrentGameState == GameState.Gameover || CurrentGameState == GameState.EndGame) return;
        _input.DisablePlayerInput();
        Time.timeScale = 0f;
        CurrentGameState = GameState.Gameover;
        _audio.StopAudioByType(AudioType.Music);
        _audio.StopAudioByType(AudioType.Ambience);
        OnGameover?.Invoke();
    }

    public void EndGame() {
        if (CurrentGameState == GameState.EndGame || _isLoadingScene) return;
        _input.DisablePlayerInput();
        Time.timeScale = 0f;
        CurrentGameState = GameState.EndGame;
        if (OnGameEnded != null) OnGameEnded.Invoke();
        else CompleteEndGame();
    }

    public void CompleteEndGame() {
        if (CurrentGameState == GameState.EndGame) LoadScene("EndGame");
    }

    public void StartNewGame(string firstLevel) {
        if (_isLoadingScene || !CanLoadScene(firstLevel)) return;
        _saves.Clear();
        LoadScene(firstLevel);
    }

    public Result ChangeScene(string sceneName, Func<Result> collect) {
        if (_isLoadingScene || CurrentGameState != GameState.Playing)
            return Result.Fail("A gameplay scene transition is not available now.");
        if (!CanLoadScene(sceneName) || sceneName == "MainMenu" || sceneName == "EndGame")
            return Result.Fail("The target gameplay scene is not available.");
        if (collect == null) return Result.Fail("A scene collect callback is required.");
        var previousTimeScale = Time.timeScale;
        _isLoadingScene = true;
        _input.DisablePlayerInput();
        Time.timeScale = 0f;
        try {
            var result = collect();
            if (!result.Success) {
                _isLoadingScene = false;
                Time.timeScale = previousTimeScale;
                _input.EnablePlayerInput();
                return result;
            }
            _saves.BeginTransition();
            if (SceneManager.LoadSceneAsync(sceneName) == null) throw new InvalidOperationException("Could not start scene loading.");
            return Result.Ok();
        } catch (Exception error) {
            _isLoadingScene = false;
            _saves.CompleteTransition(SceneManager.GetActiveScene().name);
            Time.timeScale = previousTimeScale;
            _input.EnablePlayerInput();
            return Result.Fail("Could not change scene: " + error.Message);
        }
    }

    public Result ContinueGame() {
        if (_isLoadingScene) return Result.Fail("A scene is already loading.");
        var result = _saves.QuickLoad();
        if (!result.Success) return result;
        string scene = _saves.GetLastSavedSceneName();
        if (!CanLoadScene(scene)) return Result.Fail("The saved scene is not available.");
        LoadScene(scene);
        return Result.Ok();
    }

    public void RestartGame() {
        if (_isLoadingScene) return;
        var result = _saves.QuickLoad();
        string scene = _saves.GetLastSavedSceneName();
        if (result.Success && CanLoadScene(scene)) LoadScene(scene);
        else {
            Debug.LogWarning("Failed to load last save: " + result.ErrorMessage);
            _saves.Clear();
            LoadScene("MainMenu");
        }
    }

    public void ReturnToMainMenu() {
        if (_isLoadingScene) return;
        _audio.StopAllAudio();
        LoadScene("MainMenu");
    }

    public void QuitGame() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static bool CanLoadScene(string scene) {
        return !string.IsNullOrEmpty(scene) && Application.CanStreamedLevelBeLoaded(scene);
    }

    private void LoadScene(string scene) {
        if (_isLoadingScene) return;
        if (!CanLoadScene(scene)) {
            Debug.LogError("Scene is not available in the build: " + scene);
            return;
        }
        _isLoadingScene = true;
        SceneManager.LoadSceneAsync(scene);
    }

    internal void BeginScene(Scene scene) {
        if (scene.name == "MainMenu") _saves.Clear();
        _isLoadingScene = true;
        CurrentGameState = scene.name == "EndGame" ? GameState.EndGame : GameState.Playing;
        Time.timeScale = 0f;
        _input.DisablePlayerInput();
    }

    internal void CompleteScene(Scene scene, Result startup) {
        if (_disposed || !scene.isLoaded || scene != SceneManager.GetActiveScene()) return;
        _isLoadingScene = false;
        if (!startup.Success) {
            Debug.LogError("Could not start the loaded scene: " + startup.ErrorMessage);
            _saves.Clear();
            if (scene.name != "MainMenu") ReturnToMainMenu();
            return;
        }
        if (CurrentGameState == GameState.Gameover) return;
        _saves.CompleteTransition(scene.name);
        Time.timeScale = 1f;
        CurrentGameState = scene.name == "EndGame" ? GameState.EndGame : GameState.Playing;
        if (CurrentGameState == GameState.EndGame) _input.DisablePlayerInput();
        else _input.EnablePlayerInput();
        if (scene.name != "MainMenu") OnGameStarted?.Invoke();
    }
    public void Dispose() {
        _disposed = true;

        OnGamePaused = null;
        OnGameStarted = null;
        OnGameEnded = null;
        OnGameover = null;
    }
}

public enum GameState { Playing, Paused, Gameover, EndGame }
