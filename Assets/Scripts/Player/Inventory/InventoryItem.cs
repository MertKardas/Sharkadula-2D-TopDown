using System;
using Newtonsoft.Json;

[Serializable]
public class InventoryItem
{
    [JsonIgnore]
    public ItemData itemData { get; private set; }
    public string item;
    public int quantity;
    [JsonConstructor]
    public InventoryItem(string item, int quantity)
    {
        this.item = item;
        this.quantity = quantity;
    }

    public InventoryItem(ItemData itemData, int quantity)
    {
        this.itemData = itemData;
        this.item = itemData.ID;
        this.quantity = quantity;
    }

}
