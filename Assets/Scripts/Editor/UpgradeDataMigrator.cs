using UnityEditor;
using UnityEngine;

/// <summary>
/// UpgradeLevel 구조 변경(개별 필드 → TowerStats) 후 에셋 데이터 재입력용.
/// 실행 후 삭제해도 무방.
/// </summary>
public static class UpgradeDataMigrator
{
    [MenuItem("PopRouteTD/업그레이드 데이터 마이그레이션")]
    public static void Migrate()
    {
        MigrateBasicTower();
        MigrateBombTower();
        MigrateRapidTower();
        MigrateSlowTower();
        MigratePierceTower();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PopRouteTD] 업그레이드 데이터 마이그레이션 완료!");
    }

    private static TowerUpgradeData Load(string name)
    {
        var path = $"Assets/Data/TowerUpgrades/{name}.asset";
        var data = AssetDatabase.LoadAssetAtPath<TowerUpgradeData>(path);
        if (data == null) Debug.LogError($"[PopRouteTD] {path} 로드 실패");
        return data;
    }

    private static UpgradeLevel Level(string name, string desc, int cost, TowerStats stats,
        StatusEffectEntry[] effects = null)
    {
        return new UpgradeLevel
        {
            levelName = name,
            description = desc,
            cost = cost,
            stats = stats,
            statusEffects = effects ?? new StatusEffectEntry[0]
        };
    }

    private static StatusEffectEntry Effect(StatusEffectType type, float duration)
    {
        return new StatusEffectEntry { type = type, duration = duration };
    }

    // ── BasicTower ──

    private static void MigrateBasicTower()
    {
        var d = Load("BasicTower");
        if (d == null) return;

        d.towerName = "BasicTower";

        d.main1 = Level("Basic Shooter", "Single-target ranged attack", 15,
            new TowerStats { attackInterval = 1, attackRange = 3, pierceCount = 1 });

        d.main2 = Level("Basic Shooter", "SPD, RNG, pierce up", 80,
            new TowerStats { attackInterval = -0.1f, attackRange = 0.5f, pierceCount = 1 });

        d.main3 = Level("Basic Shooter", "Further stat boost", 200,
            new TowerStats { attackInterval = -0.1f, attackRange = 0.5f, pierceCount = 1 });

        d.main4 = Level("Basic Shooter", "Max performance", 500,
            new TowerStats { attackInterval = -0.1f, attackRange = 0.5f, pierceCount = 2 });

        d.subA = Level("Elite", "Balanced stat bonus", 200,
            new TowerStats { attackInterval = -0.1f, attackRange = 0.5f, pierceCount = 1 });

        d.subB = Level("Frost", "Grants slow effect", 200,
            new TowerStats(),
            new[] { Effect(StatusEffectType.Slow, 1.5f) });

        EditorUtility.SetDirty(d);
    }

    // ── BombTower ──

    private static void MigrateBombTower()
    {
        var d = Load("BombTower");
        if (d == null) return;

        d.towerName = "BombTower";

        d.main1 = Level("Bomb Shooter", "Splash area attack", 30,
            new TowerStats { attackInterval = 1.5f, attackRange = 3, splashRadius = 1, pierceCount = 1 });

        d.main2 = Level("Bomb Shooter", "SPD, RNG, splash up", 100,
            new TowerStats { attackInterval = -0.1f, attackRange = 0.5f, splashRadius = 0.2f, pierceCount = 1 });

        d.main3 = Level("Bomb Shooter", "Further stat boost", 250,
            new TowerStats { attackInterval = -0.1f, attackRange = 0.5f, splashRadius = 0.3f, pierceCount = 1 });

        d.main4 = Level("Bomb Shooter", "Max performance", 600,
            new TowerStats { attackInterval = -0.1f, attackRange = 0.5f, splashRadius = 0.3f, pierceCount = 1 });

        d.subA = Level("Wide Area", "Greatly expands splash radius", 250,
            new TowerStats { splashRadius = 1 });

        d.subB = Level("Incendiary", "Grants burn effect", 250,
            new TowerStats(),
            new[] { Effect(StatusEffectType.Burn, 3f) });

        EditorUtility.SetDirty(d);
    }

    // ── RapidTower ──

    private static void MigrateRapidTower()
    {
        var d = Load("RapidTower");
        if (d == null) return;

        d.towerName = "RapidTower";

        d.main1 = Level("Rapid Shooter", "Fast fire rate single-target attack", 20,
            new TowerStats { attackInterval = 0.4f, attackRange = 3, pierceCount = 1 });

        d.main2 = Level("Rapid Shooter", "SPD, RNG up", 80,
            new TowerStats { attackInterval = -0.05f, attackRange = 0.5f });

        d.main3 = Level("Rapid Shooter", "Further stat boost", 200,
            new TowerStats { attackInterval = -0.05f, attackRange = 0.5f, pierceCount = 1 });

        d.main4 = Level("Rapid Shooter", "Max performance", 500,
            new TowerStats { attackInterval = -0.05f, attackRange = 0.5f, pierceCount = 1 });

        d.subA = Level("Enhance", "Greatly increases pierce", 200,
            new TowerStats { pierceCount = 2 });

        d.subB = Level("Spread", "Converts to splash area attack", 200,
            new TowerStats { splashRadius = 1 });

        EditorUtility.SetDirty(d);
    }

    // ── SlowTower ──

    private static void MigrateSlowTower()
    {
        var d = Load("SlowTower");
        if (d == null) return;

        d.towerName = "SlowTower";

        d.main1 = Level("Slow Shooter", "Attacks apply slow effect", 20,
            new TowerStats { attackInterval = 1.5f, attackRange = 3.5f, pierceCount = 1 },
            new[] { Effect(StatusEffectType.Slow, 1f) });

        d.main2 = Level("Slow Shooter", "Slow duration, RNG up", 80,
            new TowerStats { attackInterval = -0.1f, attackRange = 0.5f },
            new[] { Effect(StatusEffectType.Slow, 0.5f) });

        d.main3 = Level("Slow Shooter", "Further stat boost", 200,
            new TowerStats { attackInterval = -0.1f, attackRange = 0.5f },
            new[] { Effect(StatusEffectType.Slow, 0.5f) });

        d.main4 = Level("Slow Shooter", "Max performance", 500,
            new TowerStats { attackInterval = -0.1f, attackRange = 0.5f },
            new[] { Effect(StatusEffectType.Slow, 0.5f) });

        d.subA = Level("Frostbite", "Adds burn effect", 200,
            new TowerStats(),
            new[] { Effect(StatusEffectType.Burn, 2f) });

        d.subB = Level("Freeze", "Greatly extends slow duration", 200,
            new TowerStats(),
            new[] { Effect(StatusEffectType.Slow, 1.5f) });

        EditorUtility.SetDirty(d);
    }

    // ── PierceTower ──

    private static void MigratePierceTower()
    {
        var d = Load("PierceTower");
        if (d == null) return;

        d.towerName = "PierceTower";

        d.main1 = Level("Pierce Shooter", "Piercing projectile hits multiple targets", 25,
            new TowerStats { attackInterval = 1.2f, attackRange = 4, pierceCount = 2 });

        d.main2 = Level("Pierce Shooter", "SPD, pierce up", 120,
            new TowerStats { attackInterval = -0.1f, attackRange = 0.5f, pierceCount = 1 });

        d.main3 = Level("Pierce Shooter", "Further stat boost", 300,
            new TowerStats { attackInterval = -0.2f, attackRange = 0.5f, pierceCount = 1 });

        d.main4 = Level("Pierce Shooter", "Max performance", 700,
            new TowerStats { attackInterval = -0.2f, attackRange = 0.5f, pierceCount = 1 });

        d.subA = Level("Rapid Fire", "Greatly increases attack speed", 300,
            new TowerStats { attackInterval = -0.3f, pierceCount = 1 });

        d.subB = Level("Impact", "Grants stun effect", 300,
            new TowerStats { pierceCount = 2 },
            new[] { Effect(StatusEffectType.Stun, 0.5f) });

        EditorUtility.SetDirty(d);
    }
}
