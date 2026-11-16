using UnityEngine;

public class TakingDamageState : State<PlayerController> {
    private readonly MovementComponent _movementComponent;
    private readonly PlayerVisualComponent _visual;
    public TakingDamageState(PlayerController controller, StateMachine<PlayerController> machine)
        : base(controller, machine)
    {
        _movementComponent = controller.MovementComponent;
        _visual = controller.VisualComponent;
    }

    public override void Enter()
    {
        _visual.OnFinishTakeDamage += OnFinishTakeDamage;
        _visual.TriggerTakeDamage();
    }

    public override void FixedUpdate()
    {
        _movementComponent.MoveCharacter(Vector2.zero);
    }

    public override void Exit()
    {
        _visual.OnFinishTakeDamage -= OnFinishTakeDamage;
    }
    private void OnFinishTakeDamage() {
        var state = _machine.GetState<LocomotionState>();
        _machine.SetState(state);
    }

}
