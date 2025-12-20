using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FinalArea : MonoBehaviour
{
    [Tooltip("Reference to the boss object that will receive items")]
    [SerializeField] private Boss _boss;

    [Tooltip("Should player movement be stopped when entering the final area?")]
    [SerializeField] private bool _stopPlayer = true;

    private void Reset()
    {
        // Ensure collider is trigger by default
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_boss == null) 
        {
            Debug.LogWarning("FinalArea has no Boss assigned.");
            return;
        }

        var player = other.gameObject;
        // We expect the player to have a PlayerController or PlayerInventory component
        var inv = player.GetComponent<PlayerInventory>();
        var controller = player.GetComponent<PlayerController>();

        if (inv == null || controller == null)
            return;

        // Stop player's forward movement and lateral input when entering final area
        if (_stopPlayer)
        {
            // Stop forward movement
            controller.ForwardSpeed = 0f;
            // Lock lateral movement by setting speeds to zero
            controller.LateralSpeed = 0f;
        }

        // Tell boss to start receiving items from this player
        _boss.StartReceivingFromPlayer(player);
    }
}
