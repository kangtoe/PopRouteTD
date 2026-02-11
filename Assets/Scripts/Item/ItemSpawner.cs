using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    public static ItemSpawner Instance { get; private set; }

    [Header("드롭 설정")]
    [SerializeField, Range(0f, 1f)] private float dropChance = 0.1f;
    [SerializeField] private int dropEnergy = 5;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        Balloon.OnBalloonDestroyed += OnBalloonDestroyed;
    }

    private void OnDisable()
    {
        Balloon.OnBalloonDestroyed -= OnBalloonDestroyed;
    }

    private void OnBalloonDestroyed(Balloon balloon)
    {
        if (balloon.CurrentLayer != BalloonLayer.Red) return;
        if (Random.value > dropChance) return;

        SpawnItem(balloon.transform.position, dropEnergy);
    }

    public void SpawnItem(Vector3 position, int energy)
    {
        if (ItemPool.Instance == null) return;
        ItemPool.Instance.Spawn(position, energy);
    }
}
