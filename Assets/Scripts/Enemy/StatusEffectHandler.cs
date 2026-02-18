using System.Collections.Generic;
using UnityEngine;

public class StatusEffectHandler : MonoBehaviour
{
    [Header("이펙트 프리팹 (프리팹에서 설정)")]
    [SerializeField] private GameObject burnEffectPrefab;
    [SerializeField] private GameObject slowEffectPrefab;
    [SerializeField] private GameObject stunEffectPrefab;

    private class ActiveEffect
    {
        public StatusEffectType Type;
        public float RemainingTime;
        public float TickTimer;
    }

    private class VisualEffect
    {
        public ParticleSystem ParticleSystem;
        public float OriginalRate;
    }

    private readonly Dictionary<StatusEffectType, ActiveEffect> activeEffects = new();
    private readonly Dictionary<StatusEffectType, VisualEffect> visuals = new();
    private Enemy enemy;
    private WaypointFollower follower;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        follower = GetComponent<WaypointFollower>();
        InitVisual(StatusEffectType.Burn, burnEffectPrefab);
        InitVisual(StatusEffectType.Slow, slowEffectPrefab);
        InitVisual(StatusEffectType.Stun, stunEffectPrefab);
    }

    private void InitVisual(StatusEffectType type, GameObject prefab)
    {
        if (prefab == null) return;

        var obj = Instantiate(prefab, transform);
        obj.transform.localPosition = Vector3.zero;

        var ps = obj.GetComponent<ParticleSystem>();
        if (ps == null) return;

        var emission = ps.emission;
        float originalRate = emission.rateOverTimeMultiplier;
        emission.rateOverTimeMultiplier = 0f;

        visuals[type] = new VisualEffect
        {
            ParticleSystem = ps,
            OriginalRate = originalRate
        };
    }

    public void ApplyEffect(StatusEffectType type, float duration)
    {
        if (type == StatusEffectType.None) return;

        if (activeEffects.TryGetValue(type, out var existing))
        {
            if (duration > existing.RemainingTime)
                existing.RemainingTime = duration;
            return;
        }

        var effect = new ActiveEffect
        {
            Type = type,
            RemainingTime = duration,
            TickTimer = GameConstants.BurnTickInterval
        };

        activeEffects[type] = effect;
        SetEmission(type, true);
        UpdateSpeedMultiplier();
    }

    private void Update()
    {
        if (activeEffects.Count == 0) return;

        var toRemove = new List<StatusEffectType>();

        foreach (var kvp in new List<KeyValuePair<StatusEffectType, ActiveEffect>>(activeEffects))
        {
            var effect = kvp.Value;
            effect.RemainingTime -= Time.deltaTime;

            if (effect.RemainingTime <= 0f)
            {
                toRemove.Add(kvp.Key);
                continue;
            }

            if (effect.Type == StatusEffectType.Burn)
            {
                effect.TickTimer -= Time.deltaTime;
                if (effect.TickTimer <= 0f)
                {
                    enemy.TakeDamage(GameConstants.BurnDamagePerTick);
                    effect.TickTimer += GameConstants.BurnTickInterval;
                }
            }
        }

        foreach (var type in toRemove)
        {
            RemoveEffect(type);
        }
    }

    private void RemoveEffect(StatusEffectType type)
    {
        if (activeEffects.Remove(type))
        {
            SetEmission(type, false);
            UpdateSpeedMultiplier();
        }
    }

    public void ClearAll()
    {
        foreach (var kvp in activeEffects)
            SetEmission(kvp.Key, false);

        activeEffects.Clear();
        UpdateSpeedMultiplier();
    }

    private void SetEmission(StatusEffectType type, bool active)
    {
        if (!visuals.TryGetValue(type, out var visual)) return;

        var emission = visual.ParticleSystem.emission;
        emission.rateOverTimeMultiplier = active ? visual.OriginalRate : 0f;
    }

    private void UpdateSpeedMultiplier()
    {
        if (follower == null) return;

        if (activeEffects.ContainsKey(StatusEffectType.Stun))
        {
            follower.SpeedMultiplier = 0f;
            return;
        }

        if (activeEffects.ContainsKey(StatusEffectType.Slow))
        {
            follower.SpeedMultiplier = GameConstants.SlowSpeedMultiplier;
            return;
        }

        follower.SpeedMultiplier = 1f;
    }
}
