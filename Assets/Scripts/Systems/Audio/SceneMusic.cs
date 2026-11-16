using VContainer;
using UnityEngine;

public class SceneMusic : MonoBehaviour
{
    [Inject]
    public void Construct(AudioService audio) {
        _audioManager = audio;
    }

    [SerializeField]private AudioClip _musicData;
    private AudioService _audioManager;
    private bool _isQuitting;
       void Start()
    {

        _audioManager.PlayMusic(_musicData);
    }
    void OnApplicationQuit() {
        _isQuitting = true;
    }
    void OnDestroy()
    {
        float fadeOutDuration = 2f; // Duration of the fade-out in seconds
        if (!_isQuitting && Application.isPlaying && _audioManager != null)
            _audioManager.FadeOutMusic(fadeOutDuration);
    }
}
