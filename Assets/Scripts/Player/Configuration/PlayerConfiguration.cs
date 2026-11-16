using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Sharkadula/Player Configuration", fileName = "PlayerConfiguration")]
public sealed class PlayerConfiguration : ScriptableObject {
    [Header("Movement")]
    [SerializeField, Min(0.01f)] private float walkSpeed = 2.5f;
    [SerializeField, Min(0.01f)] private float runSpeed = 3.75f;
    [SerializeField, Min(0.01f)] private float acceleration = 80f;
    [SerializeField, Min(0.01f)] private float deceleration = 100f;
    [SerializeField, Range(0.001f, 0.99f)] private float inputDeadzone = 0.1f;
    [SerializeField, Min(0.001f)] private float idleSpeedThreshold = 0.1f;
    [Header("Health and inventory")]
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField, Min(1)] private int inventorySlots = 5;
    [SerializeField, Min(0)] private int startingCoin;
    [Header("Interaction")]
    [SerializeField, Min(0.01f)] private float interactionRadius = 0.75f;
    [Header("Presentation")]
    [SerializeField, Min(0.01f)] private float runAnimationSpeedMultiplier = 1.5f;
    [SerializeField] private AudioData footstepClip;
    [SerializeField] private AudioData takeDamageClip;
    [SerializeField] private AudioData deathClip;
    [SerializeField] private AudioData attackClip;

    public float WalkSpeed => walkSpeed;
    public float RunSpeed => runSpeed;
    public float Acceleration => acceleration;
    public float Deceleration => deceleration;
    public float InputDeadzone => inputDeadzone;
    public float IdleSpeedThreshold => idleSpeedThreshold;
    public int MaxHealth => maxHealth;
    public int InventorySlots => inventorySlots;
    public int StartingCoin => startingCoin;
    public float InteractionRadius => interactionRadius;
    public float RunAnimationSpeedMultiplier => runAnimationSpeedMultiplier;
    public AudioData FootstepClip => footstepClip;
    public AudioData TakeDamageClip => takeDamageClip;
    public AudioData DeathClip => deathClip;
    public AudioData AttackClip => attackClip;

    public void Validate() {
        if (!Positive(walkSpeed) || !Positive(runSpeed) || !Positive(acceleration) || !Positive(deceleration)
            || !Positive(inputDeadzone) || inputDeadzone >= 1 || !Positive(idleSpeedThreshold)
            || maxHealth <= 0 || inventorySlots <= 0 || startingCoin < 0
            || !Positive(interactionRadius) || !Positive(runAnimationSpeedMultiplier))
            throw new InvalidOperationException("PlayerConfiguration contains invalid settings: " + name);
    }

    private static bool Positive(float value) => value > 0 && !float.IsInfinity(value);
}
