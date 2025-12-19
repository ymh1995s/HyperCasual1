using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Interactable : MonoBehaviour
{
    [SerializeField] private ItemType _type = ItemType.Treasure;
    [SerializeField] private int _value = 1; // amount or level
    [SerializeField] private bool _autoDestroy = true; // destroy after pickup

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
            if (player != null)
            {
                player.PickupItem(_type, _value);

                // Optional visual/sound could be triggered here
                if (_autoDestroy)
                    Destroy(gameObject);
            }
        }
    }
}
