using UnityEngine;

public class DeathState : State<PlayerController> {
    public DeathState(PlayerController controller, StateMachine<PlayerController> machine)
        : base(controller, machine) { }

    public override void Enter() {
        _controller.Rigidbody.linearVelocity = Vector2.zero;
        _controller.VisualComponent.TriggerDeath();
    }
}
