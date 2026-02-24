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

    [Header("Icon Rendering")]
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
    private SpriteRenderer[] visualRenderers;
    private Color[] originalColors;
    private Vector3 originalScale;
    private Coroutine boingCoroutine;

    public void Initialize()
    {
        var list = new System.Collections.Generic.List<SpriteRenderer>();
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
        {
            if (sr == rangeIndicator) continue;
            sr.sortingLayerName = GameConstants.SortTower;
            list.Add(sr);
        }
        visualRenderers = list.ToArray();
        originalColors = new Color[visualRenderers.Length];
        for (int i = 0; i < visualRenderers.Length; i++)
            originalColors[i] = visualRenderers[i].color;

        if (rangeIndicator != null)
            rangeIndicator.gameObject.SetActive(false);

        enemyLayerMask = 1 << LayerMask.NameToLayer(GameConstants.LayerEnemy);
        originalScale = transform.localScale;
        PlayBoing();
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

    private void PlayBoing()
    {
        if (boingCoroutine != null)
            StopCoroutine(boingCoroutine);
        transform.localScale = originalScale;
        boingCoroutine = StartCoroutine(BoingRoutine());
    }

    private IEnumerator BoingRoutine()
    {
        const float duration = 0.35f;
        const float amplitude = 0.15f;
        const float frequency = 3f;
        Vector3 baseScale = originalScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float decay = 1f - t;
            float wave = Mathf.Sin(t * frequency * Mathf.PI * 2f);
            float s = amplitude * decay * wave;
            transform.localScale = new Vector3(baseScale.x * (1f - s * 0.5f), baseScale.y * (1f + s), baseScale.z);
            yield return null;
        }

        transform.localScale = baseScale;
        boingCoroutine = null;
    }

    private IEnumerator FuseCountdown()
    {
        float elapsed = 0f;

        while (elapsed < fuseTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fuseTime);
            float interval = Mathf.Lerp(0.6f, 0.15f, t);
            float wave = Mathf.PingPong(elapsed / interval, 1f);
            float glow = Mathf.Lerp(0.15f, 0.5f, t) * wave;
            SetVisualBrightness(1f + glow);
            yield return null;
        }

        RestoreVisualColors();
        Explode();
    }

    private void SetVisualBrightness(float brightness)
    {
        for (int i = 0; i < visualRenderers.Length; i++)
        {
            if (visualRenderers[i] == null) continue;
            var c = originalColors[i];
            visualRenderers[i].color = new Color(c.r * brightness, c.g * brightness, c.b * brightness, c.a);
        }
    }

    private void RestoreVisualColors()
    {
        for (int i = 0; i < visualRenderers.Length; i++)
        {
            if (visualRenderers[i] == null) continue;
            visualRenderers[i].color = originalColors[i];
        }
    }

    private void Explode()
    {
        SoundManager.Instance.PlayExplode();
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
