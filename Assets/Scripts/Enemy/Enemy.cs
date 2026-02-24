using System;
using UnityEngine;

[RequireComponent(typeof(WaypointFollower), typeof(StatusEffectHandler))]
public class Enemy : MonoBehaviour
{
    [Header("적 데이터")]
    [SerializeField] private EnemyLayerColors layerColors;

    private readonly int hp = 1;
    private readonly int shieldHp = GameConstants.DefaultShieldHp;

    [Header("이펙트")]
    [SerializeField] private ParticleSystem hitEffect;

    [Header("변형 외관")]
    [SerializeField] private GameObject bodyOutline;
    [SerializeField] private GameObject enhancedVisual;
    [SerializeField] private GameObject enhancedOutline;
    [SerializeField] private GameObject shieldVisual;
    [SerializeField] private SpriteRenderer[] colorExcludes;

    private int currentHp;
    private int currentShieldHp;
    private EnemyLayer currentLayer;
    private EnemyVariant currentVariant;
    private const int SortingGap = 100;
    private const int SortingRange = SortingGap * 10000;
    private static int nextSortingIndex;
    private static int NextSortingOrder => nextSortingIndex-- % SortingRange;

    private SpriteRenderer[] spriteRenderers;
    private SpriteRenderer bodyRenderer;
    private WaypointFollower follower;
    private StatusEffectHandler statusEffectHandler;
    private WaypointPath path;
    private bool deactivated;

    public EnemyLayer CurrentLayer => currentLayer;
    public EnemyVariant CurrentVariant => currentVariant;
    public int CurrentHp => currentHp;
    public WaypointFollower Follower => follower;

    private bool IsEnhanced => currentVariant is EnemyVariant.Enhanced or EnemyVariant.EnhancedShielded;
    private bool HasShield => currentShieldHp > 0;

    public static event Action<Enemy> OnEnemyDestroyed;
    public static event Action<Enemy> OnEnemyReachedBase;

    private void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        bodyRenderer = GetComponent<SpriteRenderer>();
        follower = GetComponent<WaypointFollower>();
        statusEffectHandler = GetComponent<StatusEffectHandler>();

