using UnityEngine;

[RequireComponent(typeof(PlayerController), typeof(PlayerInventory))]
public class Player : MonoBehaviour
{
    private PlayerController _controller;
    private PlayerInventory _inventory;

    void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _inventory = GetComponent<PlayerInventory>();
    }

    // Preserve existing Interactable integration: delegate to inventory
    // Keep overload to support callers that pass source GameObject and autoDestroy flag
    public void PickupItem(ItemType type, int value)
    {
        PickupItem(type, value, null, true, Vector3.zero);
    }

    public void PickupItem(ItemType type, int value, GameObject sourceObject, bool autoDestroy)
    {
        PickupItem(type, value, sourceObject, autoDestroy, Vector3.zero);
    }

    public void PickupItem(ItemType type, int value, GameObject sourceObject, bool autoDestroy, Vector3 perItemOffset)
    {
        if (_inventory != null)
            _inventory.PickupItem(type, value, sourceObject, autoDestroy, perItemOffset);
    }

    // Optional helpers for other systems
    public PlayerController Controller => _controller;
    public PlayerInventory Inventory => _inventory;
}
