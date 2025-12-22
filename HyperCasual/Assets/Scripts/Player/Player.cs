using UnityEngine;
using System.Collections;

[System.Serializable]
public class EffectsGroup
{
    public string EffectName;
    public float Speed = 20;
    public ParticleSystem ChargeParticles;
    public float ChargeParticleTime;
    public AudioClip ChargeClip;
    public ParticleSystem StartParticles;
    public ParticleSystem BulletParticles;
    public ParticleSystem HitParticles;
    public AudioClip startClip;
    public AudioClip bulletClip;
    public AudioClip hitClip;
    public bool isTargeting;
    public float RotSpeed;
    [Tooltip("평사 여부, true일 경우 y축 방향에서 탄알의 속도는 0이며 상하로 회전할 수 없습니다")]
    public bool isFlatShoot = false;
}

[RequireComponent(typeof(PlayerController), typeof(PlayerInventory))]
public class Player : MonoBehaviour
{
    private PlayerController _controller;
    private PlayerInventory _inventory;

    [Header("Auto Shoot")]
    [SerializeField] private BulletDatas _bulletDatas;
    [SerializeField] private Transform _shootOrigin; // where bullets spawn
    [SerializeField] private float _shootInterval = 1f;

    private Coroutine _autoShootCoroutine;

    private Boss _boss;

    void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _inventory = GetComponent<PlayerInventory>();

        // cache boss reference if present
        _boss = FindObjectOfType<Boss>();
    }

    void Start()
    {
        // start automatic shooting if configured
        if (_bulletDatas != null && _bulletDatas.Effects != null && _bulletDatas.Effects.Count > 0 && _shootOrigin != null)
        {
            _autoShootCoroutine = StartCoroutine(AutoShootRoutine());
        }
    }

    private IEnumerator AutoShootRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(_shootInterval);

            // do not shoot while player is dead
            if (_controller != null && _controller.IsDead)
                continue;

            // do not shoot while boss is receiving (player reached boss zone)
            if (_boss != null && _boss.IsReceiving)
                continue;

            DoPlayerShoot();
        }
    }

    private void DoPlayerShoot()
    {
        // use first effect (index 0) as requested
        var effect = _bulletDatas.Effects[0];
        if (effect == null) return;

        // direction: player's forward. If flat shoot, zero out y
        Vector3 targetDir = transform.forward;
        if (effect.isFlatShoot) targetDir.y = 0;
        targetDir = targetDir.normalized;

        if (effect.StartParticles != null)
        {
            var startPar = Instantiate(effect.StartParticles, _shootOrigin.position, Quaternion.identity);
            startPar.transform.forward = targetDir;

            var onStart = startPar.gameObject.AddComponent<BulletAudioTrigger>();
            if (effect.startClip != null)
                onStart.onClip = effect.startClip;
        }

        if (effect.BulletParticles != null)
        {
            var bulletObj = Instantiate(effect.BulletParticles, _shootOrigin.position, Quaternion.identity);
            bulletObj.transform.forward = targetDir;

            var bullet = bulletObj.gameObject.AddComponent<Bullet>();
            bullet.OnHitEffect = effect.HitParticles;
            bullet.Speed = effect.Speed;
            bullet.isTargeting = effect.isTargeting;
            bullet.isFlatShoot = effect.isFlatShoot;

            if (effect.isTargeting)
            {
                var target = FindNearestTarget("Respawn");
                if (target != null)
                {
                    bullet.rotSpeed = effect.RotSpeed;
                    bullet.target = target.transform;
                }
            }

            if (effect.hitClip != null) bullet.onHitClip = effect.hitClip;
            if (effect.bulletClip != null) bullet.bulletClip = effect.bulletClip;

            var collider = bulletObj.gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.6f;
        }
    }

    // helper to find nearest target by tag (same as BulletShooter)
    public GameObject FindNearestTarget(string tag)
    {
        var gameObjects = GameObject.FindGameObjectsWithTag(tag);
        if (gameObjects == null || gameObjects.Length == 0) return null;
        GameObject nearest = null;
        float bestDist = float.MaxValue;
        foreach (var g in gameObjects)
        {
            float d = Vector3.Distance(transform.position, g.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                nearest = g;
            }
        }
        return nearest;
    }

    // Preserve existing Interactable integration: delegate to inventory
    // Keep overload to support callers that pass source GameObject and autoDestroy flag
    public void PickupItem(ItemType type, int value)
    {
        PickupItem(type, value, null, true, Vector3.zero);
    }

    public void PickupItem(ItemType type, int value, GameObject sourceObject, bool autoDestroy)
    {
        PickupItem(type, value, sourceObject, autoDestroy, Vector3.zero);
    }

    public void PickupItem(ItemType type, int value, GameObject sourceObject, bool autoDestroy, Vector3 perItemOffset)
    {
        if (_inventory != null)
            _inventory.PickupItem(type, value, sourceObject, autoDestroy, perItemOffset);
    }

    // Optional helpers for other systems
    public PlayerController Controller => _controller;
    public PlayerInventory Inventory => _inventory;
}
