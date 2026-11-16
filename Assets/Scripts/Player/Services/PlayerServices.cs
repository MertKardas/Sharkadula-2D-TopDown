using System;

public sealed class PlayerServices {
    public PlayerMovementService Movement { get; }
    public PlayerHealthService Health { get; }
    public PlayerInventoryService Inventory { get; }
    public PlayerInteractionService Interaction { get; }

    public PlayerServices(PlayerConfiguration configuration, ItemCatalog catalog) {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        configuration.Validate();
        Movement = new PlayerMovementService(configuration);
        Health = new PlayerHealthService(configuration.MaxHealth);
        Inventory = new PlayerInventoryService(configuration.InventorySlots, configuration.StartingCoin, catalog);
        Interaction = new PlayerInteractionService();
    }
}
