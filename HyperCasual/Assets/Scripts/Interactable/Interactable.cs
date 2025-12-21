using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Interactable : MonoBehaviour
{
    [SerializeField] private ItemType _type = ItemType.Treasure;
    [SerializeField] private int _value = 1; // amount or level
    [Header("Pile Settings")]
    [SerializeField] private Vector3 _pileOffset = Vector3.zero; // offset when stacking on player's pile

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        var player = other.GetComponent<Player>();
        if (player == null)
            return;

        // If this Interactable is an enemy type, trigger player death
        if (_type == ItemType.Enemy)
        {
            var controller = player.Controller;
            if (controller != null)
            {
                controller.Kill();
            }

            return;
        }

        // Otherwise, delegate pickup to PlayerInventory. Pass this gameObject and a fixed autoDestroy value (inventory expects the parameter).
        if (player.Inventory != null)
        {
            player.Inventory.PickupItem(_type, _value, gameObject, false, _pileOffset);
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
