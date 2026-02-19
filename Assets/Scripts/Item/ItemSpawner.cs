using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    public static ItemSpawner Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SpawnItem(Vector3 position, int gold)
    {
        if (ItemPool.Instance == null) return;
        ItemPool.Instance.Spawn(position, gold);
    }
}
