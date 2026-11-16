using System;
using UnityEngine;

public sealed class PlayerHealthService {
    public int Health { get; private set; }
    public int MaxHealth { get; private set; }
    public event Action<int> OnHealthChanged;
    public event Action OnDeath;
    public event Action OnTakingDamage;

    public PlayerHealthService(int maxHealth) {
        if (maxHealth <= 0) throw new ArgumentOutOfRangeException(nameof(maxHealth));
        Health = MaxHealth = maxHealth;
    }

    public void TakeDamage(int damage) {
        if (damage <= 0 || Health <= 0) return;
        Health = Mathf.Max(Health - damage, 0);
        OnHealthChanged?.Invoke(Health);
        if (Health == 0) OnDeath?.Invoke();
        else OnTakingDamage?.Invoke();
    }

    public void Restore(int health, int maxHealth) {
        if (maxHealth <= 0 || health < 0 || health > maxHealth)
            throw new ArgumentOutOfRangeException(nameof(health), "Invalid saved health.");
        Health = health;
        MaxHealth = maxHealth;
        OnHealthChanged?.Invoke(Health);
    }
}
