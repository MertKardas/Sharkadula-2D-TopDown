using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class InteractionComponent : MonoBehaviour {
    private PlayerInteractionService _service;
    private PlayerController _player;

    public void Initialize(PlayerInteractionService service, PlayerController player, float radius) {
        _service = service;
        _player = player;
        GetComponent<CircleCollider2D>().radius = radius;
    }

    public void InteractAction() => _service.Interact(transform.position, _player);
    private void OnTriggerEnter2D(Collider2D collision) => _service.Enter(collision);
    private void OnTriggerExit2D(Collider2D collision) => _service.Exit(collision);
    private void OnDisable() => _service?.Clear();
}
