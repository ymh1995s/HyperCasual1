using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Boss : MonoBehaviour
{
    [Header("Handover")]
    [Tooltip("Where incoming items should be placed on the boss (assign a transform in inspector)")]
    [SerializeField] private Transform _itemReturnTransform;

    [Header("Treasure Spawn (for boss handover)")]
    [Tooltip("Prefab to use for spawned treasure boxes when flying to the boss")]
    [SerializeField] private GameObject _treasureBoxPrefab;

    [Tooltip("Optional small positional jitter added to spawn points to avoid exact overlap")]
    [SerializeField] private float _spawnJitter = 0.15f;

    [Tooltip("Base duration for an item to travel to the boss (seconds). Individual items are randomized around this value)")]
    [SerializeField] private float _baseTravelDuration = 1.2f;

    [Tooltip("Delay after all items have arrived before finishing (seconds)")]
    [SerializeField] private float _finishDelay = 0.5f;

    // max upward multiplier for control point based on distance
    [SerializeField] private float _minArcHeight = 0.5f;
    [SerializeField] private float _maxArcHeight = 2.0f;

    // lateral offset max for variation
    [SerializeField] private float _maxLateralOffset = 0.6f;

    private bool _isReceiving = false;

    // expose read-only flag so other systems can check
    public bool IsReceiving => _isReceiving;

    // Called by FinalArea when player reaches it
    public void StartReceivingFromPlayer(GameObject player)
    {
        if (_isReceiving)
            return;

        if (player == null)
            return;

        var inv = player.GetComponent<PlayerInventory>();
        if (inv == null)
        {
            Debug.LogWarning("PlayerInventory not found on player while starting handover.");
            return;
        }

        int count = Mathf.Max(0, inv.TreasureCount);
        if (count == 0)
        {
            // Nothing to do
            return;
        }

        if (_treasureBoxPrefab == null)
        {
            Debug.LogWarning("Treasure box prefab not assigned on Boss.");
            return;
        }

        StartCoroutine(ReceiveItemsCoroutine(inv, player.transform.position, count));
    }

    private IEnumerator ReceiveItemsCoroutine(PlayerInventory inv, Vector3 playerPosition, int count)
    {
        _isReceiving = true;

        Vector3 bossPos = (_itemReturnTransform != null) ? _itemReturnTransform.position : transform.position;

        List<GameObject> spawned = new List<GameObject>(count);

        // Spawn all treasures at player's position with small jitter
        for (int i = 0; i < count; i++)
        {
            Vector3 jitter = new Vector3(
                Random.Range(-_spawnJitter, _spawnJitter),
                Random.Range(-_spawnJitter, _spawnJitter),
                Random.Range(-_spawnJitter, _spawnJitter));

            GameObject go = Instantiate(_treasureBoxPrefab, playerPosition + jitter, Quaternion.identity);

            // Defensive: ensure visible scale
            if (go != null && go.transform.localScale == Vector3.zero)
                go.transform.localScale = Vector3.one;

            spawned.Add(go);
        }

        int remaining = spawned.Count;

        // Launch each spawned object towards the boss along a parabolic quadratic Bezier using DOTween
        for (int i = 0; i < spawned.Count; i++)
        {
            GameObject go = spawned[i];
            if (go == null)
            {
                remaining--;
                continue;
            }

            Vector3 start = go.transform.position;

            // small target jitter so they don't overlap exactly
            Vector3 targetJitter = new Vector3(
                Random.Range(-_spawnJitter, _spawnJitter),
                Random.Range(-_spawnJitter, _spawnJitter),
                Random.Range(-_spawnJitter, _spawnJitter));

            Vector3 target = bossPos + targetJitter;

            float dist = Vector3.Distance(start, target);

            // choose arc height proportional to distance but randomized
            float arcHeight = Random.Range(_minArcHeight, _maxArcHeight) * Mathf.Sqrt(dist);

            // lateral offset perpendicular to flight direction for variety
            Vector3 dir = (target - start);
            Vector3 planarDir = new Vector3(dir.x, 0f, dir.z).normalized;
            Vector3 perp = Vector3.Cross(planarDir, Vector3.up).normalized;
            float lateral = Random.Range(-_maxLateralOffset, _maxLateralOffset) * dist;

            Vector3 control = (start + target) * 0.5f + Vector3.up * arcHeight + perp * lateral;

            // randomize duration (50% - 150% of base)
            float duration = Mathf.Max(0.05f, Random.Range(_baseTravelDuration * 0.5f, _baseTravelDuration * 1.5f));

            // create a tween that animates t from 0 to 1 and updates position along quadratic Bezier
            float t = 0f;
            DOTween.To(() => t, x => t = x, 1f, duration)
                .SetEase(Ease.OutQuad)
                .OnUpdate(() =>
                {
                    if (go == null) return;
                    // Quadratic Bezier: B(t) = (1-t)^2 * P0 + 2(1-t)t * CP + t^2 * P1
                    float invt = 1f - t;
                    Vector3 pos = invt * invt * start + 2f * invt * t * control + t * t * target;
                    // Ensure y >= 0 during flight
                    pos.y = Mathf.Max(0f, pos.y);
                    go.transform.position = pos;
                })
                .OnComplete(() =>
                {
                    // Ensure final position is target (and above ground)
                    if (go != null)
                    {
                        Vector3 final = target;
                        final.y = Mathf.Max(0f, final.y);
                        go.transform.position = final;
                    }

                    inv.ConsumeTreasures(1);
                    if (go != null)
                        Destroy(go);

                    remaining--;
                });
        }

        // wait until all arrived
        while (remaining > 0)
            yield return null;

        if (_finishDelay > 0f)
            yield return new WaitForSeconds(_finishDelay);

        // Finished receiving: unset flag
        _isReceiving = false;

        // After 3 seconds request StageController to restart the stage (respawn, reset player/drone state)
        yield return new WaitForSeconds(3f);

        var stageController = FindObjectOfType<StageController>();
        if (stageController != null)
        {
            stageController.RestartStageImmediate();
        }
        else
        {
            Debug.LogWarning("StageController not found when attempting to restart after boss handover.");
        }
    }
}
