using System;
using System.Linq;
using UnityEngine;
using VContainer;

[DefaultExecutionOrder(-4000)]
[DisallowMultipleComponent]
public sealed class SaveableEntity : MonoBehaviour {
    [SerializeField] private string uniqueId;
    private SceneSaveContext _context;
    private ISavable _savable;

    public string UniqueId => uniqueId;
    public string PrefabId { get; private set; }
    public bool IsDynamic => !string.IsNullOrEmpty(PrefabId);
    public bool IsRemoved { get; private set; }
    public bool HasStarted { get; private set; }
    public string StartupError { get; private set; }

    [Inject]
    public void Construct(SceneSaveContext context) { _context = context; }

    private void OnEnable() {
        if (IsRemoved) gameObject.SetActive(false);
    }

    public ISavable GetSavable() {
        if (_savable != null) return _savable;
        var targets = GetComponents<MonoBehaviour>().OfType<ISavable>().ToArray();
        if (targets.Length != 1) throw new InvalidOperationException($"'{name}' must have exactly one ISavable.");
        return _savable = targets[0];
    }

    private void Start() {
        try {
            if (_context == null) throw new InvalidOperationException("Scene save context was not injected.");
            var savable = GetSavable();
            var record = _context.GetRecord(UniqueId);
            if (record != null && record.IsRemoved) {
                IsRemoved = true;
                HasStarted = true;
                gameObject.SetActive(false);
                return;
            }
            if (record != null) {
                var state = record.State;
                if (record.PrefabId != PrefabId && !(string.IsNullOrEmpty(record.PrefabId) && string.IsNullOrEmpty(PrefabId)))
                    throw new InvalidOperationException("Saved entity origin does not match: " + UniqueId);
                if (state == null || state.GetType() != savable.StateType)
                    throw new InvalidOperationException($"Saved state type does not match '{name}' ({UniqueId}).");
                savable.RestoreState(state);
            }
            HasStarted = true;
        } catch (Exception error) {
            StartupError = error.Message;
            Debug.LogError($"Could not initialize saveable '{name}' ({UniqueId}): {StartupError}", this);
            gameObject.SetActive(false);
        }
    }

    public void MarkRemoved() {
        if (IsRemoved) return;
        if (_context == null) throw new InvalidOperationException("Scene save context was not injected.");
        _context.MarkRemoved(this);
        IsRemoved = true;
        gameObject.SetActive(false);
    }

    public void SetUniqueId(string id) {
        if (HasStarted) throw new InvalidOperationException("Cannot change a started saveable's identity.");
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("An entity ID is required.", nameof(id));
        uniqueId = id;
    }

    public void InitializeDynamic(string prefabId, string id) {
        if (string.IsNullOrWhiteSpace(prefabId)) throw new ArgumentException("A prefab ID is required.", nameof(prefabId));
        if (IsDynamic) throw new InvalidOperationException("A dynamic identity can only be assigned once.");
        SetUniqueId(id);
        PrefabId = prefabId;
    }

    public void GenerateNewId() { SetUniqueId(Guid.NewGuid().ToString()); }

    private void OnValidate() {
        if (string.IsNullOrEmpty(uniqueId)) uniqueId = Guid.NewGuid().ToString();
    }
}

