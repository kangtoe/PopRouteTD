using UnityEditor;
using UnityEngine;

public class PrefabCreator
{
    private static Sprite circleSprite;
    private static Sprite squareSprite;

    [MenuItem("PopRouteTD/프리팹 에셋 생성")]
    public static void CreateAllPrefabs()
    {
        EnsureFolder("Assets/Prefabs/Enemies");
        EnsureFolder("Assets/Prefabs/Towers");
        EnsureFolder("Assets/Prefabs/Items");

        circleSprite = CreateCircleSprite();
        squareSprite = CreateSquareSprite();

        // 베이스 프리팹
        CreateBaseBalloonPrefab();
        CreateBaseTowerPrefab();
        CreateProjectilePrefab();
        CreateEnergyItemPrefab();

        // 베리언트
        CreateBalloonVariants();
        CreateTowerVariants();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PopRouteTD] 프리팹 에셋 생성 완료!");
    }

    // ── 베이스 풍선 ──

    private static void CreateBaseBalloonPrefab()
    {
        var path = "Assets/Prefabs/Enemies/Balloon.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        var obj = new GameObject("Balloon");
        obj.layer = LayerMask.NameToLayer(GameConstants.LayerEnemy);

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = circleSprite;
        sr.color = Color.white;
        sr.sortingLayerName = GameConstants.SortEnemy;
        obj.transform.localScale = Vector3.one * 0.5f;

        var col = obj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        obj.AddComponent<WaypointFollower>();
        obj.AddComponent<Balloon>();

        PrefabUtility.SaveAsPrefabAsset(obj, path);
        Object.DestroyImmediate(obj);
    }

    private static void CreateBalloonVariants()
    {
        var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Balloon.prefab");
        if (basePrefab == null) return;

        for (int i = 0; i < 7; i++)
        {
            var layer = (BalloonLayer)(i + 1);
            var path = $"Assets/Prefabs/Enemies/Balloon_{layer}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) continue;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            instance.name = $"Balloon_{layer}";

            instance.GetComponent<SpriteRenderer>().color = GameConstants.GetBalloonColor(layer);
            instance.GetComponent<Balloon>().SetupData(layer);

            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
        }
    }

    // ── 베이스 타워 ──

    private static void CreateBaseTowerPrefab()
    {
        var path = "Assets/Prefabs/Towers/Tower.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        var obj = new GameObject("Tower");
        obj.layer = LayerMask.NameToLayer(GameConstants.LayerTower);

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.color = Color.white;
        sr.sortingLayerName = GameConstants.SortTower;
        obj.transform.localScale = Vector3.one * 0.7f;

        var col = obj.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;

        obj.AddComponent<Tower>();

        PrefabUtility.SaveAsPrefabAsset(obj, path);
        Object.DestroyImmediate(obj);
    }

    private static void CreateTowerVariants()
    {
        var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Towers/Tower.prefab");
        if (basePrefab == null) return;

        // 기본 사수
        CreateTowerVariant(basePrefab, "Tower_BasicShooter", "기본 사수", 15, 1, 1f, 3f, 10,
            true, 0, 0, new Color(0.4f, 0.4f, 0.8f));

        // 에너지 생성기 (아이템당 에너지: 2, 웨이브당 아이템 수: 5)
        CreateTowerVariant(basePrefab, "Tower_EnergyGenerator", "에너지 생성기", 20, 0, 0, 0, 12,
            false, 2, 5f, new Color(0.8f, 0.8f, 0.2f));
    }

    private static void CreateTowerVariant(GameObject basePrefab, string fileName, string towerName,
        int cost, float attackDamage, float attackInterval, float attackRange, int sellRefund,
        bool isAttacker, float energyPerTick, float energyTickInterval, Color color)
    {
        var path = $"Assets/Prefabs/Towers/{fileName}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
        instance.name = fileName;

        instance.GetComponent<SpriteRenderer>().color = color;
        instance.GetComponent<Tower>().SetupData(towerName, cost, attackDamage, attackInterval,
            attackRange, sellRefund, isAttacker, energyPerTick, energyTickInterval);

        if (!isAttacker) instance.AddComponent<TowerEnergyGenerator>();

        PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);
    }

    // ── 투사체 ──

    private static void CreateProjectilePrefab()
    {
        var path = "Assets/Prefabs/Towers/Projectile.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        var obj = new GameObject("Projectile");

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = circleSprite;
        sr.color = Color.white;
        sr.sortingLayerName = GameConstants.SortProjectile;
        obj.transform.localScale = Vector3.one * 0.15f;

        var rb = obj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = obj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        obj.AddComponent<Projectile>();

        PrefabUtility.SaveAsPrefabAsset(obj, path);
        Object.DestroyImmediate(obj);
    }

    // ── 아이템 ──

    private static void CreateEnergyItemPrefab()
    {
        var path = "Assets/Prefabs/Items/EnergyItem.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        var obj = new GameObject("EnergyItem");
        obj.layer = LayerMask.NameToLayer(GameConstants.LayerItem);

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = circleSprite;
        sr.color = new Color(0.2f, 0.9f, 0.3f);
        sr.sortingLayerName = GameConstants.SortItem;
        obj.transform.localScale = Vector3.one * 0.3f;

        var col = obj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        obj.AddComponent<EnergyItem>();

        PrefabUtility.SaveAsPrefabAsset(obj, path);
        Object.DestroyImmediate(obj);
    }

    // ── 텍스처 생성 ──

    private static Sprite CreateCircleSprite()
    {
        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Circle" };
        float center = size / 2f;
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                tex.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
            }

        tex.Apply();

        var texPath = "Assets/Prefabs/Circle.png";
        System.IO.File.WriteAllBytes(texPath, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(texPath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 64;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
    }

    private static Sprite CreateSquareSprite()
    {
        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Square" };
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, Color.white);

        tex.Apply();

        var texPath = "Assets/Prefabs/Square.png";
        System.IO.File.WriteAllBytes(texPath, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(texPath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 64;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
    }

    private static void EnsureFolder(string path)
    {
        var parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
