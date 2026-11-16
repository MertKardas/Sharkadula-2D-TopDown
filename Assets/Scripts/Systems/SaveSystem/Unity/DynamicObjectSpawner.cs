using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

public sealed class DynamicObjectSpawner : IInitializable {
    private readonly SceneSaveContext _context;
    private readonly IDynamicObjectFactory _factory;
    private readonly Scene _scene;
    private readonly Dictionary<string, SaveableEntity> _instances = new(StringComparer.Ordinal);
    public string SaveId { get; }

    public DynamicObjectSpawner(SceneSaveContext context, IDynamicObjectFactory factory, Scene scene, string saveId) {
        _context = context; _factory = factory; _scene = scene; SaveId = saveId;
    }

    public void Initialize() {
        _context.AttachSpawner(this);
        try {
            if (string.IsNullOrWhiteSpace(SaveId) || SaveId == GameSaveService.PlayerId)
                throw new InvalidOperationException("The scene spawner needs a unique save ID.");
            Spawn();
        } catch (Exception error) { _context.ReportInitializationError("Dynamic object creation failed: " + error.Message); }
    }

    public SaveableEntity Create(string prefabId, Vector3 position) {
        return _factory.Create(prefabId, Guid.NewGuid().ToString(), position);
    }

    internal void Track(SaveableEntity entity) {
        if (entity.gameObject.scene != _scene || !entity.IsDynamic || entity.UniqueId == SaveId || entity.UniqueId == GameSaveService.PlayerId)
            throw new InvalidOperationException("Invalid dynamic entity identity or scene.");
        if (_instances.TryGetValue(entity.UniqueId, out var existing) && !ReferenceEquals(existing, entity))
            throw new InvalidOperationException("Duplicate dynamic save ID: " + entity.UniqueId);
        if (_context.IsRemoved(entity.UniqueId)) throw new InvalidOperationException("Cannot recreate a removed entity before collect.");
        var previous = _context.GetRecord(entity.UniqueId);
        if (previous != null && (previous.IsRemoved || previous.PrefabId != entity.PrefabId))
            throw new InvalidOperationException("The dynamic identity conflicts with an existing record.");
        _instances[entity.UniqueId] = entity;
    }

    internal bool Contains(SaveableEntity entity) {
        return _instances.TryGetValue(entity.UniqueId, out var tracked) && ReferenceEquals(entity, tracked);
    }

    internal SpawnerSaveData CaptureMembership() {
        var state = new SpawnerSaveData();
        foreach (var entry in _instances) {
            if (_context.IsRemoved(entry.Key) || (entry.Value != null && entry.Value.IsRemoved)) continue;
            if (entry.Value == null) throw new InvalidOperationException("Dynamic entity was destroyed without MarkRemoved: " + entry.Key);
            state.DynamicIds.Add(entry.Key);
        }
        return state;
    }

    internal void CompleteCollect() {
        var removed = new List<string>();
        foreach (var entry in _instances)
            if (_context.IsRemoved(entry.Key) || (entry.Value != null && entry.Value.IsRemoved)) removed.Add(entry.Key);
        foreach (var id in removed) _instances.Remove(id);
    }

    public void Spawn() {
        var record = _context.GetRecord(SaveId);
        if (record == null) return;
        if (record.IsRemoved || record.State is not SpawnerSaveData state)
            throw new InvalidOperationException("Invalid spawner record.");
        var existing = new Dictionary<string, SaveableEntity>(StringComparer.Ordinal);
        foreach (var root in _scene.GetRootGameObjects())
            foreach (var entity in root.GetComponentsInChildren<SaveableEntity>(true))
                if (!existing.TryAdd(entity.UniqueId, entity)) throw new InvalidOperationException("Duplicate save ID: " + entity.UniqueId);
        foreach (var id in state.DynamicIds) {
            var item = _context.GetRecord(id);
            if (item == null || item.IsRemoved || string.IsNullOrEmpty(item.PrefabId))
                throw new InvalidOperationException("Missing dynamic object record: " + id);
            if (_context.IsRemoved(id)) continue;
            if (existing.TryGetValue(id, out var entity)) {
                if (!entity.IsDynamic || entity.PrefabId != item.PrefabId)
                    throw new InvalidOperationException("Dynamic identity conflicts with a scene object: " + id);
                Track(entity);
                continue;
            }
            _factory.Create(item.PrefabId, id, item.SpawnPosition);
        }
    }
}
