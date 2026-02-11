using UnityEngine;

public class EnergyItem : MonoBehaviour
{
    [SerializeField] private int energyAmount = 2;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        gameObject.layer = LayerMask.NameToLayer(GameConstants.LayerItem);
        if (spriteRenderer != null)
            spriteRenderer.sortingLayerName = GameConstants.SortItem;
    }

    public void Initialize(int energy, Vector3 position)
    {
        energyAmount = energy;
        transform.position = position;
        gameObject.SetActive(true);
    }

    public void Collect()
    {
        ResourceManager.Instance.AddEnergy(energyAmount);
        ItemPool.Instance.Return(gameObject);
    }
}
