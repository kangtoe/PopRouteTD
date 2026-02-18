using UnityEngine;
using UnityEngine.Pool;

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance { get; private set; }

    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform projectileParent;
    [SerializeField] private int initialPoolSize = 20;

    private ObjectPool<GameObject> pool;
    private float cachedProjectileSpeed;

    public float ProjectileSpeed => cachedProjectileSpeed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        var prefabProjectile = projectilePrefab.GetComponent<Projectile>();
        cachedProjectileSpeed = prefabProjectile != null ? prefabProjectile.Speed : 15f;

        pool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(projectilePrefab, projectileParent),
            actionOnGet: obj => obj.SetActive(true),
            actionOnRelease: obj => obj.SetActive(false),
            actionOnDestroy: Destroy,
            defaultCapacity: initialPoolSize
        );
    }

    public GameObject Get() => pool.Get();
    public void Return(GameObject obj) => pool.Release(obj);
}
