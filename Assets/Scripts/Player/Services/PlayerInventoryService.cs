using System.Collections.Generic;
using System;
using UnityEngine;

public sealed class PlayerInventoryService
{
    private readonly ItemCatalog _catalog;
    private List<InventoryItem> items = new List<InventoryItem>();
    public int SlotNumber { get; }
    public int Coin { get; private set; }

    public PlayerInventoryService(int slotNumber, int startingCoin, ItemCatalog catalog) {
        if (slotNumber <= 0) throw new ArgumentOutOfRangeException(nameof(slotNumber));
        if (startingCoin < 0) throw new ArgumentOutOfRangeException(nameof(startingCoin));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        SlotNumber = slotNumber;
        Coin = startingCoin;
    }

    public event Action<int> OnCoinChanged;
    public event Action<int> OnCoinChangedAmount;
    public event Action<List<InventoryItem>> OnInventoryChanged;
    public void AddCoin(int amount)
    {
        if (amount <= 0) return;
        if (Coin > int.MaxValue - amount) throw new OverflowException("Coin balance exceeds its limit.");
        Coin += amount;
        OnCoinChanged?.Invoke(Coin);
        OnCoinChangedAmount?.Invoke(amount);
    }
    public void SetCoin(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Coin = amount;
        OnCoinChanged?.Invoke(Coin);
    }
    public void SpendMoney(int amount)
    {
        if (amount <= 0) return;
        if (Coin >= amount)
        {
            Coin -= amount;
            OnCoinChanged?.Invoke(Coin);
            OnCoinChangedAmount?.Invoke(-amount);
        }
        else
        {
            Debug.LogWarning("Not enough coins to spend.");
        }
    }
    public void LoadInventory(List<InventoryItem> inventory)
    {
        var loaded = new List<InventoryItem>();
        foreach (var item in inventory ?? new List<InventoryItem>()) {
            if (item == null) throw new ArgumentException("The inventory contains a null item.", nameof(inventory));
            var data = _catalog.GetItem(item.item);
            if (data == null) throw new InvalidOperationException("Unknown inventory item: " + item.item);
            if (item.quantity <= 0)
                throw new ArgumentException("Invalid inventory stack quantity.", nameof(inventory));
            loaded.Add(new InventoryItem(data, item.quantity));
        }
        items = loaded;
        OnInventoryChanged?.Invoke(items);
    }

    public List<InventoryItem> GetInventory()
    {
        return items;
    }

    public List<InventoryItem> CaptureInventory()
    {
        return items.ConvertAll(item => new InventoryItem(item.item, item.quantity));
    }

    /// <summary>
    /// Adds an item to the inventory.
    /// </summary>
    /// <param name="inventoryItem"></param>
    /// <returns>Returns the number of items actually added.</returns>
    public int AddItem(InventoryItem inventoryItem)
    {
        if (inventoryItem == null || inventoryItem.itemData == null || inventoryItem.quantity <= 0) return 0;
        if (inventoryItem.itemData.IsStackable && inventoryItem.itemData.StackSize <= 0) return 0;
        var itemId = inventoryItem.item;
        var itemsToAdd = inventoryItem.quantity;
        int itemsAdded = 0;

        if (inventoryItem.itemData.IsStackable)
        {
            var stackSize = inventoryItem.itemData.StackSize;

            // First, try to fill existing stacks
            foreach (var invItem in items)
            {
                if (itemsToAdd <= 0) break;
                if (invItem.item != itemId) continue;

                int availableSpace = stackSize - invItem.quantity;
                if (availableSpace > 0)
                {
                    int amountToAdd = Mathf.Min(availableSpace, itemsToAdd);
                    invItem.quantity += amountToAdd;
                    itemsAdded += amountToAdd;
                    itemsToAdd -= amountToAdd;
                }
            }

            // Then, create new stacks for remaining items
            while (itemsToAdd > 0)
            {
                if (items.Count >= SlotNumber)
                {
                    break; // Inventory full
                }

                int amountForNewStack = Mathf.Min(stackSize, itemsToAdd);
                items.Add(new InventoryItem(inventoryItem.itemData, amountForNewStack));
                itemsAdded += amountForNewStack;
                itemsToAdd -= amountForNewStack;
            }
        }
        else
        {
            // Non-stackable items - each item takes one slot
            while (itemsToAdd > 0)
            {
                if (items.Count >= SlotNumber)
                {
                    break; // Inventory full
                }
                items.Add(new InventoryItem(inventoryItem.itemData, 1));
                itemsAdded++;
                itemsToAdd--;
            }
        }

        if (itemsAdded > 0)
        {
            OnInventoryChanged?.Invoke(items);
        }

        return itemsAdded;
    }
    public bool HasItem(string itemId, int quantity)
    {
        if (quantity <= 0) return false;
        int totalQuantity = 0;
        foreach (var item in items)
        {
            if (item.item == itemId)
            {
                totalQuantity += item.quantity;
                if (totalQuantity >= quantity)
                {
                    return true;
                }
            }
        }
        return false;
    }
    public bool RemoveItem(string itemId, int quantity)
    {
        if (!HasItem(itemId, quantity))
        {
            return false;
        }

        int remaining = quantity;
        for (int i = items.Count - 1; i >= 0 && remaining > 0; i--)
        {
            if (items[i].item == itemId)
            {
                if (items[i].quantity > remaining)
                {
                    items[i].quantity -= remaining;
                    remaining = 0;
                }
                else
                {
                    remaining -= items[i].quantity;
                    items.RemoveAt(i);
                }
            }
        }

        OnInventoryChanged?.Invoke(items);
        return true;
    }

}
