using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BalloonSpawner : MonoBehaviour
{
    public static BalloonSpawner Instance { get; private set; }

    [Header("레이어별 풍선 프리팹 (Red=0 ~ Purple=6)")]
    [SerializeField] private GameObject[] balloonPrefabsByLayer;

    [Header("부모 Transform")]
    [SerializeField] private Transform enemyParent;

    [Header("풀 크기")]
    [SerializeField] private int initialPoolSize = 30;

    private readonly Dictionary<int, ObjectPool<GameObject>> balloonPools = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Balloon.OnLayerDestroyed += HandleLayerDestroyed;
        InitializePools();
    }

    private void InitializePools()
    {
        if (balloonPrefabsByLayer == null || balloonPrefabsByLayer.Length == 0) return;

        for (int i = 0; i < balloonPrefabsByLayer.Length; i++)
        {
            int index = i;
            int capacity = initialPoolSize / balloonPrefabsByLayer.Length;
            balloonPools[i] = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(balloonPrefabsByLayer[index], enemyParent),
                actionOnGet: obj => obj.SetActive(true),
                actionOnRelease: obj => obj.SetActive(false),
                actionOnDestroy: Destroy,
                defaultCapacity: capacity
            );
        }
    }

    private void OnDestroy()
    {
        Balloon.OnLayerDestroyed -= HandleLayerDestroyed;
    }

    public GameObject SpawnBalloon(BalloonLayer layer, WaypointPath path)
    {
        var obj = GetFromPool(layer);
        if (obj == null) return null;
        var balloon = obj.GetComponent<Balloon>();
        balloon.Initialize(path);
        GameManager.Instance.RegisterEnemy();
        return obj;
    }

    public GameObject SpawnBalloonAtProgress(BalloonLayer layer, WaypointPath path, int waypointIndex, float fraction)
    {
        var obj = GetFromPool(layer);
        if (obj == null) return null;
        var balloon = obj.GetComponent<Balloon>();
        balloon.InitializeAtProgress(path, waypointIndex, fraction);
        GameManager.Instance.RegisterEnemy();
        return obj;
    }

    public void Return(GameObject obj, BalloonLayer balloonLayer)
    {
        int index = (int)balloonLayer - 1;
        if (balloonPools.TryGetValue(index, out var pool))
            pool.Release(obj);
    }

    private void HandleLayerDestroyed(BalloonLayer childLayer, Vector3 position, int waypointIndex, float fraction, WaypointPath path)
    {
        SpawnBalloonAtProgress(childLayer, path, waypointIndex, fraction);
    }

    private GameObject GetFromPool(BalloonLayer layer)
    {
        int index = (int)layer - 1;
        if (index < 0 || index >= balloonPrefabsByLayer.Length) return null;
        if (!balloonPools.TryGetValue(index, out var pool)) return null;

        return pool.Get();
    }
}
