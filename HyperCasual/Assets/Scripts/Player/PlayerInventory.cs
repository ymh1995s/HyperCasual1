using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private int _attackPower = 1;
    [SerializeField] private float _attackSpeed = 1f; // attacks per second

    [Header("Pickup Pile")]
    [SerializeField] private Transform _pileTransform; // where picked items should stack
    // _stackOffset removed from inventory; per-item offset now comes from Interactable

    private List<GameObject> _stackedItems = new List<GameObject>();

    public void PickupItem(ItemType type, int value, GameObject sourceObject, bool autoDestroy, Vector3 perItemOffset)
    {
        switch (type)
        {
            case ItemType.Drone:
                EquipDrone(value);
                break;
            case ItemType.ATKPowerUp:
                _attackPower += value;
                Debug.Log($"Picked ATK Power Up: +{value}, now {_attackPower}");
                break;
            case ItemType.ATKSpeedUp:
                _attackSpeed += value * 0.2f; // each value increases attack speed by 20%
                Debug.Log($"Picked ATK Speed Up: +{value}, now {_attackSpeed}");
                break;
            case ItemType.Treasure:
                CollectTreasure(value);
                break;
            default:
                Debug.Log("Picked unknown item");
                break;
        }

        // Always try to stack the source object if a pile transform is assigned.
        if (_pileTransform != null && sourceObject != null)
        {
            StackItem(sourceObject, perItemOffset);
        }
        else if (sourceObject != null)
        {
            // Fallback: if no pile transform is configured, destroy the original object
            Destroy(sourceObject);
        }
    }

    private void StackItem(GameObject item, Vector3 perItemOffset)
    {
        // Compute offset in world space based on the Interactable's local offset
        Vector3 worldOffset = item.transform.TransformDirection(perItemOffset);
        // Convert world offset to pile local space
        Vector3 pileLocalOffset = Vector3.zero;
        if (_pileTransform != null)
        {
            pileLocalOffset = _pileTransform.InverseTransformDirection(worldOffset);
        }

        // Disable physics/interaction on the picked object
        var rb = item.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        var col = item.GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        // Parent to pile transform and position it
        item.transform.SetParent(_pileTransform);

        // Position the item so each item is separated by pileLocalOffset from the previous one.
        // Use (_stackedItems.Count + 1) so first stacked item is placed at one offset distance (not at zero).
        Vector3 localPos = pileLocalOffset * (_stackedItems.Count + 1);
        item.transform.localPosition = localPos;
        item.transform.localRotation = Quaternion.identity;

        _stackedItems.Add(item);
    }

    private void EquipDrone(int count)
    {
        // Placeholder: spawn or enable drone(s) as support. Implementation depends on your drone prefab/system.
        Debug.Log($"Equipped {count} drone(s)");
    }

    private void CollectTreasure(int amount)
    {
        // Placeholder: add to score/coin. Implementation depends on your currency system.
        Debug.Log($"Collected treasure x{amount}");
    }

    // --- New API for boss handover ---

    // Returns true if there are stacked items available to hand over.
    public bool HasStackedItems()
    {
        return _stackedItems != null && _stackedItems.Count > 0;
    }

    // Peeks the topmost stacked item (last added) without removing it. Returns null if none.
    public GameObject PeekTopStackedItem()
    {
        if (!HasStackedItems())
            return null;

        return _stackedItems[_stackedItems.Count - 1];
    }

    // Removes and destroys the specified stacked item. Safe if item is null or not found.
    // Call this after an external animation has finished moving the item.
    public void RemoveTopStackedItem(GameObject item)
    {
        if (item == null)
            return;

        int idx = _stackedItems.IndexOf(item);
        if (idx >= 0)
        {
            _stackedItems.RemoveAt(idx);
        }

        // Ensure it's unparented and then destroy
        item.transform.SetParent(null);
        Destroy(item);
    }

    // Backwards-compatible pop that immediately removes and returns the top item.
    public GameObject PopTopStackedItem()
    {
        if (!HasStackedItems())
            return null;

        int last = _stackedItems.Count - 1;
        var item = _stackedItems[last];
        _stackedItems.RemoveAt(last);

        if (item != null)
        {
            // Unparent so caller can reparent to boss return transform cleanly.
            item.transform.SetParent(null);
        }

        return item;
    }
}
