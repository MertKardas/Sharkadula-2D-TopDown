public class RunState : MoveState {
    protected override bool IsRunning => true;

    public RunState(PlayerController controller, StateMachine<PlayerController> machine)
        : base(controller, machine) { }
}
