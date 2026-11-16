using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

public sealed class SceneSaveContext : IInitializable, IStartable, IDisposable {
    private readonly Scene _scene;
    private readonly GameSaveService _saves;
    private readonly GameService _game;
    // Keep removal decisions after the Unity object has been destroyed, until commit succeeds.
    private readonly Dictionary<string, bool> _removed = new(StringComparer.Ordinal);
    private DynamicObjectSpawner _spawner;
    private string _initializationError;
    private bool _disposed;
    private bool _ready;
    private readonly bool _restorePlayerAtEntrance;

    public SceneSaveContext(Scene scene, GameSaveService saves, GameService game) {
        _scene = scene; _saves = saves; _game = game;
        _restorePlayerAtEntrance = saves.EnteringAnotherScene;
    }

    internal bool RestorePlayerAtEntrance => _restorePlayerAtEntrance;
    internal EntitySaveData GetRecord(string id) => _saves.GetRecord(id);
    internal bool IsRemoved(string id) => _removed.ContainsKey(id);
    internal void AttachSpawner(DynamicObjectSpawner spawner) { _spawner = spawner; }
    internal void TrackDynamic(SaveableEntity entity) { _spawner.Track(entity); }

    public void Initialize() { _game.BeginScene(_scene); }

    public async void Start() {
        await UniTask.Yield(Cysharp.Threading.Tasks.PlayerLoopTiming.LastPostLateUpdate);
        if (_disposed || !_scene.isLoaded) return;
        var result = ValidateStartup();
        _ready = result.Success;
        _game.CompleteScene(_scene, result);
    }

    internal void ReportInitializationError(string message) { _initializationError = message; }

    internal void MarkRemoved(SaveableEntity entity) {
        if (_disposed || entity.gameObject.scene != _scene) throw new InvalidOperationException("The entity does not belong to this scene.");
        if (entity.UniqueId == GameSaveService.PlayerId) throw new InvalidOperationException("The player cannot be permanently removed.");
        if (entity.IsDynamic && !_spawner.Contains(entity))
            throw new InvalidOperationException("The dynamic entity is not tracked by its spawner.");
        _removed[entity.UniqueId] = entity.IsDynamic;
    }

    private SaveableEntity[] GetEntities() {
        var roots = _scene.GetRootGameObjects();
        foreach (var component in roots.SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)))
            if (component is ISavable && component.GetComponent<SaveableEntity>() == null)
                throw new InvalidOperationException($"'{component.name}' has no SaveableEntity.");
        var entities = roots.SelectMany(root => root.GetComponentsInChildren<SaveableEntity>(true)).ToArray();
        var ids = new HashSet<string>(StringComparer.Ordinal) { _spawner.SaveId };
        foreach (var entity in entities) {
            if (string.IsNullOrWhiteSpace(entity.UniqueId) || !ids.Add(entity.UniqueId))
                throw new InvalidOperationException($"Missing or duplicate save ID on '{entity.name}'.");
            entity.GetSavable();
        }
        return entities;
    }

    public Result ValidateStartup() {
        try {
            if (_initializationError != null) return Result.Fail(_initializationError);
            var entities = GetEntities();
            foreach (var entity in entities) {
                if (entity.StartupError != null) return Result.Fail($"'{entity.name}' could not restore: {entity.StartupError}");
                if (entity.gameObject.activeInHierarchy && !entity.HasStarted && !entity.IsRemoved)
                    return Result.Fail($"'{entity.name}' did not complete save initialization.");
            }
            _spawner.CaptureMembership();
            return Result.Ok();
        } catch (Exception error) {
            return Result.Fail("Scene initialization failed: " + error.Message);
        }
    }

    internal Dictionary<string, EntitySaveData> Capture() {
        if (_disposed || !_scene.isLoaded) throw new InvalidOperationException("The scene is no longer available.");
        var updates = new Dictionary<string, EntitySaveData>(StringComparer.Ordinal);
        foreach (var entity in GetEntities()) {
            if (entity.IsRemoved || _removed.ContainsKey(entity.UniqueId)) {
                if (!entity.IsDynamic) updates[entity.UniqueId] = new EntitySaveData { IsRemoved = true };
                continue;
            }
            if (entity.StartupError != null) throw new InvalidOperationException(entity.StartupError);
            if (!entity.HasStarted && entity.gameObject.activeInHierarchy)
                throw new InvalidOperationException($"'{entity.name}' has not completed startup.");
            object state = entity.HasStarted ? entity.GetSavable().CaptureState() : GetRecord(entity.UniqueId)?.State;
            if (state == null) {
                if (entity.IsDynamic) throw new InvalidOperationException($"Dynamic object '{entity.name}' has not initialized.");
                continue;
            }
            if (entity.IsDynamic && !_spawner.Contains(entity))
                throw new InvalidOperationException($"Dynamic object '{entity.name}' is not tracked.");
            updates.Add(entity.UniqueId, new EntitySaveData {
                State = state,
                PrefabId = entity.PrefabId,
                SpawnPosition = entity.IsDynamic ? entity.transform.position : default
            });
        }
        foreach (var entry in _removed)
            if (!entry.Value) updates[entry.Key] = new EntitySaveData { IsRemoved = true };
        updates.Add(_spawner.SaveId, new EntitySaveData { State = _spawner.CaptureMembership() });
        return updates;
    }

    private Result Collect(bool writeToDisk, bool forTransition = false) {
        try {
            if (!_ready) return Result.Fail("The scene has not completed save initialization.");
            if (_game.IsLoadingScene && !forTransition) return Result.Fail("A scene is already loading.");
            var updates = Capture();
            var result = _saves.Commit(_scene.name, updates, _removed.Where(pair => pair.Value).Select(pair => pair.Key), writeToDisk);
            if (result.Success) {
                _spawner.CompleteCollect();
                _removed.Clear();
            }
            return result;
        } catch (Exception error) {
            return Result.Fail("Could not collect scene: " + error.Message);
        }
    }

    public Result Collect() => Collect(false);
    public Result QuickSave() => _saves.ReportSaveResult(Collect(true));
    public Result ChangeScene(string sceneName) {
        var result = _game.ChangeScene(sceneName, () => Collect(false, true));
        if (result.Success) _ready = false;
        return result;
    }
    public void Dispose() { _disposed = true; }
}
