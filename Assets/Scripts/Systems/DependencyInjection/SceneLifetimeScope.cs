using System.Linq;
using UnityEngine;
using VContainer;
using VContainer.Unity;

[DefaultExecutionOrder(-5000)]
public sealed class SceneLifetimeScope : LifetimeScope {
    [SerializeField] private string spawnerSaveId;
    public string SpawnerSaveId => spawnerSaveId;

    private void OnValidate() {
        if (string.IsNullOrWhiteSpace(spawnerSaveId)) spawnerSaveId = System.Guid.NewGuid().ToString();
    }

    protected override void Configure(IContainerBuilder builder) {
        builder.RegisterEntryPoint<SceneSaveContext>().AsSelf().WithParameter(gameObject.scene);
        builder.Register<ItemDynamicObjectFactory>(Lifetime.Scoped).As<IDynamicObjectFactory>().WithParameter(gameObject.scene);
        builder.RegisterEntryPoint<DynamicObjectSpawner>().AsSelf().WithParameter(gameObject.scene).WithParameter("saveId", spawnerSaveId);
        autoInjectGameObjects = gameObject.scene.GetRootGameObjects()
            .Where(root => root != gameObject).ToList();
    }
}