        gameObject.layer = LayerMask.NameToLayer(GameConstants.LayerEnemy);
        SetSortingLayer(GameConstants.SortEnemy);
    }

    private void SetSortingLayer(string layerName)
    {
        foreach (var sr in spriteRenderers)
            sr.sortingLayerName = layerName;
    }

    private void ApplySortingOrder()
    {   
        SetSortingOrder(shieldVisual, NextSortingOrder);    
        SetSortingOrder(enhancedVisual, NextSortingOrder);
        
        bodyRenderer.sortingOrder = NextSortingOrder;

        SetSortingOrder(enhancedOutline, NextSortingOrder);
        SetSortingOrder(bodyOutline, NextSortingOrder);

        nextSortingIndex += SortingGap;
    }

    private static void SetSortingOrder(GameObject visual, int order)
    {
        if (!visual) return;
        foreach (var sr in visual.GetComponentsInChildren<SpriteRenderer>(true))
            sr.sortingOrder = order;
    }

    private void SetColor(Color color)
    {
        foreach (var sr in spriteRenderers)
        {
            if (System.Array.IndexOf(colorExcludes, sr) < 0)
                sr.color = color;
        }
    }

    private void ApplyVariantVisual()
    {
        bool hasShield = currentVariant is EnemyVariant.Shielded or EnemyVariant.EnhancedShielded;
        bool isEnhanced = currentVariant is EnemyVariant.Enhanced or EnemyVariant.EnhancedShielded;

        if (shieldVisual) shieldVisual.SetActive(hasShield);
        if (enhancedVisual) enhancedVisual.SetActive(isEnhanced);
    }

    public void Initialize(EnemyLayer startLayer, WaypointPath waypointPath,
        EnemyVariant variant = EnemyVariant.Normal)
    {
        path = waypointPath;
        currentLayer = startLayer;
        currentVariant = variant;
        currentHp = GetLayerHp();
        currentShieldHp = IsShieldVariant(variant) ? shieldHp : 0;

        deactivated = false;
        statusEffectHandler.ClearAll();
        ApplySortingOrder();
        SetColor(layerColors.GetColor(currentLayer));
        ApplyVariantVisual();

        float speed = GameConstants.GetEnemySpeed(currentLayer);
        if (IsEnhanced) speed *= GameConstants.EnhancedSpeedMultiplier;
        follower.Initialize(path, speed);
        follower.OnReachedEnd += OnReachBase;

        gameObject.SetActive(true);
    }

    private int GetLayerHp()
    {
        return IsEnhanced ? hp * GameConstants.EnhancedMultiplier : hp;
    }

    private int GetReward()
    {
        int baseReward = GameConstants.BaseLayerReward;
        return IsEnhanced ? baseReward * GameConstants.EnhancedMultiplier : baseReward;
    }

    private static bool IsShieldVariant(EnemyVariant variant)
    {
        return variant is EnemyVariant.Shielded or EnemyVariant.EnhancedShielded;
    }

    public void TakeDamage(int damage)
    {
        if (deactivated) return;

        if (HasShield)
        {
            currentShieldHp -= damage;
            PlayHitEffect();
            if (currentShieldHp <= 0)
            {
                int overflow = -currentShieldHp;
                BreakShield();
                if (overflow > 0) TakeDamage(overflow);
            }
            return;
        }

        currentHp -= damage;
        PlayHitEffect();
        if (currentHp <= 0)
        {
            PopLayer();
        }
    }

    /// <summary>
    /// 관통 탄환용: layerCount만큼 레이어를 한 번에 벗긴다.
    /// 반환값은 실제 소비된 레이어 수.
    /// </summary>
    public int TakeLayerDamage(int layerCount)
    {
        if (deactivated) return 0;

        int shieldConsumed = 0;
        if (HasShield)
        {
            int shieldDamage = Mathf.Min(layerCount, currentShieldHp);
            currentShieldHp -= shieldDamage;
            shieldConsumed = shieldDamage;
            layerCount -= shieldDamage;
            PlayHitEffect();

            if (currentShieldHp <= 0)
                BreakShield();

            if (layerCount <= 0) return shieldConsumed;
        }

        int consumed = Mathf.Min(layerCount, (int)currentLayer);

        for (int i = 0; i < consumed; i++)
        {
            ResourceManager.Instance.AddGold(GetReward());
            OnEnemyDestroyed?.Invoke(this);
            PlayHitEffect();
            currentLayer--;
        }

        if (currentLayer >= EnemyLayer.Red)
        {
            currentHp = GetLayerHp();
            SetColor(layerColors.GetColor(currentLayer));
            ApplyLayerSpeed();
        }
        else
        {
            Deactivate();
        }

        return shieldConsumed + consumed;
    }

    /// <summary>현재 레이어 하나를 벗긴다.</summary>
    private void PopLayer(bool giveReward = true)
    {
        if (giveReward) ResourceManager.Instance.AddGold(GetReward());
        OnEnemyDestroyed?.Invoke(this);

        EnemyLayer lowerLayer = currentLayer - 1;
        if (lowerLayer >= EnemyLayer.Red)
        {
            currentLayer = lowerLayer;
            currentHp = GetLayerHp();
            SetColor(layerColors.GetColor(currentLayer));
            ApplyLayerSpeed();
        }
        else
        {
            Deactivate();
        }
    }

    private void BreakShield(bool giveReward = true)
    {
        if (giveReward) ResourceManager.Instance.AddGold(shieldHp);
        currentShieldHp = 0;

        if (currentVariant == EnemyVariant.Shielded)
            currentVariant = EnemyVariant.Normal;
        else if (currentVariant == EnemyVariant.EnhancedShielded)
            currentVariant = EnemyVariant.Enhanced;

        ApplyVariantVisual();
    }

    private void ApplyLayerSpeed()
    {
        float speed = GameConstants.GetEnemySpeed(currentLayer);
        if (IsEnhanced) speed *= GameConstants.EnhancedSpeedMultiplier;
        follower.SetSpeed(speed);
    }

    private void OnReachBase()
    {
        ResourceManager.Instance.LoseLife((int)currentLayer);
        OnEnemyReachedBase?.Invoke(this);
        Deactivate();
    }

    /// <summary>모든 레이어를 벗기고 적을 제거한다.</summary>
    public void PopAll(bool giveReward = true)
    {
        if (deactivated) return;

        if (HasShield) BreakShield(giveReward);

        while (currentLayer >= EnemyLayer.Red)
        {
            if (giveReward) ResourceManager.Instance.AddGold(GetReward());
            OnEnemyDestroyed?.Invoke(this);
            currentLayer--;
        }

        Deactivate();
    }

    public void ApplyStatusEffect(StatusEffectType type, float duration)
    {
        if (deactivated) return;

        float resistance = IsEnhanced ? GameConstants.EnhancedStatusResistance : 0f;
        float reducedDuration = duration * (1f - resistance);
        if (reducedDuration <= 0f) return;

        statusEffectHandler.ApplyEffect(type, reducedDuration);
    }

    private void PlayHitEffect()
    {
        if (hitEffect != null)
        {
            var ps = Instantiate(hitEffect, transform.position, Quaternion.identity);
            var main = ps.main;
            main.stopAction = ParticleSystemStopAction.Destroy;
            ps.Play();
        }
    }

    private void Deactivate()
    {
        if (deactivated) return;
        deactivated = true;
        statusEffectHandler.ClearAll();
        if (shieldVisual) shieldVisual.SetActive(false);
        if (enhancedVisual) enhancedVisual.SetActive(false);
        follower.OnReachedEnd -= OnReachBase;
        EnemySpawner.Instance.Return(gameObject);
    }
}
