using UnityEngine;

/// <summary>
/// StageController orchestrates stage transitions: when boss receives all items, advance to next stage by
/// - resetting player to start
/// - resetting spawner (respawn items)
/// - showing victory UI (optional)
/// Attach this to an empty GameObject in the scene and assign references.
/// </summary>
public class StageController : MonoBehaviour
{
    [SerializeField] private Boss _boss;
    [SerializeField] private StageSpawner _spawner;
    [SerializeField] private PlayerController _playerController;

    [Tooltip("Optional: UI root to enable on victory advance (can be null)")]
    [SerializeField] private GameObject _victoryUI;

    [Tooltip("Delay before restarting the loop after boss receives all items")]
    [SerializeField] private float _restartDelay = 1.0f;

    // Public method so external systems (e.g. PlayerController on death) can request an immediate stage reset
    public void RestartStageImmediate()
    {
        // Hide any victory UI
        if (_victoryUI != null)
            _victoryUI.SetActive(false);

        // Remove any lingering interactables in scene (defensive)
        var interactables = FindObjectsOfType<Interactable>();
        foreach (var it in interactables)
        {
            if (it != null && it.gameObject != null)
                Destroy(it.gameObject);
        }

        // Remove any existing bullets/projectiles
        var bullets = FindObjectsOfType<Bullet>();
        foreach (var b in bullets)
        {
            if (b != null && b.gameObject != null)
                Destroy(b.gameObject);
        }

        // Respawn stage items
        if (_spawner != null)
            _spawner.Respawn();

        // Reset player to start
        if (_playerController != null)
            _playerController.ResetToStart();

        // Reset drones
        var drones = FindObjectsOfType<Drone>();
        foreach (var d in drones)
        {
            if (d != null)
                d.ResetToStart();
        }

        // Cancel any pending restart invokes
        CancelInvoke(nameof(RestartStageImmediate));
    }
}
