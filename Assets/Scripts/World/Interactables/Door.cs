using VContainer;
using System;
using UnityEngine;

public class Door : InteractableBase, ISavable
{
    private AudioService _audio;
    private GameService _game;

    [Inject]
    public void Construct(AudioService audio, GameService game) {
        _audio = audio;
        _game = game;
    }


    [Header("Door Audio")]
    [SerializeField] private AudioData _lockedDoorAudio;
    [SerializeField] private AudioData _unlockDoorAudio;
    [SerializeField] private AudioData _openDoorAudio;
    [SerializeField] private GameObject _highlightObject;
    [SerializeField] private ItemData _keyItem;
    private string _keyItemId => _keyItem != null ? _keyItem.ID : string.Empty;

    private bool _isLocked = true;

    public override bool CanInteract => base.CanInteract;
    public System.Type StateType => typeof(DoorSaveData);

    public override void Interact(PlayerController player)
    {
        if (!_isLocked)
        {
            if(_openDoorAudio != null)
                _audio.PlaySound3D(_openDoorAudio, transform.position);
            _game.EndGame();
            return;
        }

        // Kapı kilitli - anahtar kontrolü yap
        if (player.Inventory.HasItem(_keyItemId, 1))
        {
            if(_unlockDoorAudio != null)
                _audio.PlaySound3D(_unlockDoorAudio, transform.position);
            Unlock();
            player.Inventory.RemoveItem(_keyItemId, 1);
        }
        else
        {
            if(_lockedDoorAudio != null)
                _audio.PlaySound3D(_lockedDoorAudio, transform.position);
        }
    }
    protected override void OnRangeChanged(bool inRange)   
    {
        _highlightObject.SetActive(inRange);
    }
    public void Unlock() => _isLocked = false;
    public void Lock() => _isLocked = true;

    public object CaptureState()
    {
        return new DoorSaveData
        {
            IsLocked = _isLocked
        };
    }

    public void RestoreState(object data)
    {
        if (data is DoorSaveData saveData && saveData != null)
        {
            _isLocked = saveData.IsLocked;
        }
    }
}

[Serializable]
public class DoorSaveData
{
    public bool IsLocked;
}
