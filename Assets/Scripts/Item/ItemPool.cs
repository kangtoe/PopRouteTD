using UnityEngine;
using UnityEngine.Pool;

public class ItemPool : MonoBehaviour
{
    public static ItemPool Instance { get; private set; }

    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Transform itemParent;
    [SerializeField] private int initialPoolSize = 20;

    private ObjectPool<GameObject> pool;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        pool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(itemPrefab, itemParent),
            actionOnGet: obj => obj.SetActive(true),
            actionOnRelease: obj => obj.SetActive(false),
            actionOnDestroy: Destroy,
            defaultCapacity: initialPoolSize
        );
    }

    public GameObject Spawn(Vector3 position, int goldAmount)
    {
        var obj = pool.Get();
        var item = obj.GetComponent<GoldItem>();
        item.Initialize(goldAmount, position);
        return obj;
    }

    public void Return(GameObject obj) => pool.Release(obj);
}
