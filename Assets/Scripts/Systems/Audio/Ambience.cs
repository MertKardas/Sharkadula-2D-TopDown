using VContainer;
using UnityEngine;
using UnityEngine.Audio;

public class Ambience : MonoBehaviour
{
    private AudioService _audio;

    [Inject]
    public void Construct(AudioService audio) {
        _audio = audio;
    }

    [SerializeField] AudioData ambienceData; 
    private void Start() {
        _audio.PlaySound(ambienceData);
    }

}
