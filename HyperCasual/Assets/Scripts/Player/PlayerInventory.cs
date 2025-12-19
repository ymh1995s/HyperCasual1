using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private int _attackPower = 1;
    [SerializeField] private float _attackSpeed = 1f; // attacks per second

    public void PickupItem(ItemType type, int value)
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
}
