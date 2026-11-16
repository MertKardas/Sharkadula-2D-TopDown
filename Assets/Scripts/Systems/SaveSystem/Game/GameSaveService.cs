using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class GameSaveService {
    public const string PlayerId = "sharkadula.player";
    private const string QuickSaveSlot = "quickSave";
    private readonly ISaveStorage _storage;
    private readonly WorldSaveValidator _validator;
    private GameSaveData _currentData;

    public event Action<Result> OnSaveAfter;
    public bool HasLoadedGame => _currentData != null;
    internal bool EnteringAnotherScene { get; private set; }

    public GameSaveService(ISaveStorage storage, WorldSaveValidator validator) {
        _storage = storage;
        _validator = validator;
    }

    public void Clear() { _currentData = null; EnteringAnotherScene = false; }

    internal EntitySaveData GetRecord(string id) {
        return _currentData != null && _currentData.Objects.TryGetValue(id, out var record) ? record : null;
    }

    internal Result Commit(string sceneName, Dictionary<string, EntitySaveData> updates, IEnumerable<string> removedIds, bool writeToDisk) {
        try {
            var candidate = new GameSaveData {
                ResumeSceneName = sceneName,
                Objects = _currentData == null ? new() : new(_currentData.Objects)
            };
            foreach (var id in removedIds) candidate.Objects.Remove(id);
            foreach (var entry in updates) candidate.Objects[entry.Key] = entry.Value;
            var result = _validator.Validate(candidate);
            if (!result.Success) return result;
            if (writeToDisk) {
                result = _storage.Save(candidate, new MetaData(QuickSaveSlot, "Quick Save", DateTime.UtcNow, Application.version));
                if (!result.Success) return result;
            }
            _currentData = candidate;
            return Result.Ok();
        } catch (Exception error) {
            return Result.Fail("Could not collect/save game: " + error.Message);
        }
    }

    internal void BeginTransition() { EnteringAnotherScene = true; }

    internal void CompleteTransition(string sceneName) {
        if (EnteringAnotherScene && _currentData != null)
            _currentData = new GameSaveData { ResumeSceneName = sceneName, Objects = _currentData.Objects };
        EnteringAnotherScene = false;
    }

    internal Result ReportSaveResult(Result result) {
        OnSaveAfter?.Invoke(result);
        return result;
    }

    public Result QuickLoad() {
        var result = _storage.GetAllMetaData(out var metadata);
        if (!result.Success) return result;
        var latest = metadata.OrderByDescending(meta => meta.SaveTime).FirstOrDefault();
        return latest == null ? Result.Fail("No saved games found.") : LoadGame(latest.SaveName);
    }

    public Result LoadGame(string slotName) {
        var result = _storage.Load(slotName, out var data);
        if (!result.Success) return result;
        try { result = _validator.Validate(data); }
        catch (Exception error) { return Result.Fail("Invalid save data: " + error.Message); }
        if (result.Success) { _currentData = data; EnteringAnotherScene = false; }
        return result;
    }

    public Result LoadAllMetadata(out MetaData[] metadata) {
        var result = _storage.GetAllMetaData(out var loaded);
        metadata = result.Success ? loaded.ToArray() : Array.Empty<MetaData>();
        return result;
    }

    public string GetLastSavedSceneName() { return _currentData?.ResumeSceneName; }
}
