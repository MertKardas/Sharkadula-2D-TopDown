using UnityEngine;

public class MovementComponent : MonoBehaviour {
    private Rigidbody2D _rigidbody;
    private PlayerMovementService _service;

    public void Initialize(Rigidbody2D rigidbody, PlayerMovementService service) {
        _rigidbody = rigidbody;
        _service = service;
    }

    public void MoveCharacter(Vector2 input) => Move(input, false);
    public void RunCharacter(Vector2 input) => Move(input, true);

    private void Move(Vector2 input, bool running) {
        _rigidbody.linearVelocity = _service.CalculateVelocity(_rigidbody.linearVelocity, input, running, Time.fixedDeltaTime);
    }
}
