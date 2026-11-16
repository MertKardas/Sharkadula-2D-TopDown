using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public sealed class ItemCatalog : IDisposable {
    private readonly Dictionary<string, ItemData> _items = new Dictionary<string, ItemData>(StringComparer.Ordinal);
    private AsyncOperationHandle<IList<ItemData>> _handle;

    public ItemCatalog() {
        _handle = Addressables.LoadAssetsAsync<ItemData>("Item", null);
        try {
            _handle.WaitForCompletion();
            if (_handle.Status != AsyncOperationStatus.Succeeded)
                throw new InvalidOperationException("Could not load the Addressables Item catalog.", _handle.OperationException);
            foreach (var item in _handle.Result) {
                if (item == null || string.IsNullOrEmpty(item.ID))
                    throw new InvalidOperationException("The Item catalog contains an invalid item.");
                _items.Add(item.ID, item);
            }
        } catch {
            Dispose();
            throw;
        }
    }

    public ItemData GetItem(string id) {
        if (string.IsNullOrEmpty(id)) return null;
        _items.TryGetValue(id, out var item);
        return item;
    }

    public void Dispose() {
        if (_handle.IsValid()) Addressables.Release(_handle);
        _handle = default;
        _items.Clear();
    }

    public UnityEngine.GameObject GetDropPrefab(string id) {
        var prefab = GetItem(id)?.Prefab;
        return prefab != null && prefab.GetComponent<SaveableEntity>() != null && prefab.GetComponent<DropObject>() != null
            && prefab.GetComponents<ISavable>().Length == 1 ? prefab : null;
    }
}
