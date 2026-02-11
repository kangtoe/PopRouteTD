using UnityEngine;

public class ExplosionEffect : MonoBehaviour
{
    private static Sprite circleSprite;

    private SpriteRenderer sr;
    private float targetScale;
    private float startAlpha;
    private float duration;
    private float elapsed;

    public static void Spawn(Vector3 position, float radius, float duration = 0.3f)
    {
        var obj = new GameObject("ExplosionEffect");
        obj.transform.position = position;

        var effect = obj.AddComponent<ExplosionEffect>();
        effect.sr = obj.AddComponent<SpriteRenderer>();
        effect.sr.sprite = GetCircleSprite();
        effect.sr.sortingLayerName = GameConstants.SortProjectile;
        effect.sr.color = new Color(1f, 0.5f, 0f, 0.7f);
        effect.startAlpha = 0.7f;
        effect.targetScale = radius * 2f;
        effect.duration = duration;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        float scale = Mathf.Lerp(0f, targetScale, t);
        transform.localScale = Vector3.one * scale;

        var color = sr.color;
        color.a = Mathf.Lerp(startAlpha, 0f, t);
        sr.color = color;
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null) return circleSprite;

        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                tex.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
            }

        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64);
        return circleSprite;
    }
}
