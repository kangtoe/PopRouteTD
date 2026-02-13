using UnityEditor;
using UnityEngine;

/// <summary>
/// 타워 프리팹에 업그레이드 부속물(Main_2~4, Sub_a/b) 자식 오브젝트를 자동 생성.
/// Tower_Base에 빈 컨테이너 + 필드 연결, 각 베리언트에 실제 스프라이트 추가.
/// Tools > Generate Tower Visuals 메뉴에서 실행.
/// 재실행 시 기존 부속물을 삭제 후 재생성 (idempotent).
/// </summary>
public static class TowerVisualGenerator
{
    private static Sprite sprCircle, sprSquare, sprTriangle, sprCapsule;

    private const string PrefabFolder = "Assets/Prefabs/Towers";
    private const string SpriteBase =
        "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2";

    private static readonly string[] PartNames = { "Main_2", "Main_3", "Main_4", "Sub_a", "Sub_b" };

    [MenuItem("Tools/Generate Tower Visuals")]
    public static void Generate()
    {
        LoadSprites();

        GenerateBase();
        GenerateBasic();
        GenerateBomb();
        GeneratePierce();
        GenerateSlow();
        GenerateRapid();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[TowerVisualGenerator] Generated visual parts for 5 towers.");
    }

    private static void LoadSprites()
    {
        sprCircle   = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteBase}/Circle.png");
        sprSquare   = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteBase}/Square.png");
        sprTriangle = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteBase}/Triangle.png");
        sprCapsule  = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteBase}/Capsule.png");

        if (sprCircle == null || sprSquare == null || sprTriangle == null || sprCapsule == null)
            Debug.LogError("[TowerVisualGenerator] Failed to load one or more default sprites!");
    }

    // ───────── Base Prefab ─────────
    // 빈 컨테이너 생성 + Tower 컴포넌트에 시리얼 필드 연결
    private static void GenerateBase()
    {
        var path = $"{PrefabFolder}/Tower_Base.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);

        // 기존 컨테이너 제거 (idempotent)
        CleanContainers(root);

        // 빈 컨테이너 생성 (비활성)
        var m2 = CreateContainer(root, "Main_2");
        var m3 = CreateContainer(root, "Main_3");
        var m4 = CreateContainer(root, "Main_4");
        var sa = CreateContainer(root, "Sub_a");
        var sb = CreateContainer(root, "Sub_b");

        // Tower 컴포넌트에 참조 연결
        var tower = root.GetComponent<Tower>();
        if (tower != null)
        {
            var so = new SerializedObject(tower);
            so.FindProperty("mainVisual2").objectReferenceValue = m2;
            so.FindProperty("mainVisual3").objectReferenceValue = m3;
            so.FindProperty("mainVisual4").objectReferenceValue = m4;
            so.FindProperty("subVisualA").objectReferenceValue = sa;
            so.FindProperty("subVisualB").objectReferenceValue = sb;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    // ───────── Basic Shooter ─────────
    // Body: Circle + Square barrel
    // Sub A (Elite): stat bonus   Sub B (Frost): slow
    private static void GenerateBasic()
    {
        var root = BeginVariant("Tower_Basic");
        var body = GetBodyColor(root);

        var m2 = FindPart(root, "Main_2");
        S(m2, "BarrelExt", sprSquare, P(0, 0.85f), Sc(0.18f, 0.25f), body);
        S(m2, "AmmoL",     sprSquare, P(-0.3f, 0.15f), Sc(0.06f, 0.15f), body);
        S(m2, "AmmoR",     sprSquare, P(0.3f, 0.15f),  Sc(0.06f, 0.15f), body);

        var m3 = FindPart(root, "Main_3");
        S(m3, "ReinforcedBarrel", sprCapsule, P(0, 0.95f), Sc(0.12f, 0.2f), body);
        S(m3, "ArmorL", sprSquare, P(-0.45f, 0), Sc(0.1f, 0.25f), body);
        S(m3, "ArmorR", sprSquare, P(0.45f, 0),  Sc(0.1f, 0.25f), body);

        var m4 = FindPart(root, "Main_4");
        S(m4, "Badge",     sprTriangle, P(0, 0.3f),    Sc(0.15f, 0.15f), body);
        S(m4, "ShoulderL", sprSquare,   P(-0.5f, 0.2f), Sc(0.12f, 0.2f), body);
        S(m4, "ShoulderR", sprSquare,   P(0.5f, 0.2f),  Sc(0.12f, 0.2f), body);

        var sa = FindPart(root, "Sub_a");
        S(sa, "ArmorBoostL", sprSquare, P(-0.4f, -0.1f), Sc(0.12f, 0.3f), body);
        S(sa, "ArmorBoostR", sprSquare, P(0.4f, -0.1f),  Sc(0.12f, 0.3f), body);

        var sb = FindPart(root, "Sub_b");
        var frost = new Color(0.5f, 0.85f, 1f);
        S(sb, "Cooler",     sprCircle,   P(0, 0.7f),     Sc(0.15f, 0.15f), frost);
        S(sb, "IceCrystal", sprTriangle, P(0.35f, 0.4f), Sc(0.12f, 0.12f), frost);

        EndVariant(root, "Tower_Basic");
    }

    // ───────── Bomb Shooter ─────────
    // Body: Square body + Capsule barrel
    // Sub A (Wide Area): splash radius   Sub B (Incendiary): burn
    private static void GenerateBomb()
    {
        var root = BeginVariant("Tower_Bomb");
        var body = GetBodyColor(root);

        var m2 = FindPart(root, "Main_2");
        S(m2, "ArmorL",      sprSquare,  P(-0.4f, 0),   Sc(0.1f, 0.3f), body);
        S(m2, "ArmorR",      sprSquare,  P(0.4f, 0),    Sc(0.1f, 0.3f), body);
        S(m2, "BarrelBoost", sprCapsule, P(0, 0.85f),   Sc(0.2f, 0.2f), body);

        var m3 = FindPart(root, "Main_3");
        S(m3, "WideBarrel", sprCapsule, P(0, 0.95f),   Sc(0.25f, 0.2f), body);
        S(m3, "WingL",      sprSquare,  P(-0.5f, 0.2f), Sc(0.15f, 0.1f), body);
        S(m3, "WingR",      sprSquare,  P(0.5f, 0.2f),  Sc(0.15f, 0.1f), body);

        var m4 = FindPart(root, "Main_4");
        S(m4, "HeavyBarrel", sprCapsule,  P(0, 1.05f),   Sc(0.3f, 0.22f), body);
        S(m4, "FinL",        sprTriangle, P(-0.55f, 0.3f), Sc(0.12f, 0.15f), body);
        S(m4, "FinR",        sprTriangle, P(0.55f, 0.3f),  Sc(0.12f, 0.15f), body);

        var sa = FindPart(root, "Sub_a");
        S(sa, "WingBoostL", sprSquare, P(-0.55f, 0.1f), Sc(0.18f, 0.12f), body);
        S(sa, "WingBoostR", sprSquare, P(0.55f, 0.1f),  Sc(0.18f, 0.12f), body);

        var sb = FindPart(root, "Sub_b");
        var flame = new Color(1f, 0.5f, 0.2f);
        S(sb, "FlameEmblem",  sprTriangle, P(0, 0.3f),  Sc(0.15f, 0.15f), flame);
        S(sb, "FlameBarrel",  sprCapsule,  P(0, 0.75f), Sc(0.14f, 0.18f), flame);

        EndVariant(root, "Tower_Bomb");
    }

    // ───────── Pierce Shooter ─────────
    // Body: Circle + Capsule long barrel
    // Sub A (Rapid Fire): attack speed   Sub B (Impact): stun
    private static void GeneratePierce()
    {
        var root = BeginVariant("Tower_Pierce");
        var body = GetBodyColor(root);

        var m2 = FindPart(root, "Main_2");
        S(m2, "BarrelExt",    sprCapsule, P(0, 0.9f),    Sc(0.12f, 0.25f), body);
        S(m2, "StabilizerL",  sprSquare,  P(-0.35f, 0.15f), Sc(0.08f, 0.2f), body);
        S(m2, "StabilizerR",  sprSquare,  P(0.35f, 0.15f),  Sc(0.08f, 0.2f), body);

        var m3 = FindPart(root, "Main_3");
        S(m3, "Scope", sprSquare,   P(0.15f, 0.7f),  Sc(0.08f, 0.15f), body);
        S(m3, "FinL",  sprTriangle, P(-0.45f, 0.3f), Sc(0.1f, 0.12f), body);
        S(m3, "FinR",  sprTriangle, P(0.45f, 0.3f),  Sc(0.1f, 0.12f), body);

        var m4 = FindPart(root, "Main_4");
        S(m4, "HeavyBarrel", sprCapsule, P(0, 1.05f),   Sc(0.15f, 0.25f), body);
        S(m4, "HeavyArmorL", sprSquare,  P(-0.5f, 0),   Sc(0.12f, 0.25f), body);
        S(m4, "HeavyArmorR", sprSquare,  P(0.5f, 0),    Sc(0.12f, 0.25f), body);

        var sa = FindPart(root, "Sub_a");
        S(sa, "AccelL", sprCircle, P(-0.4f, 0.3f), Sc(0.12f, 0.12f), body);
        S(sa, "AccelR", sprCircle, P(0.4f, 0.3f),  Sc(0.12f, 0.12f), body);

        var sb = FindPart(root, "Sub_b");
        var impact = new Color(1f, 0.9f, 0.3f);
        S(sb, "ImpactPlate",  sprTriangle, P(0, 0.3f),     Sc(0.15f, 0.15f), impact);
        S(sb, "Reinforcement", sprSquare,  P(0, -0.25f),   Sc(0.2f, 0.08f),  impact);

        EndVariant(root, "Tower_Pierce");
    }

    // ───────── Slow Shooter ─────────
    // Body: Circle + Triangle crystal top
    // Sub A (Frostbite): adds burn   Sub B (Freeze): enhanced slow
    private static void GenerateSlow()
    {
        var root = BeginVariant("Tower_Slower");
        var body = GetBodyColor(root);

        var m2 = FindPart(root, "Main_2");
        S(m2, "CrystalL",  sprTriangle, P(-0.35f, 0.2f), Sc(0.1f, 0.12f), body);
        S(m2, "CrystalR",  sprTriangle, P(0.35f, 0.2f),  Sc(0.1f, 0.12f), body);
        S(m2, "Emitter",   sprCircle,   P(0, 0.8f),       Sc(0.12f, 0.12f), body);

        var m3 = FindPart(root, "Main_3");
        S(m3, "CrystalBarrel", sprCapsule, P(0, 0.9f),    Sc(0.12f, 0.2f), body);
        S(m3, "CoolPlateL",    sprSquare,  P(-0.45f, 0),  Sc(0.08f, 0.2f), body);
        S(m3, "CoolPlateR",    sprSquare,  P(0.45f, 0),   Sc(0.08f, 0.2f), body);

        var m4 = FindPart(root, "Main_4");
        S(m4, "LargeCrystalL", sprTriangle, P(-0.5f, 0.3f), Sc(0.15f, 0.18f), body);
        S(m4, "LargeCrystalR", sprTriangle, P(0.5f, 0.3f),  Sc(0.15f, 0.18f), body);
        S(m4, "CoreBoost",     sprCircle,   P(0, 0),         Sc(0.15f, 0.15f), body);

        var sa = FindPart(root, "Sub_a");
        var flame = new Color(1f, 0.6f, 0.3f);
        S(sa, "FlameCrystal", sprTriangle, P(0, 0.3f),     Sc(0.12f, 0.12f), flame);
        S(sa, "HeatPlate",    sprSquare,   P(0, -0.25f),   Sc(0.18f, 0.08f), flame);

        var sb = FindPart(root, "Sub_b");
        var ice = new Color(0.3f, 0.6f, 1f);
        S(sb, "CoolerL", sprCircle, P(-0.4f, -0.1f), Sc(0.12f, 0.12f), ice);
        S(sb, "CoolerR", sprCircle, P(0.4f, -0.1f),  Sc(0.12f, 0.12f), ice);

        EndVariant(root, "Tower_Slower");
    }

    // ───────── Rapid Shooter ─────────
    // Body: Circle (small) + Square barrel
    // Sub A (Enhance): ATK bonus   Sub B (Spread): splash conversion
    private static void GenerateRapid()
    {
        var root = BeginVariant("Tower_Rapid");
        var body = GetBodyColor(root);

        var m2 = FindPart(root, "Main_2");
        S(m2, "BarrelExt", sprSquare,  P(0, 0.85f),     Sc(0.15f, 0.2f),  body);
        S(m2, "DrumL",     sprCircle,  P(-0.3f, 0.15f), Sc(0.1f, 0.1f),   body);
        S(m2, "DrumR",     sprCircle,  P(0.3f, 0.15f),  Sc(0.1f, 0.1f),   body);

        var m3 = FindPart(root, "Main_3");
        S(m3, "DualBarrel", sprCapsule, P(0, 0.95f),    Sc(0.18f, 0.2f), body);
        S(m3, "FeederL",    sprSquare,  P(-0.4f, 0.3f), Sc(0.08f, 0.15f), body);
        S(m3, "FeederR",    sprSquare,  P(0.4f, 0.3f),  Sc(0.08f, 0.15f), body);

        var m4 = FindPart(root, "Main_4");
        S(m4, "BarrelBoostL", sprSquare,   P(-0.15f, 0.9f), Sc(0.08f, 0.2f),  body);
        S(m4, "BarrelBoostR", sprSquare,   P(0.15f, 0.9f),  Sc(0.08f, 0.2f),  body);
        S(m4, "Badge",        sprTriangle, P(0, 0.3f),      Sc(0.12f, 0.12f), body);

        var sa = FindPart(root, "Sub_a");
        S(sa, "PowerL",      sprSquare,  P(-0.4f, 0),   Sc(0.1f, 0.2f),  body);
        S(sa, "PowerR",      sprSquare,  P(0.4f, 0),    Sc(0.1f, 0.2f),  body);
        S(sa, "HeavyBarrel", sprCapsule, P(0, 0.85f),   Sc(0.12f, 0.18f), body);

        var sb = FindPart(root, "Sub_b");
        var spread = new Color(0.6f, 1f, 0.6f);
        S(sb, "SpreadDevice", sprCircle,   P(0, 0.6f),     Sc(0.15f, 0.15f), spread);
        S(sb, "SpreadFinL",   sprTriangle, P(-0.35f, 0.4f), Sc(0.1f, 0.1f), spread);
        S(sb, "SpreadFinR",   sprTriangle, P(0.35f, 0.4f),  Sc(0.1f, 0.1f), spread);

        EndVariant(root, "Tower_Rapid");
    }

    // ───────── Helpers ─────────

    /// <summary>베리언트 프리팹 열기 + 컨테이너 내 자식만 제거</summary>
    private static GameObject BeginVariant(string name)
    {
        var path = $"{PrefabFolder}/{name}.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        CleanPartChildren(root);
        return root;
    }

    /// <summary>베리언트 프리팹 저장 (필드 연결은 Base에서 완료)</summary>
    private static void EndVariant(GameObject root, string name)
    {
        var path = $"{PrefabFolder}/{name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    /// <summary>Base 프리팹용: 컨테이너 자체를 제거 (재생성 전)</summary>
    private static void CleanContainers(GameObject root)
    {
        foreach (var partName in PartNames)
        {
            var existing = root.transform.Find(partName);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);
        }
    }

    /// <summary>베리언트용: 컨테이너는 유지하고 자식 스프라이트만 제거</summary>
    private static void CleanPartChildren(GameObject root)
    {
        foreach (var partName in PartNames)
        {
            var container = root.transform.Find(partName);
            if (container == null) continue;
            for (int i = container.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(container.GetChild(i).gameObject);
        }
    }

    private static Color GetBodyColor(GameObject root)
    {
        var sr = root.GetComponent<SpriteRenderer>();
        return sr != null ? sr.color : Color.white;
    }

    /// <summary>Base용: 빈 컨테이너 생성 (비활성)</summary>
    private static GameObject CreateContainer(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        go.SetActive(false);
        return go;
    }

    /// <summary>베리언트용: 기존 컨테이너 찾기</summary>
    private static GameObject FindPart(GameObject root, string name)
    {
        var t = root.transform.Find(name);
        if (t == null)
            Debug.LogError($"[TowerVisualGenerator] Container '{name}' not found in {root.name}. Run GenerateBase first.");
        return t?.gameObject;
    }

    private static void S(GameObject parent, string name, Sprite sprite,
        Vector3 pos, Vector3 scale, Color color)
    {
        if (parent == null) return;
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = scale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = 1;
    }

    private static Vector3 P(float x, float y) => new Vector3(x, y, 0);
    private static Vector3 Sc(float x, float y) => new Vector3(x, y, 1);
}
