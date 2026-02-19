using UnityEngine;

public static class TowerIconGenerator
{
    private const int TextureSize = 128;
    private const float CameraOrthoSize = 1.0f;
    private const float OffScreenPos = -100f;

    public static Sprite GenerateIcon(GameObject towerPrefab)
    {
        var instance = Object.Instantiate(towerPrefab,
            new Vector3(OffScreenPos, OffScreenPos, 0f), Quaternion.identity);

        PrepareInstance(instance);
        return CaptureIcon(instance);
    }

    public static Sprite GenerateUpgradeIcon(Tower tower, int targetMainLevel)
    {
        var instance = Object.Instantiate(tower.gameObject,
            new Vector3(OffScreenPos, OffScreenPos, 0f), Quaternion.identity);

        PrepareInstance(instance);

        var cloneTower = instance.GetComponent<Tower>();
        for (int level = 2; level <= targetMainLevel; level++)
        {
            var visual = cloneTower.GetMainVisual(level);
            if (visual != null) visual.SetActive(true);
        }

        return CaptureIcon(instance);
    }

    public static Sprite GenerateSubIcon(Tower tower, UpgradeTrack sub)
    {
        var instance = Object.Instantiate(tower.gameObject,
            new Vector3(OffScreenPos, OffScreenPos, 0f), Quaternion.identity);

        PrepareInstance(instance);

        var cloneTower = instance.GetComponent<Tower>();
        var visual = cloneTower.GetSubVisual(sub);
        if (visual != null) visual.SetActive(true);

        return CaptureIcon(instance);
    }

    private static void PrepareInstance(GameObject instance)
    {
        foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>())
            behaviour.enabled = false;
        foreach (var col in instance.GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        var tower = instance.GetComponent<Tower>();
        if (tower != null)
        {
            if (tower.Body != null)
                tower.Body.localRotation = Quaternion.identity;
            if (tower.RangeIndicator != null)
                tower.RangeIndicator.gameObject.SetActive(false);
        }
    }

    private static Sprite CaptureIcon(GameObject instance)
    {
        var rt = new RenderTexture(TextureSize, TextureSize, 24, RenderTextureFormat.ARGB32);
        rt.Create();

        var camObj = new GameObject("TowerIconCamera");
        camObj.transform.position = new Vector3(OffScreenPos, OffScreenPos, -10f);
        var cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = CameraOrthoSize;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.clear;
        cam.targetTexture = rt;
        cam.cullingMask = ~0;
        cam.enabled = false;

        cam.Render();

        var prevRT = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, TextureSize, TextureSize), 0, 0);
        tex.Apply();
        RenderTexture.active = prevRT;

        var sprite = Sprite.Create(
            tex,
            new Rect(0, 0, TextureSize, TextureSize),
            new Vector2(0.5f, 0.5f),
            TextureSize);

        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(camObj);
        Object.DestroyImmediate(instance);

        return sprite;
    }
}
