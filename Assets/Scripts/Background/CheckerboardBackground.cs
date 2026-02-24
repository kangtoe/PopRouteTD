using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CheckerboardBackground : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Color colorA = new(0.28f, 0.43f, 0.28f);
    [SerializeField] private Color colorB = new(0.32f, 0.48f, 0.32f);
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector2 offset;
    [SerializeField] private int margin = 2;

    private void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null)
        {
            Debug.LogError("[CheckerboardBackground] 카메라를 찾을 수 없습니다.");
            return;
        }

        int halfH = Mathf.CeilToInt(targetCamera.orthographicSize / cellSize) + margin;
        int halfW = Mathf.CeilToInt(targetCamera.orthographicSize * targetCamera.aspect / cellSize) + margin;
        int w = halfW * 2;
        int h = halfH * 2;

        var tex = new Texture2D(w, h, TextureFormat.RGB24, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                pixels[y * w + x] = (x + y) % 2 == 0 ? colorA : colorB;

        tex.SetPixels(pixels);
        tex.Apply();

        float ppu = 1f / cellSize;
        var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu);

        var sr = GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = GameConstants.SortBackground;

        transform.position = new Vector3(
            targetCamera.transform.position.x + offset.x,
            targetCamera.transform.position.y + offset.y,
            0f);
    }
}
