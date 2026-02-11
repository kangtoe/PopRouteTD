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

    [SerializeField] private Transform firePoint;
    [SerializeField] private SpriteRenderer rangeIndicator;

    private float attackTimer;
    private int enemyLayerMask;
    private bool initialized;
    private Balloon currentTarget;

    public string TowerName => towerName;
    public int Cost => cost;
    public float AttackDamage => attackDamage;
    public float AttackInterval => attackInterval;
    public float AttackRange => attackRange;
    public int SellRefund => sellRefund;
    public bool IsAttacker => isAttacker;
    public float SplashRadius => splashRadius;
    public SpriteRenderer RangeIndicator => rangeIndicator;
    public TargetPriority Priority { get; private set; } = TargetPriority.First;

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
        ResourceManager.Instance.AddEnergy(sellRefund);
        Destroy(gameObject);
    }

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
        projectile.Initialize((int)attackDamage, splashRadius);
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
