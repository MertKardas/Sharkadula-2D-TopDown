using UnityEngine;

public class LocomotionState : HierarchicalState<PlayerController> {
    public LocomotionState(PlayerController controller, StateMachine<PlayerController> machine)
        : base(controller, machine) {
        AddSubState(new IdleState(controller, _subStateMachine));
        AddSubState(new MoveState(controller, _subStateMachine));
        AddSubState(new RunState(controller, _subStateMachine));
    }

    public override void Enter() {
        _subStateMachine.SetState(_subStateMachine.GetState<IdleState>());
    }

    public override void Exit() {
        _subStateMachine.CurrentState?.Exit();
    }

    public override void CheckTransitions() {
        State<PlayerController> next;
        if (_controller.MoveInput != Vector2.zero) {
            next = _controller.RunRequested
                ? _subStateMachine.GetState<RunState>()
                : _subStateMachine.GetState<MoveState>();
        } else if (_controller.Rigidbody.linearVelocity.sqrMagnitude < _controller.Movement.IdleSpeedThreshold * _controller.Movement.IdleSpeedThreshold) {
            next = _subStateMachine.GetState<IdleState>();
        } else {
            return;
        }

        if (_subStateMachine.CurrentState != next) _subStateMachine.SetState(next);
    }
}
