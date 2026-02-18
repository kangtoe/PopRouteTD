using UnityEngine;

public class GoldItem : MonoBehaviour
{
    [SerializeField] private int goldAmount = 2;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        gameObject.layer = LayerMask.NameToLayer(GameConstants.LayerItem);
        if (spriteRenderer != null)
            spriteRenderer.sortingLayerName = GameConstants.SortItem;
    }

    public void Initialize(int gold, Vector3 position)
    {
        goldAmount = gold;
        transform.position = position;
        gameObject.SetActive(true);
    }

    public void Collect()
    {
        ResourceManager.Instance.AddGold(goldAmount);
        ItemPool.Instance.Return(gameObject);
    }
}
