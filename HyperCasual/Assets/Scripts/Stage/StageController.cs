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

    private void Start()
    {
        if (_boss != null)
        {
            //_boss.OnAllItemsReceived += OnBossFinishedReceiving;
        }
    }

    private void OnDestroy()
    {
        if (_boss != null)
        {
            //_boss.OnAllItemsReceived -= OnBossFinishedReceiving;
        }
    }

    private void OnBossFinishedReceiving()
    {
        // Show victory UI if assigned
        if (_victoryUI != null)
            _victoryUI.SetActive(true);

        // Advance to next stage after a short delay to let the player see the result
        Invoke(nameof(AdvanceToNextStage), _restartDelay);
    }

    private void AdvanceToNextStage()
    {
        // Hide victory UI
        if (_victoryUI != null)
            _victoryUI.SetActive(false);

        // Respawn items using public Respawn if available
        if (_spawner != null)
        {
            _spawner.Respawn();
        }

        // Reset player position and movement
        if (_playerController != null)
            _playerController.ResetToStart();

        // Optionally, re-enable any player movement locks or UI
    }

    // Public method so external systems (e.g. PlayerController on death) can request an immediate stage reset
    public void RestartStageImmediate()
    {
        // Hide any victory UI
        if (_victoryUI != null)
            _victoryUI.SetActive(false);

        // Respawn stage items
        if (_spawner != null)
            _spawner.Respawn();

        // Reset player to start
        if (_playerController != null)
            _playerController.ResetToStart();

        // Cancel any pending AdvanceToNextStage invokes
        CancelInvoke(nameof(AdvanceToNextStage));
    }
}
