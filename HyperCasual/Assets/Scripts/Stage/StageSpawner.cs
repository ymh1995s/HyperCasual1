using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns Interactable prefabs for the current stage at start along periodic positions based on a reference transform's forward direction.
/// Keeps track of spawned items so ResetStage can remove them.
/// Now spawns, at each periodic position, three items placed in parallel (left, center, right) offset along the lateral axis.
/// </summary>
public class StageSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Prefab(s) to spawn. Prefab must contain an Interactable component.")]
    [SerializeField] private GameObject[] _spawnPrefabs;

    [Tooltip("If set, spawns will use one of these transform positions (random). If empty, spawns along reference forward direction.")]
    [SerializeField] private Transform[] _spawnPoints;

    [Tooltip("Area center (local to this GameObject) used if no spawnPoints provided")]
    [SerializeField] private Vector3 _areaCenter = Vector3.zero;

    [Header("Spawn Mode")]
    [Tooltip("Reference transform used to position the periodic spawns. Typically the player transform.")]
    [SerializeField] private Transform _distanceReference;
    [Tooltip("Distance between spawn columns (forward direction)")]
    [SerializeField] private float _spawnSpacing = 5f;

    [Tooltip("Lateral distance from center for the left and right items. If set to 3, lateral positions will be -3, 0, +3.")]
    [SerializeField] private float _lateralDistance = 3f;

    [Tooltip("Number of columns (forward direction). Spawns will be 3 rows by this many columns.")]
    [SerializeField] private int _columns = 10;

    // kept for compatibility (unused for grid size now)
    [SerializeField] private int _maxSpawned = 20;

    private List<GameObject> _spawned = new List<GameObject>();

    void Awake()
    {
        if (_distanceReference == null)
        {
            // try to find player by tag as fallback
            var player = GameObject.FindWithTag("Player");
            if (player != null)
                _distanceReference = player.transform;
        }
    }

    void Start()
    {
        // On start, spawn a fixed grid of items (3 rows x _columns columns)
        SpawnInitialLine();
    }

    public void ResetStage()
    {
        // Destroy existing items
        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            var go = _spawned[i];
            if (go != null)
                Destroy(go);
        }
        _spawned.Clear();
    }

    // Public wrapper to respawn items (used by StageController)
    public void Respawn()
    {
        ResetStage();
        SpawnInitialLine();
    }

    private void SpawnInitialLine()
    {
        if (_distanceReference == null || _spawnPrefabs == null || _spawnPrefabs.Length == 0)
            return;

        int cols = Mathf.Max(0, _columns);

        for (int i = 1; i <= cols; i++)
        {
            // Determine base position and rotation for this spawn column
            Vector3 basePos;
            Quaternion baseRot = Quaternion.identity;
            Vector3 lateralDir;

            if (_spawnPoints != null && _spawnPoints.Length > 0)
            {
                var p = _spawnPoints[(i - 1) % _spawnPoints.Length];
                basePos = p.position + _distanceReference.forward * (_spawnSpacing * i);
                baseRot = p.rotation;
                lateralDir = p.right;
            }
            else
            {
                basePos = _distanceReference.position + _distanceReference.forward * (_spawnSpacing * i);
                // Use deterministic center offset (no randomness) for fixed grid
                basePos += transform.TransformVector(_areaCenter);
                baseRot = Quaternion.identity;
                lateralDir = _distanceReference.right;
            }

            // three rows: left (-1), center (0), right (+1) at -_lateralDistance, 0, +_lateralDistance
            for (int row = -1; row <= 1; row++)
            {
                Vector3 pos = basePos + lateralDir * (_lateralDistance * row);

                var prefab = _spawnPrefabs[Random.Range(0, _spawnPrefabs.Length)];
                if (prefab == null)
                    continue;

                var go = Instantiate(prefab, pos, baseRot);
                _spawned.Add(go);
            }
        }
    }

    // Draw planned spawn positions as yellow gizmos in the editor for easier placement visualization.
    void OnDrawGizmos()
    {
        if (_distanceReference == null)
            return;

        Gizmos.color = Color.yellow;

        int cols = Mathf.Max(0, _columns);

        for (int i = 1; i <= cols; i++)
        {
            Vector3 basePos;
            Vector3 lateralDir;

            if (_spawnPoints != null && _spawnPoints.Length > 0)
            {
                var p = _spawnPoints[(i - 1) % _spawnPoints.Length];
                basePos = p.position + (_distanceReference != null ? _distanceReference.forward : transform.forward) * (_spawnSpacing * i);
                lateralDir = p.right;
            }
            else
            {
                if (_distanceReference != null)
                    basePos = _distanceReference.position + _distanceReference.forward * (_spawnSpacing * i);
                else
                    basePos = transform.position + transform.forward * (_spawnSpacing * i);

                basePos += transform.TransformVector(_areaCenter);
                lateralDir = (_distanceReference != null) ? _distanceReference.right : transform.right;
            }

            for (int row = -1; row <= 1; row++)
            {
                Vector3 pos = basePos + lateralDir * (_lateralDistance * row);
                Gizmos.DrawWireSphere(pos, 0.25f);
            }
        }
    }
}
