using UnityEngine;

public sealed class PlayerMovementService {
    public float MaxSpeed { get; set; }
    public float RunSpeed { get; }
    public float Acceleration { get; set; }
    public float Deceleration { get; set; }
    public float InputDeadzone { get; }
    public float IdleSpeedThreshold { get; }

    public PlayerMovementService(PlayerConfiguration configuration) {
        MaxSpeed = configuration.WalkSpeed;
        RunSpeed = configuration.RunSpeed;
        Acceleration = configuration.Acceleration;
        Deceleration = configuration.Deceleration;
        InputDeadzone = configuration.InputDeadzone;
        IdleSpeedThreshold = configuration.IdleSpeedThreshold;
    }

    public Vector2 CalculateVelocity(Vector2 current, Vector2 input, bool running, float deltaTime) {
        bool hasInput = input != Vector2.zero;
        float speed = running ? RunSpeed : MaxSpeed;
        Vector2 target = hasInput ? input * speed : Vector2.zero;
        float rate = hasInput ? Acceleration : Deceleration;
        return Vector2.MoveTowards(current, target, rate * deltaTime);
    }
}
