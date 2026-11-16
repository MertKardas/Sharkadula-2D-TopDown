using VContainer;
using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(CanvasGroup))]
public class SettingsMenu : MonoBehaviour, IGamePanel {
    private AudioService _audio;

    [Inject]
    public void Construct(AudioService audio) {
        _audio = audio;
    }

    [Header("Volume Sliders"), SerializeField]
    private Slider 
        masterVolumeSlider,
        musicVolumeSlider,
        sfxVolumeSlider,
        ambienceVolumeSlider,
        uiVolumeSlider;
    GameUI gameUI; 
    private void OnEnable()
    {
        InitializeSliders();

        masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        ambienceVolumeSlider.onValueChanged.AddListener(OnAmbienceVolumeChanged);
        uiVolumeSlider.onValueChanged.AddListener(OnUIVolumeChanged);
    }

    private void InitializeSliders() {
        masterVolumeSlider.SetValueWithoutNotify(_audio.GetVolume(AudioType.Master));
        musicVolumeSlider.SetValueWithoutNotify(_audio.GetVolume(AudioType.Music));
        sfxVolumeSlider.SetValueWithoutNotify(_audio.GetVolume(AudioType.SFX));
        ambienceVolumeSlider.SetValueWithoutNotify(_audio.GetVolume(AudioType.Ambience));
        uiVolumeSlider.SetValueWithoutNotify(_audio.GetVolume(AudioType.UI));
    }
    void IGamePanel.SetPanelController(GameUI controller) {
        gameUI = controller;
    }
    private void OnDisable()
    {
        
        masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
        musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
        ambienceVolumeSlider.onValueChanged.RemoveListener(OnAmbienceVolumeChanged);
        uiVolumeSlider.onValueChanged.RemoveListener(OnUIVolumeChanged);

    }
    #region Volume Change Handlers
    public void OnMasterVolumeChanged(float value)
    {
        _audio.SetVolume(AudioType.Master, value);
    }
    public void OnMusicVolumeChanged(float value)
    {
        _audio.SetVolume(AudioType.Music, value);
    }
    public void OnSFXVolumeChanged(float value)
    {
        _audio.SetVolume(AudioType.SFX, value);
    }
    public void OnAmbienceVolumeChanged(float value)
    {
        _audio.SetVolume(AudioType.Ambience, value);
    }
    public void OnUIVolumeChanged(float value)
    {
        _audio.SetVolume(AudioType.UI, value);
    }

    #endregion
    
    public void OnReturnClicked() {
        gameUI.ReturnToPauseMenu();
    }


}
