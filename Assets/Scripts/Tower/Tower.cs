using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("타워 데이터 (프리팹에서 설정)")]
    [SerializeField] private string towerName;
    [SerializeField] private int cost;
    [SerializeField] private float attackDamage;
    [SerializeField] private float attackInterval = 1f;
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private int sellRefund;
    [SerializeField] private bool isAttacker = true;
    [SerializeField] private float energyPerTick;
    [SerializeField] private float energyTickInterval;
    [SerializeField] private float splashRadius;
    [SerializeField] private float rotationSpeed = 360f;

    [Header("상태이상 (프리팹에서 설정)")]
    [SerializeField] private StatusEffectType statusEffectType = StatusEffectType.None;
    [SerializeField] private float effectDuration;

    [Header("업그레이드")]
    [SerializeField] private TowerUpgradeData upgradeData;
    [SerializeField] private GameObject mainVisual2;
    [SerializeField] private GameObject mainVisual3;
    [SerializeField] private GameObject mainVisual4;
    [SerializeField] private GameObject subVisualA;
    [SerializeField] private GameObject subVisualB;

    [SerializeField] private Transform firePoint;
    [SerializeField] private SpriteRenderer rangeIndicator;

    private float attackTimer;
    private int enemyLayerMask;
    private bool initialized;
    private Balloon currentTarget;

    private int mainLevel = 1;
    private UpgradeTrack selectedSub = UpgradeTrack.None;
    private int totalUpgradeCost;

    public string TowerName => towerName;
    public int Cost => cost;
    public float AttackDamage => attackDamage;
    public float AttackInterval => attackInterval;
    public float AttackRange => attackRange;
    public int SellRefund => Mathf.RoundToInt((cost + totalUpgradeCost) * GameConstants.SellRefundRate);
    public bool IsAttacker => isAttacker;
    public float SplashRadius => splashRadius;
    public SpriteRenderer RangeIndicator => rangeIndicator;
    public TargetPriority Priority { get; private set; } = TargetPriority.First;

    public int MainLevel => mainLevel;
    public UpgradeTrack SelectedSub => selectedSub;
    public bool CanUpgradeMain => upgradeData != null && mainLevel < 4;
    public bool CanSelectSub => upgradeData != null && selectedSub == UpgradeTrack.None;

    /// <summary>런타임 프리팹 생성 시 데이터 설정</summary>
    public void SetupData(string name, int towerCost, float damage, float interval, float range, int refund,
        bool attacker = true, float enerPerTick = 0, float enerInterval = 0, float rotSpeed = 360f,
        float splash = 0f)
    {
        towerName = name;
        cost = towerCost;
        attackDamage = damage;
        attackInterval = interval;
        attackRange = range;
        sellRefund = refund;
        isAttacker = attacker;
        energyPerTick = enerPerTick;
        energyTickInterval = enerInterval;
        rotationSpeed = rotSpeed;
        splashRadius = splash;
    }

    public void Initialize()
    {
        gameObject.layer = LayerMask.NameToLayer(GameConstants.LayerTower);
        SetSortingLayer(GameConstants.SortTower);

        attackTimer = 0f;
        enemyLayerMask = 1 << LayerMask.NameToLayer(GameConstants.LayerEnemy);
        initialized = true;

        if (upgradeData != null)
            RecalculateStats();

        if (!isAttacker)
        {
            var generator = GetComponent<TowerEnergyGenerator>();
            if (generator != null) generator.Initialize(energyPerTick, energyTickInterval);
        }
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
        float dmg = 0, interval = 0, range = 0, splash = 0;

        // 주 모듈: main1(절대값) + main2~N(증분) 합산
        for (int i = 1; i <= mainLevel; i++)
        {
            var level = GetMainLevel(i);
            if (level == null) continue;
            dmg += level.attackDamage;
            interval += level.attackInterval;
            range += level.attackRange;
            splash += level.splashRadius;
        }

        // 서브 모듈 보너스
        if (selectedSub != UpgradeTrack.None)
        {
            var sub = GetSubData(selectedSub);
            if (sub != null)
            {
                dmg += sub.attackDamage;
                interval += sub.attackInterval;
                range += sub.attackRange;
                splash += sub.splashRadius;
            }
        }

        attackDamage = dmg;
        attackInterval = interval;
        attackRange = range;
        splashRadius = splash;

        // 상태이상: 타입별 duration 합산
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
            float diameter = attackRange * 2f;
            Vector3 parentScale = rangeIndicator.transform.parent.lossyScale;
            rangeIndicator.transform.localScale = new Vector3(
                diameter / parentScale.x,
                diameter / parentScale.y,
                1f);
        }
    }

    private void Update()
    {
        if (!initialized || !isAttacker) return;

        // 매 프레임 우선순위에 따라 최적 타겟 재평가
        currentTarget = TargetSelector.SelectTarget(transform.position, attackRange, Priority, enemyLayerMask);

        // 타겟을 향해 회전
        if (currentTarget != null)
        {
            LookAt(currentTarget.transform.position);
        }

        // 발사 라인에 적이 있으면 공격
        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f && HasEnemyInFireLine())
        {
            Attack();
            attackTimer = attackInterval;
        }
    }

    private void LookAt(Vector3 targetPos)
    {
        Vector2 dir = (targetPos - transform.position).normalized;
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        float currentAngle = transform.eulerAngles.z;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0, 0, newAngle);
    }

    private bool HasEnemyInFireLine()
    {
        Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        RaycastHit2D hit = Physics2D.Raycast(origin, transform.up, attackRange, enemyLayerMask);
        return hit.collider != null;
    }

    private void Attack()
    {
        if (ProjectilePool.Instance == null) return;
        var projObj = ProjectilePool.Instance.Get();
        if (projObj == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        projObj.transform.position = spawnPos;
        projObj.transform.rotation = transform.rotation;
        var projectile = projObj.GetComponent<Projectile>();
        projectile.Initialize((int)attackDamage, splashRadius,
            statusEffectType, effectDuration);
    }

    private void SetSortingLayer(string layerName)
    {
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            sr.sortingLayerName = layerName;
    }

    private void SetColor(Color color)
    {
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            if (sr != rangeIndicator)
                sr.color = color;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
