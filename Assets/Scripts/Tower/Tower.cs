using UnityEngine;

public class Tower : MonoBehaviour
{
    private readonly float rotationSpeed = 360f;

    private StatusEffectType statusEffectType;
    private float effectDuration;

    [Header("업그레이드")]
    [SerializeField] private TowerUpgradeData upgradeData;
    [SerializeField] private GameObject mainVisual2;
    [SerializeField] private GameObject mainVisual3;
    [SerializeField] private GameObject mainVisual4;
    [SerializeField] private GameObject subVisualA;
    [SerializeField] private GameObject subVisualB;

    [SerializeField] private Transform body;
    [SerializeField] private Transform firePoint;
    [SerializeField] private SpriteRenderer rangeIndicator;

    private TowerStats currentStats = new();
    private float attackTimer;
    private int enemyLayerMask;
    private bool initialized;
    private Balloon currentTarget;

    private int mainLevel = 1;
    private UpgradeTrack selectedSub = UpgradeTrack.None;
    private int totalUpgradeCost;

    public string TowerName => upgradeData != null ? upgradeData.towerName : "";
    public int Cost => upgradeData != null ? upgradeData.main1.cost : 0;
    public float AttackDamage => currentStats.attackDamage;
    public float AttackInterval => currentStats.attackInterval;
    public float AttackRange => currentStats.attackRange;
    public int SellRefund => Mathf.RoundToInt((Cost + totalUpgradeCost) * GameConstants.SellRefundRate);
    public float SplashRadius => currentStats.splashRadius;
    public SpriteRenderer RangeIndicator => rangeIndicator;
    public TargetPriority Priority { get; private set; } = TargetPriority.First;

    public int MainLevel => mainLevel;
    public UpgradeTrack SelectedSub => selectedSub;
    public bool CanUpgradeMain => upgradeData != null && mainLevel < 4;
    public bool CanSelectSub => upgradeData != null && selectedSub == UpgradeTrack.None;

    public void Initialize()
    {
        gameObject.layer = LayerMask.NameToLayer(GameConstants.LayerTower);
        SetSortingLayer(GameConstants.SortTower);

        attackTimer = 0f;
        enemyLayerMask = 1 << LayerMask.NameToLayer(GameConstants.LayerEnemy);
        initialized = true;

        if (upgradeData != null)
            RecalculateStats();
    }

    public void SetTargetPriority(TargetPriority priority)
    {
        Priority = priority;
    }

    public void Sell()
    {
        ResourceManager.Instance.AddEnergy(SellRefund);
        Destroy(gameObject);
    }

    #region 업그레이드

    public bool UpgradeMain()
    {
        if (!CanUpgradeMain) return false;

        var nextData = GetMainLevel(mainLevel + 1);
        if (nextData == null) return false;

        if (!ResourceManager.Instance.SpendEnergy(nextData.cost))
            return false;

        totalUpgradeCost += nextData.cost;
        mainLevel++;

        RecalculateStats();
        ActivateVisual(GetMainVisual(mainLevel));
        return true;
    }

    public bool SelectSub(UpgradeTrack sub)
    {
        if (!CanSelectSub) return false;
        if (sub == UpgradeTrack.None) return false;

        var subData = GetSubData(sub);
        if (subData == null) return false;

        if (!ResourceManager.Instance.SpendEnergy(subData.cost))
            return false;

        totalUpgradeCost += subData.cost;
        selectedSub = sub;

        RecalculateStats();
        ActivateVisual(sub == UpgradeTrack.A ? subVisualA : subVisualB);
        return true;
    }

    public UpgradeLevel GetNextMainInfo()
    {
        if (!CanUpgradeMain) return null;
        return GetMainLevel(mainLevel + 1);
    }

    public UpgradeLevel GetSubAInfo()
    {
        if (upgradeData == null) return null;
        return upgradeData.subA;
    }

    public UpgradeLevel GetSubBInfo()
    {
        if (upgradeData == null) return null;
        return upgradeData.subB;
    }

    private UpgradeLevel GetMainLevel(int level)
    {
        if (upgradeData == null) return null;
        return level switch
        {
            1 => upgradeData.main1,
            2 => upgradeData.main2,
            3 => upgradeData.main3,
            4 => upgradeData.main4,
            _ => null
        };
    }

    private UpgradeLevel GetSubData(UpgradeTrack sub)
    {
        if (upgradeData == null) return null;
        return sub switch
        {
            UpgradeTrack.A => upgradeData.subA,
            UpgradeTrack.B => upgradeData.subB,
            _ => null
        };
    }

