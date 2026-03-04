using System.Collections;
using UnityEngine;

public class Tower : MonoBehaviour
{
    private const float RotationSmoothTime = 0.12f;

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
    [SerializeField] private AudioClip hitSoundOverride;


    private TowerStats currentStats = new();
    private float attackTimer;
    private int enemyLayerMask;
    private bool initialized;
    private Enemy currentTarget;
    private float projectileSpeed;
    private float retargetTimer;
    private Vector3 lastAimPos;
    private float angularVelocity;

    private int mainLevel = 1;
    private bool hasSubA;
    private bool hasSubB;
    private int totalUpgradeCost;
    private Coroutine boingCoroutine;
    private Coroutine fireBoingCoroutine;
    private Vector3 originalScale;
    public Vector3 OriginalScale => originalScale;

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
        SoundManager.Instance.PlaySell();
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
        SoundManager.Instance.PlayUpgrade();
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
        SoundManager.Instance.PlayUpgrade();
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
            Vector3 scale = originalScale.x > 0f ? originalScale : rangeIndicator.transform.parent.lossyScale;
            rangeIndicator.transform.localScale = new Vector3(
                diameter / scale.x,
                diameter / scale.y,
                1f);
        }
    }

    private void Update()
    {
        if (!initialized) return;

        // 현재 타겟이 무효하면 즉시 해제 및 재평가
        if (currentTarget != null &&
            (!currentTarget.gameObject.activeInHierarchy ||
             Vector3.Distance(transform.position, currentTarget.transform.position) > currentStats.attackRange))
        {
            currentTarget = null;
            retargetTimer = 0f;
        }

        // 일정 간격으로 타겟 재평가
        retargetTimer -= Time.deltaTime;
        if (retargetTimer <= 0f)
        {
            currentTarget = TargetSelector.SelectTarget(transform.position, currentStats.attackRange, Priority, enemyLayerMask);
            retargetTimer = GameConstants.RetargetInterval;
        }

        // 타겟을 향해 예측 위치로 회전 및 공격
        if (currentTarget != null)
        {
            lastAimPos = PredictTargetPosition(currentTarget);
            LookAt(lastAimPos);

            if (attackTimer <= 0f && IsAimedAt(lastAimPos))
            {
                Attack();
                attackTimer = currentStats.attackInterval;
            }
        }
        else if (lastAimPos != Vector3.zero)
        {
            LookAt(lastAimPos);
        }

        attackTimer = Mathf.Max(attackTimer - Time.deltaTime, 0f);
    }

    private void LookAt(Vector3 targetPos)
    {
        Vector2 dir = (targetPos - transform.position).normalized;
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        float currentAngle = body.eulerAngles.z;
        float newAngle = Mathf.SmoothDampAngle(currentAngle, targetAngle, ref angularVelocity, RotationSmoothTime);
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
        if (projectileSpeed <= 0f)
            return target.transform.position;

        // 초기 추정: 직선 거리 기반 도달 시간
        float dist = Vector3.Distance(transform.position, target.transform.position);
        float t = dist / projectileSpeed;

        // 2회 반복으로 경로 기반 예측 수렴
        for (int i = 0; i < 2; i++)
        {
            Vector3 predicted = target.Follower.PredictPosition(t);
            dist = Vector3.Distance(transform.position, predicted);
            t = dist / projectileSpeed;
        }

        return target.Follower.PredictPosition(t);
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
            statusEffectType, effectDuration, currentStats.areaTargets, hitSoundOverride);
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
