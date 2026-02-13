using System;
using UnityEngine;

[RequireComponent(typeof(WaypointFollower), typeof(StatusEffectHandler))]
public class Balloon : MonoBehaviour
{
    [Header("풍선 데이터")]
    [SerializeField] private int hp = 1;
    [SerializeField] private int energyReward = 1;
    [SerializeField, Range(0f, 1f)] private float statusEffectResistance;

    private int currentHp;
    private BalloonLayer currentLayer;
    private SpriteRenderer[] spriteRenderers;
    private WaypointFollower follower;
    private StatusEffectHandler statusEffectHandler;
    private WaypointPath path;
    private bool deactivated;

    public BalloonLayer CurrentLayer => currentLayer;
    public int CurrentHp => currentHp;
    public WaypointFollower Follower => follower;

    public static event Action<Balloon> OnBalloonDestroyed;
    public static event Action<Balloon> OnBalloonReachedBase;

    private void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        follower = GetComponent<WaypointFollower>();
        statusEffectHandler = GetComponent<StatusEffectHandler>();

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

    public void Initialize(BalloonLayer startLayer, WaypointPath waypointPath)
    {
        path = waypointPath;
        currentLayer = startLayer;
        currentHp = hp;

        deactivated = false;
        statusEffectHandler.ClearAll();
        SetColor(GameConstants.GetBalloonColor(currentLayer));
        follower.Initialize(path, GameConstants.GetBalloonSpeed(currentLayer));
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

    /// <summary>
    /// 관통 탄환용: layerCount만큼 레이어를 한 번에 벗긴다.
    /// 반환값은 실제 소비된 레이어 수.
    /// </summary>
    public int TakeLayerDamage(int layerCount)
    {
        if (deactivated) return 0;

        int consumed = Mathf.Min(layerCount, (int)currentLayer);

        for (int i = 0; i < consumed; i++)
        {
            ResourceManager.Instance.AddEnergy(energyReward);
            OnBalloonDestroyed?.Invoke(this);
        }

        BalloonLayer targetLayer = (BalloonLayer)((int)currentLayer - consumed);
        if (targetLayer >= BalloonLayer.Red)
        {
            currentLayer = targetLayer;
            currentHp = hp;
            SetColor(GameConstants.GetBalloonColor(currentLayer));
            follower.SetSpeed(GameConstants.GetBalloonSpeed(currentLayer));
        }
        else
        {
            Deactivate();
        }

        return consumed;
    }

    private void DestroyLayer()
    {
        ResourceManager.Instance.AddEnergy(energyReward);
        OnBalloonDestroyed?.Invoke(this);

        BalloonLayer lowerLayer = currentLayer - 1;
        if (lowerLayer >= BalloonLayer.Red)
        {
            // in-place 레이어 다운그레이드 (상태이상 유지)
            currentLayer = lowerLayer;
            currentHp = hp;
            SetColor(GameConstants.GetBalloonColor(currentLayer));
            follower.SetSpeed(GameConstants.GetBalloonSpeed(currentLayer));
        }
        else
        {
            Deactivate();
        }
    }

    private void OnReachBase()
    {
        ResourceManager.Instance.LoseLife((int)currentLayer);
        OnBalloonReachedBase?.Invoke(this);
        Deactivate();
    }

    public void ApplyStatusEffect(StatusEffectType type, float duration)
    {
        if (deactivated) return;

        float reducedDuration = duration * (1f - statusEffectResistance);
        if (reducedDuration <= 0f) return;

        statusEffectHandler.ApplyEffect(type, reducedDuration);
    }

    private void Deactivate()
    {
        if (deactivated) return;
        deactivated = true;
        statusEffectHandler.ClearAll();
        follower.OnReachedEnd -= OnReachBase;
        BalloonSpawner.Instance.Return(gameObject);
    }
}
