using UnityEngine;

public class IdleState : State<PlayerController> {
    public IdleState(PlayerController controller, StateMachine<PlayerController> machine)
        : base(controller, machine) { }

    public override void Enter() {
        _controller.VisualComponent.SetRunning(false);
        _controller.VisualComponent.AnimationDirection(Vector2.zero);
    }
}
