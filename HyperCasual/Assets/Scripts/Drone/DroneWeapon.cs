using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using UnityEngine;

// DroneWeapon uses reflection to read BulletDatas and EffectsGroup so it can compile in firstpass assembly
public class DroneWeapon : MonoBehaviour
{
    [Header("Projectile Data")]
    [Tooltip("Assign the BulletDatas ScriptableObject here (use Object type to avoid compile-time dependency)")]
    public UnityEngine.Object BulletDataObject;

    [Tooltip("Index into BulletData.Effects to use (defaults to 0)")]
    public int EffectIndex = 0;

    [Tooltip("Optional muzzle transform. If null, this object's transform will be used.")]
    public Transform MuzzleTransform;

    [Header("Burst / Timing")]
    [Tooltip("Seconds between bursts (N)")]
    public float BurstInterval = 2f;

    [Tooltip("Number of shots per burst (M)")]
    public int ShotsPerBurst = 3;

    [Tooltip("Delay between shots inside a burst (seconds). Default 0.1s")]
    public float ShotSpacing = 0.1f;

    [Header("Damage")]
    [Tooltip("Fallback damage per shot (applied to Bullet.DamageAmount).")]
    public int Damage = 5;

    [Header("Accuracy")]
    [Tooltip("Max angle in degrees for random spread applied to each shot (both horizontal and vertical). Set 0 for perfect aim.")]
    public float AimSpreadAngle = 3f; // degrees

    private Component _droneComponent;
    private float _nextBurstTime = 0f;

    void Awake()
    {
        if (MuzzleTransform == null)
            MuzzleTransform = transform;

        foreach (var c in GetComponents<Component>())
        {
            var t = c.GetType();
            if (t.GetProperty("TargetEnemy") != null)
            {
                _droneComponent = c;
                break;
            }
            if (t.GetField("_targetEnemy", BindingFlags.NonPublic | BindingFlags.Instance) != null)
            {
                _droneComponent = c;
                break;
            }
        }
    }

    void Update()
    {
        if (_droneComponent == null) return;

        Transform target = GetDroneTarget();
        if (target != null && Time.time >= _nextBurstTime)
        {
            // attempt to read effects list from BulletDataObject
            var effectsList = GetEffectsListFromBulletData();
            if (effectsList == null) return;
            if (effectsList.Count == 0) return;

            _nextBurstTime = Time.time + BurstInterval;
            int idx = Mathf.Clamp(EffectIndex, 0, effectsList.Count - 1);
            var effect = effectsList[idx];
            StartCoroutine(FireBurst(effect, target));
        }
    }

