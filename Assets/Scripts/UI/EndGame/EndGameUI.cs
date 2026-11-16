using UnityEngine;
using VContainer;

public class EndGameUI : MonoBehaviour {
    private GameService _game;
    private bool _started;

    [Inject]
    public void Construct(GameService game) { _game = game; }

    private void OnEnable() {
        if (_started) UISelection.Select(gameObject);
    }

    private void Start() {
        _started = true;
        UISelection.Select(gameObject);
    }

    public void ReturnToMainMenu() { _game.ReturnToMainMenu(); }
}
