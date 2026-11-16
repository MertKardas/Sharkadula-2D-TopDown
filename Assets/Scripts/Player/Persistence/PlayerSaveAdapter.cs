using UnityEngine;

public sealed class PlayerSaveAdapter {
    private readonly PlayerController _player;
    private readonly SceneSaveContext _saves;

    public PlayerSaveAdapter(PlayerController player, SceneSaveContext saves) {
        _player = player;
        _saves = saves;
    }

    public PlayerSaveData Capture() {
        return new PlayerSaveData {
            StateData = _player.CurrentStateData,
            position = _player.transform.position,
            velocity = _player.Rigidbody.linearVelocity,
            acceleration = _player.Movement.Acceleration,
            deceleration = _player.Movement.Deceleration,
            MaxSpeed = _player.Movement.MaxSpeed,
            health = _player.Health.Health,
            maxHealth = _player.Health.MaxHealth,
            coin = _player.Inventory.Coin,
            items = _player.Inventory.CaptureInventory()
        };
    }

    public void Restore(PlayerSaveData data) {
        bool atEntrance = _saves.RestorePlayerAtEntrance;
        if (!atEntrance) _player.transform.position = new Vector3(data.position.x, data.position.y, 0);
        _player.Rigidbody.linearVelocity = atEntrance ? Vector2.zero : data.velocity;
        _player.Movement.Acceleration = data.acceleration;
        _player.Movement.Deceleration = data.deceleration;
        _player.Movement.MaxSpeed = data.MaxSpeed;
        _player.Health.Restore(data.health, data.maxHealth);
        _player.Inventory.LoadInventory(data.items);
        _player.Inventory.SetCoin(data.coin);
        _player.RestoreMachineState(atEntrance ? null : data.StateData);
    }
}
