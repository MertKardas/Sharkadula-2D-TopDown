using UnityEngine;

public class HealthComponent : MonoBehaviour, IDamageable {
    private PlayerHealthService _service;

    public void Initialize(PlayerHealthService service) {
        _service = service;
    }

    public void TakeDamage(int damage) => _service.TakeDamage(damage);
}
