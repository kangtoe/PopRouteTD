using UnityEngine;
using UnityEngine.Pool;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("적 프리팹")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("부모 Transform")]
    [SerializeField] private Transform enemyParent;

    [Header("풀 크기")]
    [SerializeField] private int initialPoolSize = 30;

    private ObjectPool<GameObject> enemyPool;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitializePool();
    }

    private void InitializePool()
    {
        if (enemyPrefab == null) return;

        enemyPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(enemyPrefab, enemyParent),
            actionOnGet: null,
            actionOnRelease: obj => obj.SetActive(false),
            actionOnDestroy: Destroy,
            defaultCapacity: initialPoolSize
        );
    }

    public GameObject SpawnEnemy(EnemyLayer layer, WaypointPath path,
        EnemyVariant variant = EnemyVariant.Normal)
    {
        var obj = enemyPool.Get();
        if (obj == null) return null;
        var enemy = obj.GetComponent<Enemy>();
        enemy.Initialize(layer, path, variant);
        GameManager.Instance.RegisterEnemy();
        return obj;
    }

    public void Return(GameObject obj)
    {
        enemyPool.Release(obj);
    }
}