    private void RecalculateStats()
    {
        var stats = new TowerStats();

        for (int i = 1; i <= mainLevel; i++)
        {
            var level = GetMainLevel(i);
            if (level != null) stats.Add(level.stats);
        }

        if (selectedSub != UpgradeTrack.None)
        {
            var sub = GetSubData(selectedSub);
            if (sub != null) stats.Add(sub.stats);
        }

        currentStats = stats;
        RecalculateStatusEffects();
    }

    private void RecalculateStatusEffects()
    {
        int enumCount = System.Enum.GetValues(typeof(StatusEffectType)).Length;
        float[] durations = new float[enumCount];

        for (int i = 1; i <= mainLevel; i++)
            AccumulateEffects(GetMainLevel(i), durations);

        if (selectedSub != UpgradeTrack.None)
            AccumulateEffects(GetSubData(selectedSub), durations);

        // Projectile 호환: 첫 번째 유효 효과 사용
        statusEffectType = StatusEffectType.None;
        effectDuration = 0f;
        for (int i = 1; i < durations.Length; i++)
        {
            if (durations[i] > 0)
            {
                statusEffectType = (StatusEffectType)i;
                effectDuration = durations[i];
                break;
            }
        }
    }

    private void AccumulateEffects(UpgradeLevel level, float[] durations)
    {
        if (level?.statusEffects == null) return;
        foreach (var e in level.statusEffects)
            durations[(int)e.type] += e.duration;
    }

    private GameObject GetMainVisual(int level) => level switch
    {
        2 => mainVisual2,
        3 => mainVisual3,
        4 => mainVisual4,
        _ => null
    };

    private void ActivateVisual(GameObject visual)
    {
        if (visual == null) return;
        visual.SetActive(true);
        foreach (var sr in visual.GetComponentsInChildren<SpriteRenderer>())
            sr.sortingLayerName = GameConstants.SortTower;
    }

    #endregion

    public void ShowRange(bool show)
    {
        if (rangeIndicator != null)
        {
            rangeIndicator.gameObject.SetActive(show);
            float range = currentStats.attackRange;
            if (range <= 0f && upgradeData != null)
                range = upgradeData.main1.stats.attackRange;
            float diameter = range * 2f;
            Vector3 parentScale = rangeIndicator.transform.parent.lossyScale;
            rangeIndicator.transform.localScale = new Vector3(
                diameter / parentScale.x,
                diameter / parentScale.y,
                1f);
        }
    }

    private void Update()
    {
        if (!initialized) return;

        // 매 프레임 우선순위에 따라 최적 타겟 재평가
        currentTarget = TargetSelector.SelectTarget(transform.position, currentStats.attackRange, Priority, enemyLayerMask);

        // 타겟을 향해 회전
        if (currentTarget != null)
        {
            LookAt(currentTarget.transform.position);
        }

        // 타겟이 있고 발사 라인에 적이 있으면 공격
        if (currentTarget != null)
        {
            if (attackTimer <= 0f && HasEnemyInFireLine())
            {
                Attack();
                attackTimer = currentStats.attackInterval;
            }
        }

        attackTimer = Mathf.Max(attackTimer - Time.deltaTime, 0f);
    }

    private void LookAt(Vector3 targetPos)
    {
        Vector2 dir = (targetPos - transform.position).normalized;
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        float currentAngle = body.eulerAngles.z;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);
        body.rotation = Quaternion.Euler(0, 0, newAngle);
    }

    private bool HasEnemyInFireLine()
    {
        Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        RaycastHit2D hit = Physics2D.Raycast(origin, body.up, currentStats.attackRange, enemyLayerMask);
        return hit.collider != null;
    }

    private void Attack()
    {
        if (ProjectilePool.Instance == null) return;
        var projObj = ProjectilePool.Instance.Get();
        if (projObj == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        projObj.transform.position = spawnPos;
        projObj.transform.rotation = body.rotation;
        var projectile = projObj.GetComponent<Projectile>();
        projectile.Initialize((int)currentStats.attackDamage, currentStats.splashRadius,
            statusEffectType, effectDuration, currentStats.pierceCount);
    }

    private void SetSortingLayer(string layerName)
    {
        if (body == null) return;

        foreach (var sr in body.GetComponentsInChildren<SpriteRenderer>())
            sr.sortingLayerName = layerName;

        var bodySr = body.GetComponent<SpriteRenderer>();
        if (bodySr != null)
            bodySr.sortingOrder = -1;
    }

}
