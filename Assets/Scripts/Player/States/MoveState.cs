public class MoveState : State<PlayerController> {
    protected virtual bool IsRunning => false;

    public MoveState(PlayerController controller, StateMachine<PlayerController> machine)
        : base(controller, machine) { }

    public override void Enter() {
        _controller.VisualComponent.SetRunning(IsRunning);
        _controller.VisualComponent.SetMoving(true);
    }

    public override void Update() {
        _controller.VisualComponent.AnimationDirection(_controller.MoveInput);
    }

    public override void FixedUpdate() {
        if (IsRunning)
            _controller.MovementComponent.RunCharacter(_controller.MoveInput);
        else
            _controller.MovementComponent.MoveCharacter(_controller.MoveInput);
    }

    public override void Exit() {
        _controller.VisualComponent.SetRunning(false);
    }
}
