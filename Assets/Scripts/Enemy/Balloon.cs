using System;
using UnityEngine;

[RequireComponent(typeof(WaypointFollower))]
public class Balloon : MonoBehaviour
{
    [Header("풍선 데이터 (프리팹에서 설정)")]
    [SerializeField] private BalloonLayer layer;
    [SerializeField] private int hp = 1;
    [SerializeField] private int energyReward = 1;

    private int currentHp;
    private BalloonLayer currentLayer;
    private SpriteRenderer[] spriteRenderers;
    private WaypointFollower follower;
    private WaypointPath path;
    private bool deactivated;

    public BalloonLayer CurrentLayer => currentLayer;
    public int CurrentHp => currentHp;
    public BalloonLayer Layer => layer;
    public WaypointFollower Follower => follower;

    /// <summary>런타임 프리팹 생성 시 데이터 설정</summary>
    public void SetupData(BalloonLayer balloonLayer, int balloonHp = 1, int reward = 1)
    {
        layer = balloonLayer;
        hp = balloonHp;
        energyReward = reward;
    }

    public static event Action<Balloon> OnBalloonDestroyed;
    public static event Action<Balloon> OnBalloonReachedBase;
    public static event Action<BalloonLayer, Vector3, int, float, WaypointPath> OnLayerDestroyed;

    private void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        follower = GetComponent<WaypointFollower>();

        gameObject.layer = LayerMask.NameToLayer(GameConstants.LayerEnemy);
        SetSortingLayer(GameConstants.SortEnemy);
    }

    private void SetSortingLayer(string layerName)
    {
        foreach (var sr in spriteRenderers)
            sr.sortingLayerName = layerName;
    }

    private void SetColor(Color color)
    {
        foreach (var sr in spriteRenderers)
            sr.color = color;
    }

    public void Initialize(WaypointPath waypointPath)
    {
        path = waypointPath;
        currentLayer = layer;
        currentHp = hp;

        deactivated = false;
        SetColor(GameConstants.GetBalloonColor(currentLayer));
        follower.Initialize(path, GameConstants.GetBalloonSpeed(currentLayer));
        follower.OnReachedEnd += OnReachBase;

        gameObject.SetActive(true);
    }

    public void InitializeAtProgress(WaypointPath waypointPath, int waypointIndex, float fraction)
    {
        path = waypointPath;
        currentLayer = layer;
        currentHp = hp;

        deactivated = false;
        SetColor(GameConstants.GetBalloonColor(currentLayer));
        follower.InitializeAtProgress(path, GameConstants.GetBalloonSpeed(currentLayer), waypointIndex, fraction);
        follower.OnReachedEnd += OnReachBase;

        gameObject.SetActive(true);
    }

    public void TakeDamage(int damage)
    {
        if (deactivated) return;
        currentHp -= damage;
        if (currentHp <= 0)
        {
            DestroyLayer();
        }
    }

    private void DestroyLayer()
    {
        ResourceManager.Instance.AddEnergy(energyReward);

        BalloonLayer lowerLayer = currentLayer - 1;
        if (lowerLayer >= BalloonLayer.Red)
        {
            int wpIndex = follower.CurrentWaypointIndex;
            float fraction = follower.FractionToNextWaypoint;
            OnLayerDestroyed?.Invoke(lowerLayer, transform.position, wpIndex, fraction, path);
        }

        OnBalloonDestroyed?.Invoke(this);
        Deactivate();
    }

    private void OnReachBase()
    {
        ResourceManager.Instance.LoseLife((int)currentLayer);
        OnBalloonReachedBase?.Invoke(this);
        Deactivate();
    }

    private void Deactivate()
    {
        if (deactivated) return;
        deactivated = true;
        follower.OnReachedEnd -= OnReachBase;
        BalloonSpawner.Instance.Return(gameObject, layer);
    }
}
