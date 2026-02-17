using System;
using UnityEngine;

[RequireComponent(typeof(WaypointFollower), typeof(StatusEffectHandler))]
public class Balloon : MonoBehaviour
{
    [Header("풍선 데이터")]
    [SerializeField] private int hp = 1;
    [SerializeField] private int energyReward = 1;
    [SerializeField, Range(0f, 1f)] private float statusEffectResistance;
    [SerializeField] private int shieldHp = GameConstants.DefaultShieldHp;

    [Header("변형 외관")]
    [SerializeField] private GameObject bodyOutline;
    [SerializeField] private GameObject enhancedVisual;
    [SerializeField] private GameObject enhancedOutline;
    [SerializeField] private GameObject shieldVisual;
    [SerializeField] private SpriteRenderer[] colorExcludes;

    private int currentHp;
    private int currentShieldHp;
    private BalloonLayer currentLayer;
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

    public BalloonLayer CurrentLayer => currentLayer;
    public EnemyVariant CurrentVariant => currentVariant;
    public int CurrentHp => currentHp;
    public WaypointFollower Follower => follower;

    private bool IsEnhanced => currentVariant is EnemyVariant.Enhanced or EnemyVariant.EnhancedShielded;
    private bool HasShield => currentShieldHp > 0;

    public static event Action<Balloon> OnBalloonDestroyed;
    public static event Action<Balloon> OnBalloonReachedBase;

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

    public void Initialize(BalloonLayer startLayer, WaypointPath waypointPath,
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
        SetColor(GameConstants.GetBalloonColor(currentLayer));
        ApplyVariantVisual();

        float speed = GameConstants.GetBalloonSpeed(currentLayer);
        if (IsEnhanced) speed *= GameConstants.EnhancedSpeedMultiplier;
        follower.Initialize(path, speed);
        follower.OnReachedEnd += OnReachBase;

        gameObject.SetActive(true);
    }

    private int GetLayerHp()
    {
        return IsEnhanced ? hp * GameConstants.EnhancedHpMultiplier : hp;
    }

    private int GetReward()
    {
        return IsEnhanced ? energyReward * GameConstants.EnhancedRewardMultiplier : energyReward;
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
            if (currentShieldHp <= 0)
            {
                int overflow = -currentShieldHp;
                BreakShield();
                if (overflow > 0) TakeDamage(overflow);
            }
            return;
        }

        currentHp -= damage;
        if (currentHp <= 0)
        {
            DestroyLayer();
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
            BreakShield();
            shieldConsumed = 1;
            layerCount--;
            if (layerCount <= 0) return shieldConsumed;
        }

        int consumed = Mathf.Min(layerCount, (int)currentLayer);
        int reward = GetReward();

        for (int i = 0; i < consumed; i++)
        {
            ResourceManager.Instance.AddEnergy(reward);
            OnBalloonDestroyed?.Invoke(this);
        }

        BalloonLayer targetLayer = (BalloonLayer)((int)currentLayer - consumed);
        if (targetLayer >= BalloonLayer.Red)
        {
            currentLayer = targetLayer;
            currentHp = GetLayerHp();
            SetColor(GameConstants.GetBalloonColor(currentLayer));
            ApplyLayerSpeed();
        }
        else
        {
            Deactivate();
        }

        return shieldConsumed + consumed;
    }

    private void DestroyLayer()
    {
        ResourceManager.Instance.AddEnergy(GetReward());
        OnBalloonDestroyed?.Invoke(this);

        BalloonLayer lowerLayer = currentLayer - 1;
        if (lowerLayer >= BalloonLayer.Red)
        {
            currentLayer = lowerLayer;
            currentHp = GetLayerHp();
            SetColor(GameConstants.GetBalloonColor(currentLayer));
            ApplyLayerSpeed();
        }
        else
        {
            Deactivate();
        }
    }

    private void BreakShield()
    {
        currentShieldHp = 0;

        if (currentVariant == EnemyVariant.Shielded)
            currentVariant = EnemyVariant.Normal;
        else if (currentVariant == EnemyVariant.EnhancedShielded)
            currentVariant = EnemyVariant.Enhanced;

        ApplyVariantVisual();
    }

    private void ApplyLayerSpeed()
    {
        float speed = GameConstants.GetBalloonSpeed(currentLayer);
        if (IsEnhanced) speed *= GameConstants.EnhancedSpeedMultiplier;
        follower.SetSpeed(speed);
    }

    private void OnReachBase()
    {
        ResourceManager.Instance.LoseLife((int)currentLayer);
        OnBalloonReachedBase?.Invoke(this);
        Deactivate();
    }

    public void ApplyStatusEffect(StatusEffectType type, float duration)
    {
        if (deactivated) return;

        float reducedDuration = duration * (1f - statusEffectResistance);
        if (reducedDuration <= 0f) return;

        statusEffectHandler.ApplyEffect(type, reducedDuration);
    }

    private void Deactivate()
    {
        if (deactivated) return;
        deactivated = true;
        statusEffectHandler.ClearAll();
        if (shieldVisual) shieldVisual.SetActive(false);
        if (enhancedVisual) enhancedVisual.SetActive(false);
        follower.OnReachedEnd -= OnReachBase;
        BalloonSpawner.Instance.Return(gameObject);
    }
}
