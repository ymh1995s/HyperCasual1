using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Interactable : MonoBehaviour
{
    [SerializeField] private ItemType _type = ItemType.Treasure;
    [SerializeField] private int _value = 1; // amount or level
    [SerializeField] private bool _autoDestroy = false; // default: stack by default
    [Header("Pile Settings")]
    [SerializeField] private Vector3 _pileOffset = Vector3.zero; // offset when stacking on player's pile

    void Reset()
    {
        // Ensure collider is trigger by default for pickups
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<Player>();
            if (player != null && player.Inventory != null)
            {
                // Delegate pickup to PlayerInventory. Pass this gameObject and whether to auto-destroy and per-item pile offset.
                player.Inventory.PickupItem(_type, _value, gameObject, _autoDestroy, _pileOffset);
            }
        }
    }

#if UNITY_EDITOR
    // Visualize the per-item pile offset in the Scene view so designers can tune it.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        // Draw a single line representing the per-item pile offset in world space
        Vector3 worldOffset = transform.TransformDirection(_pileOffset);
        Vector3 start = transform.position;
        Vector3 end = start + worldOffset;
        Gizmos.DrawLine(start, end);
    }
#endif
}
