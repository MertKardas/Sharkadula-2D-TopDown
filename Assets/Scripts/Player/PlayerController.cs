using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using VContainer;

[RequireComponent(typeof(SaveableEntity))]
public class PlayerController : MonoBehaviour, ISavable {
    [field: Header("Components")]
    [field: SerializeField, FormerlySerializedAs("AttackComponent")]
    public AttackComponent AttackComponent { get; private set; }
    [field: SerializeField, FormerlySerializedAs("InteractionComponent")]
    public InteractionComponent InteractionComponent { get; private set; }
    [field: SerializeField, FormerlySerializedAs("HealthComponent")]
    public HealthComponent HealthComponent { get; private set; }
    [field: SerializeField, FormerlySerializedAs("MovementComponent")]
    public MovementComponent MovementComponent { get; private set; }
    [field: SerializeField, FormerlySerializedAs("VisualComponent")]
    public PlayerVisualComponent VisualComponent { get; private set; }
    [field: SerializeField, FormerlySerializedAs("Rigidbody")]
    public Rigidbody2D Rigidbody { get; private set; }

    [SerializeField] private PlayerConfiguration configuration;
    public PlayerConfiguration Configuration => configuration;
    public PlayerMovementService Movement => _services.Movement;
    public PlayerHealthService Health => _services.Health;
    public PlayerInventoryService Inventory => _services.Inventory;
    public PlayerInteractionService Interaction => _services.Interaction;

    public StateMachine<PlayerController> Machine { get; private set; }
    public Vector2 MoveInput { get; private set; }
    public bool RunRequested { get; private set; }
    public Type StateType => typeof(PlayerSaveData);
    internal StateData CurrentStateData => _pendingStateData ?? Machine.GetCurrentStateData();

    private InputService _input;
    private SceneSaveContext _saves;
    private GameService _game;
    private ItemCatalog _catalog;
    private PlayerServices _services;
    private PlayerSaveAdapter _saveAdapter;
    private StateData _pendingStateData;
    private bool _initialized;

    [Inject]
    public void Construct(InputService input, SceneSaveContext saves, GameService game, ItemCatalog catalog) {
        _input = input;
        _saves = saves;
        _game = game;
        _catalog = catalog;
    }

    private void Awake() {
        try {
            GetComponent<SaveableEntity>().SetUniqueId(GameSaveService.PlayerId);
            _services = new PlayerServices(configuration, _catalog);
            MovementComponent.Initialize(Rigidbody, Movement);
            HealthComponent.Initialize(Health);
            VisualComponent.Initialize(configuration);
            InteractionComponent.Initialize(Interaction, this, configuration.InteractionRadius);
            _saveAdapter = new PlayerSaveAdapter(this, _saves);

            Machine = new StateMachine<PlayerController>();
            Machine.AddState(new LocomotionState(this, Machine));
            Machine.AddState(new TakingDamageState(this, Machine));
            Machine.AddState(new DeathState(this, Machine));
            _initialized = true;
        } catch (Exception error) {
            _saves.ReportInitializationError("Player initialization failed: " + error.Message);
            enabled = false;
        }
    }

    private void OnEnable() {
        if (!_initialized) return;
        Health.OnTakingDamage += OnTakingDamage;
        Health.OnDeath += OnDeath;
        VisualComponent.OnFinishDeathAnimation += OnDeathAnimationFinished;
        _input.Subscribe(InputType.Interact, OnInteract);

        var pending = _pendingStateData;
        _pendingStateData = null;
        if (Health.Health <= 0)
            Machine.SetState(Machine.GetState<DeathState>());
        else if (pending != null)
            Machine.RestoreState(pending);
        else
            Machine.CurrentState?.Enter();
    }

    private void Start() {
        if (Machine.CurrentState == null)
            Machine.SetState(Machine.GetState<LocomotionState>());
    }

    private void Update() {
        MoveInput = _input.ReadInput<Vector2>(InputType.Move);
        if (MoveInput.sqrMagnitude < Movement.InputDeadzone * Movement.InputDeadzone) MoveInput = Vector2.zero;
        RunRequested = _input.ReadInput<float>(InputType.Run) > 0.5f;
        Machine.Update();

        if (Machine.CurrentState is LocomotionState && Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
            _saves.QuickSave();
    }

    private void FixedUpdate() {
        Machine.FixedUpdate();
    }

    private void OnDisable() {
        if (!_initialized) return;
        Health.OnTakingDamage -= OnTakingDamage;
        Health.OnDeath -= OnDeath;
        VisualComponent.OnFinishDeathAnimation -= OnDeathAnimationFinished;
        _input.Unsubscribe(InputType.Interact, OnInteract);
        Machine.CurrentState?.Exit();
        MoveInput = Vector2.zero;
        RunRequested = false;
    }

    private void OnInteract(InputAction.CallbackContext context) {
        if (context.performed && Machine.CurrentState is LocomotionState && InteractionComponent.isActiveAndEnabled)
            InteractionComponent.InteractAction();
    }

    private void OnTakingDamage() {
        if (Machine.CurrentState is LocomotionState)
            Machine.SetState(Machine.GetState<TakingDamageState>());
    }

    private void OnDeath() {
        if (Machine.CurrentState is not DeathState)
            Machine.SetState(Machine.GetState<DeathState>());
    }

    private void OnDeathAnimationFinished() {
        if (Machine.CurrentState is DeathState) _game.Gameover();
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (!collision.TryGetComponent<ICollectable>(out var collectable)) return;
        int added = Inventory.AddItem(collectable.Collect(this));
        if (collectable is DropObject drop) drop.OnItemsCollected(added);
    }

    public object CaptureState() => _saveAdapter.Capture();

    public void RestoreState(object data) {
        if (data is PlayerSaveData state) _saveAdapter.Restore(state);
    }

    internal void RestoreMachineState(StateData state) {
        if (!isActiveAndEnabled)
            _pendingStateData = state;
        else if (Health.Health <= 0)
            OnDeath();
        else if (state != null)
            Machine.RestoreState(state);
    }
}
