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
        CreateTowerVariants();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PopRouteTD] 프리팹 에셋 생성 완료!");
    }

    [MenuItem("PopRouteTD/타워 프리팹 생성/폭탄 사수")]
    public static void CreateBombShooterPrefab()
    {
        EnsureFolder("Assets/Prefabs/Towers");
        squareSprite = CreateSquareSprite();

        var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Towers/Tower.prefab");
        if (basePrefab == null)
        {
            CreateBaseTowerPrefab();
            basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Towers/Tower.prefab");
        }

        CreateTowerVariant(basePrefab, "Tower_BombShooter", "BombTower", new Color(0.8f, 0.3f, 0.3f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PopRouteTD] 폭탄 사수 프리팹 생성 완료!");
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

        CreateTowerVariant(basePrefab, "Tower_BasicShooter", "BasicTower", new Color(0.4f, 0.4f, 0.8f));
        CreateTowerVariant(basePrefab, "Tower_BombShooter", "BombTower", new Color(0.8f, 0.3f, 0.3f));
    }

    private static void CreateTowerVariant(GameObject basePrefab, string fileName,
        string upgradeDataName, Color color)
    {
        var path = $"Assets/Prefabs/Towers/{fileName}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        var upgradeData = AssetDatabase.LoadAssetAtPath<TowerUpgradeData>(
            $"Assets/Data/TowerUpgrades/{upgradeDataName}.asset");

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
        instance.name = fileName;

        instance.GetComponent<SpriteRenderer>().color = color;

        var so = new SerializedObject(instance.GetComponent<Tower>());
        so.FindProperty("upgradeData").objectReferenceValue = upgradeData;
        so.ApplyModifiedPropertiesWithoutUndo();

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

    // ── 감속 이펙트 ──

    [MenuItem("PopRouteTD/이펙트 프리팹 생성/감속 파티클")]
    public static void CreateSlowEffectPrefab()
    {
        EnsureFolder("Assets/Prefabs/Effects");

        var path = "Assets/Prefabs/Effects/SlowEffect.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.Log("[PopRouteTD] SlowEffect 프리팹이 이미 존재합니다. 삭제 후 다시 시도하세요.");
            return;
        }

        var obj = new GameObject("SlowEffect");
        var ps = obj.AddComponent<ParticleSystem>();

        // 생성 직후 자동 재생 방지
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // ── Main Module ──
        var main = ps.main;
        main.duration = 1f;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.startColor = new Color(0.6f, 0.9f, 1f, 0.8f); // 하늘색
        main.maxParticles = 20;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        // ── Emission ──
        var emission = ps.emission;
        emission.rateOverTime = 15f;

        // ── Shape: 풍선 크기(0.5) 안쪽 원형 ──
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.15f;

        // ── Velocity over Lifetime: 아래로 살짝 흘러내림 ──
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.3f, -0.05f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        // ── Color over Lifetime: 하늘색 → 페이드아웃 ──
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.6f, 0.9f, 1f), 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0.4f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(gradient);

        // ── Size over Lifetime: 점점 작아짐 ──
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

        // ── Renderer: 2D 스프라이트 호환 ──
        var renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingLayerName = GameConstants.SortEnemy;
        renderer.sortingOrder = -1; // 풍선 뒤에 그려짐

        // Sprites-Default 머티리얼 사용
        var spriteMat = new Material(Shader.Find("Sprites/Default"));
        spriteMat.name = "SlowParticleMat";
        AssetDatabase.CreateAsset(spriteMat, "Assets/Prefabs/Effects/SlowParticleMat.mat");
        renderer.material = spriteMat;

        // 불필요한 모듈 비활성화
        var noise = ps.noise;
        noise.enabled = false;
        var trails = ps.trails;
        trails.enabled = false;

        PrefabUtility.SaveAsPrefabAsset(obj, path);
        Object.DestroyImmediate(obj);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PopRouteTD] 감속 이펙트 프리팹 생성 완료: " + path);
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
