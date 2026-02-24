using System.Collections;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    [Header("Info")]
    [SerializeField] private string bombName = "Bomb";
    [SerializeField] private int cost = 50;

    [Header("Stats")]
    [SerializeField] private float fuseTime = 3f;
    [SerializeField] private float explosionRadius = 1.5f;
    [SerializeField] private int damage = 3;
    [SerializeField] private float renderOffsetY = 0.3f;
    [SerializeField] private float renderScale = 1f;

    [Header("References")]
    [SerializeField] private SpriteRenderer rangeIndicator;

    public string BombName => bombName;
    public int Cost => cost;
    public float ExplosionRadius => explosionRadius;
    public float RenderOffsetY => renderOffsetY;
    public float RenderScale => renderScale;
    public SpriteRenderer RangeIndicator => rangeIndicator;

    private int enemyLayerMask;

    public void Initialize()
    {
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
        {
            if (sr == rangeIndicator) continue;
            sr.sortingLayerName = GameConstants.SortTower;
        }

        if (rangeIndicator != null)
            rangeIndicator.gameObject.SetActive(false);

        enemyLayerMask = 1 << LayerMask.NameToLayer(GameConstants.LayerEnemy);
        StartCoroutine(FuseCountdown());
    }

    public void ShowRange(bool show)
    {
        if (rangeIndicator == null) return;
        rangeIndicator.gameObject.SetActive(show);
        float diameter = explosionRadius * 2f;
        Vector3 parentScale = rangeIndicator.transform.parent != null
            ? rangeIndicator.transform.parent.lossyScale
            : Vector3.one;
        rangeIndicator.transform.localScale = new Vector3(
            diameter / parentScale.x,
            diameter / parentScale.y,
            1f);
    }

    private IEnumerator FuseCountdown()
    {
        yield return new WaitForSeconds(fuseTime);
        Explode();
    }

    private void Explode()
    {
        ExplosionEffect.Spawn(transform.position, explosionRadius);

        var hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, enemyLayerMask);
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
                enemy.TakeLayerDamage(damage);
        }

        Destroy(gameObject);
    }
}
