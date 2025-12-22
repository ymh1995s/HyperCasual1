using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private int _attackPower = 1;
    [SerializeField] private float _attackSpeed = 1f; // attacks per second

    // Data-only treasure count. Visual stacking for treasure is disabled.
    [SerializeField] private int _treasureCount = 0;

    // Public read-only accessors so other systems can read current player stats
    public int AttackPower => _attackPower;
    public float AttackSpeed => _attackSpeed;

    // Expose treasure count
    public int TreasureCount => _treasureCount;

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
                // Data-only: count treasures, no visual stacking
                _treasureCount += value;
                CollectTreasure(value);
                break;
            default:
                Debug.Log("Picked unknown item");
                break;
        }

        // Ensure the pickup GameObject is removed from the scene if provided.
        if (sourceObject != null)
        {
            Destroy(sourceObject);
        }
    }

    private void EquipDrone(int count)
    {
        // Placeholder: spawn or enable drone(s) as support. Implementation depends on your drone prefab/system.
        Debug.Log($"Equipped {count} drone(s)");
    }

    private void CollectTreasure(int amount)
    {
        // Data-only collection: add to score/coin. Implementation depends on your currency system.
        Debug.Log($"Collected treasure x{amount} (total {_treasureCount})");
    }

    // Consume treasures directly (used by boss handover)
    public void ConsumeTreasures(int amount)
    {
        _treasureCount = Mathf.Max(0, _treasureCount - amount);
    }
}
