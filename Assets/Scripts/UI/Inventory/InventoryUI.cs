using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour {
    [SerializeField] private GameObject _inventorySlotPrefab;
    [SerializeField] private PlayerController _playerController;
    private readonly List<InventorySlotUI> _inventorySlotsUIs = new List<InventorySlotUI>();
    private PlayerInventoryService _inventory;

    private void Start() {
        if (_playerController == null || _inventorySlotPrefab == null ||
            _inventorySlotPrefab.GetComponent<InventorySlotUI>() == null) {
            Debug.LogError("InventoryUI requires a player and an InventorySlotUI prefab.", this);
            enabled = false;
            return;
        }
        _inventory = _playerController.Inventory;
        for (int i = 0; i < _inventory.SlotNumber; i++) {
            var slot = Instantiate(_inventorySlotPrefab, transform).GetComponent<InventorySlotUI>();
            slot.Init(i);
            _inventorySlotsUIs.Add(slot);
        }
        BindAndRefresh();
    }

    private void OnEnable() {
        if (_inventory != null) BindAndRefresh();
    }

    private void BindAndRefresh() {
        _inventory.OnInventoryChanged += UpdateInventory;
        UpdateInventory(_inventory.GetInventory());
    }

    private void OnDisable() {
        if (_inventory != null) _inventory.OnInventoryChanged -= UpdateInventory;
    }

    public void UpdateInventory(List<InventoryItem> items) {
        for (int i = 0; i < _inventorySlotsUIs.Count; i++) {
            _inventorySlotsUIs[i].UpdateSlot(items != null && i < items.Count ? items[i] : null);
        }
    }
}
