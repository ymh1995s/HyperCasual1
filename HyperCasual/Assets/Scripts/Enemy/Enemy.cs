using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Interface so external callers can inflict damage
public interface IDamageable
{
    void Damage(int amount);
}

[RequireComponent(typeof(Collider))]
public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int _maxHealth = 50;
    private int _currentHealth;

    [Header("Death / Reward")]
    [Tooltip("Optional prefab to spawn where the enemy dies (e.g. reward)")]
    [SerializeField] private GameObject _rewardPrefab;
    [Tooltip("Seconds to wait after death before destroying the enemy and spawning reward")]
    [SerializeField] private float _deathDelay = 3f;

    private Animator _animator;
    private bool _isDead = false;

    // cached Image components for any child named "RedBar"
    private List<Image> _redBars = new List<Image>();

    void Awake()
    {
        _currentHealth = _maxHealth;
        _animator = GetComponentInChildren<Animator>();

        // find all child transforms named "RedBar" recursively and get Image components
        FindRedBarsRecursively(transform);
    }

    void Start()
    {
        UpdateRedBars();
    }

    // Public interface method
    public void Damage(int amount)
    {
        if (_isDead) return;

        _currentHealth -= amount;
        _currentHealth = Mathf.Clamp(_currentHealth, 0, _maxHealth);

        UpdateRedBars();

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void UpdateRedBars()
    {
        float t = (_maxHealth > 0) ? _currentHealth / (float)_maxHealth : 0f;
        for (int i = 0; i < _redBars.Count; i++)
        {
            if (_redBars[i] != null)
                _redBars[i].fillAmount = Mathf.Clamp01(t);
        }
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        // disable colliders and tags so this enemy no longer interacts with other objects
        DisableCollidersAndTags();

        if (_animator != null)
        {
            _animator.SetBool("death", true);
        }

        // start delayed destroy/spawn routine
        StartCoroutine(DeathRoutine());
    }

    // Disable all Collider components on this gameObject and its children and set tags to Untagged
    private void DisableCollidersAndTags()
    {
        // Disable all 3D colliders
        var colliders = GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders)
        {
            if (c != null)
                c.enabled = false;
        }

        // Optionally disable 2D colliders if any exist in children
        // (project may not use 2D but this is safe)
        var colliders2D = GetComponentsInChildren<Collider2D>(true);
        foreach (var c2 in colliders2D)
        {
            if (c2 != null)
                c2.enabled = false;
        }

        // Change tag on root and children to Untagged to avoid tag-based checks
        // Only change if tag exists (Untagged always exists by default)
        try
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.tag = "Untagged";
            }
        }
        catch
        {
            // ignore if tag change fails for any reason
        }
    }

    private IEnumerator DeathRoutine()
    {
        // wait for animation / effects
        yield return new WaitForSeconds(_deathDelay);

        bool spawned = false;

        // spawn reward if assigned
        if (_rewardPrefab != null)
        {
            Instantiate(_rewardPrefab, transform.position, Quaternion.identity);
            spawned = true;
        }

        // if a reward prefab was spawned, remove enemy object from scene
        if (spawned)
        {
            Destroy(gameObject);
            yield break;
        }

        // Otherwise, keep the root GameObject in scene but disable visuals and this component
        // (colliders and tags already disabled in DisableCollidersAndTags)
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r != null)
                r.enabled = false;
        }

        // disable animator if present
        if (_animator != null)
            _animator.enabled = false;

        // disable this script to prevent further logic
        this.enabled = false;
    }

    // Recursive search for child transforms named "RedBar" and cache Image components
    private void FindRedBarsRecursively(Transform parent)
    {
        if (parent == null) return;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child == null) continue;

            if (child.name == "RedBar")
            {
                var img = child.GetComponent<Image>();
                if (img != null)
                {
                    _redBars.Add(img);
                }
            }

            // recurse
            if (child.childCount > 0)
                FindRedBarsRecursively(child);
        }
    }

#if UNITY_EDITOR
    // draw current health % in editor for convenience
    void OnDrawGizmosSelected()
    {
        float t = (_maxHealth > 0) ? (_currentHealth / (float)_maxHealth) : 0f;
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position + Vector3.up * 1.2f, 0.1f * Mathf.Clamp01(t + 0.1f));
    }
#endif
}
