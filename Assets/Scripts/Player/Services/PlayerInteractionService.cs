using System.Collections.Generic;
using UnityEngine;
public sealed class PlayerInteractionService
{
    // shortcut: Destroyed collider entries live until Clear; prune if long-lived sensors accumulate targets.
    private readonly Dictionary<Collider2D, IInteractable> _targets = new Dictionary<Collider2D, IInteractable>();
    public void Interact(Vector2 position, PlayerController player) {
        IInteractable closest = null;
        float closestDistance = float.PositiveInfinity;
        foreach (var target in _targets) {
            if (target.Key == null || !target.Key.enabled || !target.Key.gameObject.activeInHierarchy) continue;
            if (target.Value is not MonoBehaviour behaviour || behaviour == null || !behaviour.isActiveAndEnabled) continue;
            if (!target.Value.CanInteract) continue;
            float distance = ((Vector2)(behaviour.transform.position - (Vector3)position)).sqrMagnitude;
            if (distance >= closestDistance) continue;
            closestDistance = distance;
            closest = target.Value;
        }

        if (closest != null) {
            closest.Interact(player);
        }
    }

    public void Enter(Collider2D collision) {
        if (collision.TryGetComponent<IInteractable>(out IInteractable interactable)) {
            interactable.SetInRange(true);
            _targets[collision] = interactable;
        }
    }

    public void Exit(Collider2D collision) {
        if (_targets.TryGetValue(collision, out var interactable)) {
            _targets.Remove(collision);
            if (!_targets.ContainsValue(interactable) && interactable is MonoBehaviour behaviour && behaviour != null)
                interactable.SetInRange(false);
        }
    }

    public void Clear() {
        foreach (var target in _targets.Values) {
            if (target is MonoBehaviour behaviour && behaviour != null) target.SetInRange(false);
        }
        _targets.Clear();
    }
}
