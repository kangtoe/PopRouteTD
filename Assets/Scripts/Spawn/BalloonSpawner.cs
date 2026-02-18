using UnityEngine;
using UnityEngine.Pool;

public class BalloonSpawner : MonoBehaviour
{
    public static BalloonSpawner Instance { get; private set; }

    [Header("풍선 프리팹")]
    [SerializeField] private GameObject balloonPrefab;

    [Header("부모 Transform")]
    [SerializeField] private Transform enemyParent;

    [Header("풀 크기")]
    [SerializeField] private int initialPoolSize = 30;

    private ObjectPool<GameObject> balloonPool;

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
        if (balloonPrefab == null) return;

        balloonPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(balloonPrefab, enemyParent),
            actionOnGet: null,
            actionOnRelease: obj => obj.SetActive(false),
            actionOnDestroy: Destroy,
            defaultCapacity: initialPoolSize
        );
    }

    public GameObject SpawnBalloon(BalloonLayer layer, WaypointPath path,
        EnemyVariant variant = EnemyVariant.Normal)
    {
        var obj = balloonPool.Get();
        if (obj == null) return null;
        var balloon = obj.GetComponent<Balloon>();
        balloon.Initialize(layer, path, variant);
        GameManager.Instance.RegisterEnemy();
        return obj;
    }

    public void Return(GameObject obj)
    {
        balloonPool.Release(obj);
    }
}
