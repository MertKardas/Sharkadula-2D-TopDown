using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

public sealed class ItemDynamicObjectFactory : IDynamicObjectFactory {
    private readonly ItemCatalog _catalog;
    private readonly IObjectResolver _resolver;
    private readonly Scene _scene;
    private readonly SceneSaveContext _context;

    public ItemDynamicObjectFactory(ItemCatalog catalog, IObjectResolver resolver, Scene scene, SceneSaveContext context) {
        _catalog = catalog;
        _resolver = resolver;
        _scene = scene;
        _context = context;
    }

    public SaveableEntity Create(string prefabId, string uniqueId, Vector3 position) {
        var prefab = _catalog.GetDropPrefab(prefabId);
        if (prefab == null) throw new ArgumentException("Unknown or invalid item prefab: " + prefabId);
        if (string.IsNullOrWhiteSpace(uniqueId)) throw new ArgumentException("An entity ID is required.", nameof(uniqueId));
        // The scene resolver injects before Awake; an explicit parent avoids the persistent app scope.
        var obj = _resolver.Instantiate(prefab, position, Quaternion.identity, null);
        if (obj.scene != _scene) SceneManager.MoveGameObjectToScene(obj, _scene);
        var entity = obj.GetComponent<SaveableEntity>();
        try {
            entity.InitializeDynamic(prefabId, uniqueId);
            obj.GetComponent<DropObject>().Initialize(_catalog.GetItem(prefabId), 1);
            _context.TrackDynamic(entity);
            return entity;
        } catch {
            obj.SetActive(false);
            UnityEngine.Object.Destroy(obj);
            throw;
        }
    }
}
