using UnityEngine;
using VContainer;
using VContainer.Unity;

public sealed class AppLifetimeScope : LifetimeScope {
    [SerializeField] private AudioConfiguration audioConfiguration;
    protected override void Configure(IContainerBuilder builder) {
        builder.RegisterInstance(audioConfiguration);
        builder.Register<InputService>(Lifetime.Singleton);
        builder.Register<ItemCatalog>(Lifetime.Singleton);
        builder.Register<EasySaveStorage>(Lifetime.Singleton).As<ISaveStorage>()
            .WithParameter("directory", System.IO.Path.Combine(Application.persistentDataPath, "SavesV3"));
        builder.Register<WorldSaveValidator>(Lifetime.Singleton);
        builder.Register<GameSaveService>(Lifetime.Singleton);
        builder.RegisterEntryPoint<AudioService>().AsSelf().WithParameter(transform);
        builder.Register<GameService>(Lifetime.Singleton);
        builder.RegisterBuildCallback(container => container.Resolve<GameService>());
    }
}

