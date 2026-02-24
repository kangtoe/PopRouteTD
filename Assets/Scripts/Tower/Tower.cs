using System.Collections;
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
    private Enemy currentTarget;
    private float projectileSpeed;

    private int mainLevel = 1;
    private bool hasSubA;
    private bool hasSubB;
    private int totalUpgradeCost;
    private Coroutine boingCoroutine;
    private Coroutine fireBoingCoroutine;
    private Vector3 originalScale;

    public string TowerName => upgradeData != null ? upgradeData.towerName : "";
    public int Cost => upgradeData != null ? upgradeData.main1.cost : 0;
    public float AttackInterval => currentStats.attackInterval;
    public float AttackRange => currentStats.attackRange;
    public int SellRefund => Mathf.RoundToInt((Cost + totalUpgradeCost) * GameConstants.SellRefundRate);
    public float SplashRadius => currentStats.splashRadius;
    public TowerUpgradeData UpgradeData => upgradeData;
    public Transform Body => body;
    public SpriteRenderer RangeIndicator => rangeIndicator;
    public TargetPriority Priority { get; private set; } = TargetPriority.First;

    public int MainLevel => mainLevel;
    public bool HasSubA => hasSubA;
    public bool HasSubB => hasSubB;
    public bool CanUpgradeMain => upgradeData != null && mainLevel < 4;

    public void Initialize()
    {
        gameObject.layer = LayerMask.NameToLayer(GameConstants.LayerTower);
        SetSortingLayer(GameConstants.SortTower);

        attackTimer = 0f;
        enemyLayerMask = 1 << LayerMask.NameToLayer(GameConstants.LayerEnemy);
        projectileSpeed = ProjectilePool.Instance != null ? ProjectilePool.Instance.ProjectileSpeed : 15f;
        initialized = true;

        originalScale = transform.localScale;

        if (upgradeData != null)
            RecalculateStats();

        PlayBoing();
    }

    public void SetTargetPriority(TargetPriority priority)
    {
        Priority = priority;
    }

    public void Sell()
    {
        ResourceManager.Instance.AddGold(SellRefund);
        initialized = false;
        if (boingCoroutine != null)
            StopCoroutine(boingCoroutine);
        StartCoroutine(SellBoingRoutine());
    }

    #region 업그레이드

    public bool UpgradeMain()
    {
        if (!CanUpgradeMain) return false;

        var nextData = GetMainLevel(mainLevel + 1);
        if (nextData == null) return false;

        if (!ResourceManager.Instance.SpendGold(nextData.cost))
            return false;

        totalUpgradeCost += nextData.cost;
        mainLevel++;

        RecalculateStats();
        ActivateVisual(GetMainVisual(mainLevel));
        PlayBoing();
        return true;
    }

    public bool SelectSub(UpgradeTrack sub)
    {
        if (upgradeData == null || sub == UpgradeTrack.None) return false;
        if (sub == UpgradeTrack.A && hasSubA) return false;
        if (sub == UpgradeTrack.B && hasSubB) return false;

        var subData = GetSubData(sub);
        if (subData == null) return false;

        if (!ResourceManager.Instance.SpendGold(subData.cost))
            return false;

        totalUpgradeCost += subData.cost;
        if (sub == UpgradeTrack.A) hasSubA = true;
        else hasSubB = true;

        RecalculateStats();
        ActivateVisual(sub == UpgradeTrack.A ? subVisualA : subVisualB);
        PlayBoing();
        return true;
    }

    public UpgradeLevel GetNextMainInfo()
    {
        if (!CanUpgradeMain) return null;
        return GetMainLevel(mainLevel + 1);
    }

    public UpgradeLevel GetCurrentMainInfo()
    {
        return GetMainLevel(mainLevel);
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

        if (hasSubA)
        {
            var sub = GetSubData(UpgradeTrack.A);
            if (sub != null) stats.Add(sub.stats);
        }
        if (hasSubB)
        {
            var sub = GetSubData(UpgradeTrack.B);
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

        if (hasSubA)
            AccumulateEffects(GetSubData(UpgradeTrack.A), durations);
        if (hasSubB)
            AccumulateEffects(GetSubData(UpgradeTrack.B), durations);

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

    public GameObject GetMainVisual(int level) => level switch
    {
        2 => mainVisual2,
        3 => mainVisual3,
        4 => mainVisual4,
        _ => null
    };

    public GameObject GetSubVisual(UpgradeTrack sub) => sub switch
    {
        UpgradeTrack.A => subVisualA,
        UpgradeTrack.B => subVisualB,
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

    private IEnumerator SellBoingRoutine()
    {
        const float boingDuration = 0.15f;
        const float shrinkDuration = 0.15f;
        const float amplitude = 0.2f;
        Vector3 baseScale = originalScale;
        float elapsed = 0f;

        // 한번 통통
        while (elapsed < boingDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / boingDuration;
            float wave = Mathf.Sin(t * Mathf.PI);
            float s = amplitude * wave;
            transform.localScale = new Vector3(baseScale.x * (1f - s * 0.5f), baseScale.y * (1f + s), baseScale.z);
            yield return null;
        }

        // 쪼그라들며 사라짐
        elapsed = 0f;
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shrinkDuration;
            float scale = 1f - t;
            transform.localScale = baseScale * scale;
            yield return null;
        }

        Destroy(gameObject);
    }

    private void PlayBoing()
    {
        if (boingCoroutine != null)
            StopCoroutine(boingCoroutine);
        transform.localScale = originalScale;
        boingCoroutine = StartCoroutine(BoingRoutine());
    }

    private IEnumerator BoingRoutine()
    {
        const float duration = 0.35f;
        const float amplitude = 0.15f;
        const float frequency = 3f;
        Vector3 baseScale = originalScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float decay = 1f - t;
            float wave = Mathf.Sin(t * frequency * Mathf.PI * 2f);
            float s = amplitude * decay * wave;
            transform.localScale = new Vector3(baseScale.x * (1f - s * 0.5f), baseScale.y * (1f + s), baseScale.z);
            yield return null;
        }

        transform.localScale = baseScale;
        boingCoroutine = null;
    }

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

        // 타겟을 향해 예측 위치로 회전 및 공격
        if (currentTarget != null)
        {
            Vector3 aimPos = PredictTargetPosition(currentTarget);
            LookAt(aimPos);

            if (attackTimer <= 0f && IsAimedAt(aimPos))
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

    private bool IsAimedAt(Vector3 targetPos)
    {
        Vector2 dir = ((Vector3)targetPos - transform.position).normalized;
        float angle = Vector2.Angle(body.up, dir);
        return angle < 5f;
    }

    private Vector3 PredictTargetPosition(Enemy target)
    {
        Vector3 targetPos = target.transform.position;
        Vector3 targetVel = target.Follower.Velocity;

        if (targetVel.sqrMagnitude < 0.001f)
            return targetPos;

        Vector3 relPos = targetPos - transform.position;
        float a = Vector3.Dot(targetVel, targetVel) - projectileSpeed * projectileSpeed;
        float b = 2f * Vector3.Dot(relPos, targetVel);
        float c = Vector3.Dot(relPos, relPos);

        float t;
        if (Mathf.Abs(a) < 0.001f)
        {
            // 선형 해: bt + c = 0
            if (Mathf.Abs(b) < 0.001f) return targetPos;
            t = -c / b;
            if (t < 0f) return targetPos;
        }
        else
        {
            float discriminant = b * b - 4f * a * c;
            if (discriminant < 0f) return targetPos;

            float sqrtD = Mathf.Sqrt(discriminant);
            float t1 = (-b - sqrtD) / (2f * a);
            float t2 = (-b + sqrtD) / (2f * a);

            if (t1 > 0f && t2 > 0f) t = Mathf.Min(t1, t2);
            else if (t1 > 0f) t = t1;
            else if (t2 > 0f) t = t2;
            else return targetPos;
        }

        return targetPos + targetVel * t;
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
        projectile.Initialize(currentStats.pierceCount, currentStats.splashRadius,
            statusEffectType, effectDuration, currentStats.areaTargets);

        if (fireBoingCoroutine != null)
            StopCoroutine(fireBoingCoroutine);
        body.localScale = Vector3.one;
        fireBoingCoroutine = StartCoroutine(FireBoingRoutine());
    }

    private IEnumerator FireBoingRoutine()
    {
        const float duration = 0.2f;
        const float amplitude = 0.15f;
        Vector3 baseScale = Vector3.one;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float wave = Mathf.Sin(t * Mathf.PI);
            float s = amplitude * wave;
            body.localScale = new Vector3(baseScale.x * (1f + s * 0.5f), baseScale.y * (1f - s), baseScale.z);
            yield return null;
        }

        body.localScale = baseScale;
        fireBoingCoroutine = null;
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