    private Transform GetDroneTarget()
    {
        var ttype = _droneComponent.GetType();
        var prop = ttype.GetProperty("TargetEnemy");
        if (prop != null)
        {
            return prop.GetValue(_droneComponent, null) as Transform;
        }
        else
        {
            var field = ttype.GetField("_targetEnemy", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
                return field.GetValue(_droneComponent) as Transform;
        }
        return null;
    }

    // We represent an effect as a proxy that can read expected fields via reflection
    private class EffectProxy
    {
        public object RawObject;
        public Type RawType;
        public EffectProxy(object raw)
        {
            RawObject = raw;
            RawType = raw?.GetType();
        }

        public T GetFieldValue<T>(string name)
        {
            if (RawObject == null) return default;
            var f = RawType.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null)
            {
                var v = f.GetValue(RawObject);
                if (v is T) return (T)v;
                try { return (T)Convert.ChangeType(v, typeof(T)); } catch { return default; }
            }
            var p = RawType.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null)
            {
                var v = p.GetValue(RawObject);
                if (v is T) return (T)v;
                try { return (T)Convert.ChangeType(v, typeof(T)); } catch { return default; }
            }
            return default;
        }
    }

    private List<EffectProxy> GetEffectsListFromBulletData()
    {
        if (BulletDataObject == null) return null;
        var bdType = BulletDataObject.GetType();
        object effectsObj = null;
        var prop = bdType.GetProperty("Effects");
        if (prop != null) effectsObj = prop.GetValue(BulletDataObject);
        else
        {
            var field = bdType.GetField("Effects");
            if (field != null) effectsObj = field.GetValue(BulletDataObject);
        }

        if (effectsObj == null) return null;

        var list = new List<EffectProxy>();
        var ie = effectsObj as System.Collections.IEnumerable;
        if (ie == null) return null;
        foreach (var item in ie)
        {
            list.Add(new EffectProxy(item));
        }
        return list;
    }

    private IEnumerator FireBurst(EffectProxy effect, Transform target)
    {
        for (int i = 0; i < ShotsPerBurst; i++)
        {
            SpawnShot(effect, target);
            yield return new WaitForSeconds(ShotSpacing);
        }
    }

    private void SpawnShot(EffectProxy effectProxy, Transform target)
    {
        if (effectProxy == null) return;

        Vector3 spawnPos = MuzzleTransform != null ? MuzzleTransform.position : transform.position;

        Vector3 dir = transform.forward;
        if (target != null)
            dir = target.position - spawnPos;
        bool isFlat = effectProxy.GetFieldValue<bool>("isFlatShoot");
        if (isFlat) dir.y = 0;
        dir = dir.normalized;

        // apply random spread
        if (AimSpreadAngle > 0f)
        {
            // pick random small rotation within cone
            float half = AimSpreadAngle * 0.5f;
            float yaw = UnityEngine.Random.Range(-half, half);
            float pitch = UnityEngine.Random.Range(-half, half);
            Quaternion jitter = Quaternion.Euler(pitch, yaw, 0f);
            dir = jitter * dir;
        }

        // StartParticles
        var startParticles = effectProxy.GetFieldValue<ParticleSystem>("StartParticles");
        var startClip = effectProxy.GetFieldValue<AudioClip>("startClip");
        if (startParticles != null)
        {
            var startPar = Instantiate(startParticles, spawnPos, Quaternion.LookRotation(dir, Vector3.up));
            startPar.transform.forward = dir;
            var batType = FindTypeByName("BulletAudioTrigger");
            if (batType != null)
            {
                var comp = startPar.gameObject.AddComponent(batType);
                var f = batType.GetField("onClip");
                if (f != null) f.SetValue(comp, startClip);
            }
        }

        // BulletParticles
        var bulletParticles = effectProxy.GetFieldValue<ParticleSystem>("BulletParticles");
        if (bulletParticles != null)
        {
            var bulletObj = Instantiate(bulletParticles, spawnPos, Quaternion.LookRotation(dir, Vector3.up));
            bulletObj.transform.forward = dir;

            var bulletType = FindTypeByName("Bullet");
            Component bulletComp = null;
            if (bulletType != null)
            {
                bulletComp = bulletObj.gameObject.AddComponent(bulletType);
            }

            if (bulletComp != null)
            {
                SetFieldIfExists(bulletComp, "OnHitEffect", effectProxy.GetFieldValue<ParticleSystem>("HitParticles"));
                SetFieldIfExists(bulletComp, "Speed", effectProxy.GetFieldValue<float>("Speed"));
                SetFieldIfExists(bulletComp, "isTargeting", effectProxy.GetFieldValue<bool>("isTargeting"));
                SetFieldIfExists(bulletComp, "isFlatShoot", isFlat);
                if (effectProxy.GetFieldValue<bool>("isTargeting") && target != null)
                {
                    SetFieldIfExists(bulletComp, "rotSpeed", effectProxy.GetFieldValue<float>("RotSpeed"));
                    SetFieldIfExists(bulletComp, "target", target);
                }

                SetFieldIfExists(bulletComp, "DamageAmount", Damage);
                SetFieldIfExists(bulletComp, "onHitClip", effectProxy.GetFieldValue<AudioClip>("hitClip"));
                SetFieldIfExists(bulletComp, "bulletClip", effectProxy.GetFieldValue<AudioClip>("bulletClip"));
            }

            var col = bulletObj.gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.6f;
        }
    }

    private void SetFieldIfExists(Component comp, string name, object value)
    {
        if (comp == null || value == null) return;
        var t = comp.GetType();
        var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null) { f.SetValue(comp, value); return; }
        var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (p != null && p.CanWrite) { p.SetValue(comp, value); return; }
    }

    private Type FindTypeByName(string shortName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (var t in asm.GetTypes())
                {
                    if (t.Name == shortName) return t;
                }
            }
            catch { }
        }
        return null;
    }

    // Called by external systems when stage resets
    public void ResetWeapon()
    {
        StopAllCoroutines();
        _nextBurstTime = Time.time + BurstInterval;
    }
}
