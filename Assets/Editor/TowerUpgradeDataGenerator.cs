using UnityEngine;
using UnityEditor;

public static class TowerUpgradeDataGenerator
{
    private const string Folder = "Assets/Data/TowerUpgrades";

    [MenuItem("Tools/Generate Tower Upgrade Data")]
    public static void GenerateAll()
    {
        EnsureFolder();

        CreateBasicTower();
        CreateBombTower();
        CreatePierceTower();
        CreateSlowTower();
        CreateRapidTower();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[TowerUpgradeDataGenerator] Generated 5 tower upgrade data assets.");
    }

    // ───────── Basic Tower ─────────
    // Main: balanced single-target ranged attack
    // Sub A (Elite): stat bonus   Sub B (Frost): slow effect
    private static void CreateBasicTower()
    {
        var so = ScriptableObject.CreateInstance<TowerUpgradeData>();
        so.towerName = "BasicTower";

        so.main1 = Level("Basic Shooter", 100, "Single-target ranged attack",
            interval: 1.0f, range: 3.0f, pierce: 1);
        so.main2 = Level("Basic Shooter", 110, "SPD, RNG, pierce up",
            interval: -0.05f, range: 0.5f, pierce: 1);
        so.main3 = Level("Basic Shooter", 130, "Further stat boost",
            interval: -0.05f, range: 0.5f, pierce: 1);
        so.main4 = Level("Basic Shooter", 150, "Max performance",
            interval: -0.05f, range: 0.5f, pierce: 1);

        so.subA = Level("Elite", 100, "Balanced stat bonus",
            range: 0.5f, pierce: 1);
        so.subB = Level("Frost", 100, "Grants slow effect",
            effects: new[] { Effect(StatusEffectType.Slow, 1.5f) });

        Save(so, "BasicTower");
    }

    // ───────── Bomb Tower ─────────
    // Main: splash area attack
    // Sub A (Wide): splash radius up   Sub B (Incendiary): burn effect
    private static void CreateBombTower()
    {
        var so = ScriptableObject.CreateInstance<TowerUpgradeData>();
        so.towerName = "BombTower";

        so.main1 = Level("Bomb Shooter", 150, "Splash area attack",
            interval: 1.5f, range: 3.0f, splash: 1.0f, pierce: 1);
        so.main2 = Level("Bomb Shooter", 165, "SPD, RNG, splash up",
            interval: -0.05f, range: 0.5f, splash: 0.2f, pierce: 1);
        so.main3 = Level("Bomb Shooter", 195, "Further stat boost",
            interval: -0.05f, range: 0.5f, splash: 0.3f, pierce: 1);
        so.main4 = Level("Bomb Shooter", 225, "Max performance",
            interval: -0.05f, range: 0.5f, splash: 0.3f, pierce: 1);

        so.subA = Level("Wide Area", 150, "Greatly expands splash radius",
            splash: 1.0f);
        so.subB = Level("Incendiary", 150, "Grants burn effect",
            effects: new[] { Effect(StatusEffectType.Burn, 3.0f) });

        Save(so, "BombTower");
    }

    // ───────── Pierce Tower ─────────
    // Main: piercing projectile attack
    // Sub A (Rapid Fire): attack speed bonus   Sub B (Impact): stun effect
    private static void CreatePierceTower()
    {
        var so = ScriptableObject.CreateInstance<TowerUpgradeData>();
        so.towerName = "PierceTower";

        so.main1 = Level("Pierce Shooter", 200, "Piercing projectile hits multiple targets",
            interval: 1.2f, range: 4.0f, pierce: 2);
        so.main2 = Level("Pierce Shooter", 220, "SPD, pierce up",
            interval: -0.1f, range: 0.5f, pierce: 1);
        so.main3 = Level("Pierce Shooter", 260, "SPD, RNG up",
            interval: -0.1f, range: 0.5f);
        so.main4 = Level("Pierce Shooter", 300, "Max performance",
            interval: -0.1f, range: 0.5f, pierce: 1);

        so.subA = Level("Rapid Fire", 200, "Greatly increases attack speed",
            interval: -0.15f, pierce: 1);
        so.subB = Level("Impact", 200, "Grants stun effect",
            pierce: 2,
            effects: new[] { Effect(StatusEffectType.Stun, 0.5f) });

        Save(so, "PierceTower");
    }

    // ───────── Slow Tower ─────────
    // Main: slow effect attack
    // Sub A (Frostbite): adds burn   Sub B (Freeze): greatly extends slow duration
    private static void CreateSlowTower()
    {
        var so = ScriptableObject.CreateInstance<TowerUpgradeData>();
        so.towerName = "SlowTower";

        so.main1 = Level("Slow Shooter", 100, "Attacks apply slow effect",
            interval: 1.5f, range: 3.5f, pierce: 1,
            effects: new[] { Effect(StatusEffectType.Slow, 1.0f) });
        so.main2 = Level("Slow Shooter", 110, "Slow duration, SPD, RNG up",
            interval: -0.05f, range: 0.5f,
            effects: new[] { Effect(StatusEffectType.Slow, 0.5f) });
        so.main3 = Level("Slow Shooter", 130, "Further stat boost",
            interval: -0.05f, range: 0.5f,
            effects: new[] { Effect(StatusEffectType.Slow, 0.5f) });
        so.main4 = Level("Slow Shooter", 150, "Max performance",
            interval: -0.05f, range: 0.5f,
            effects: new[] { Effect(StatusEffectType.Slow, 0.5f) });

        so.subA = Level("Frostbite", 100, "Adds burn effect",
            effects: new[] { Effect(StatusEffectType.Burn, 2.0f) });
        so.subB = Level("Freeze", 100, "Greatly extends slow duration",
            effects: new[] { Effect(StatusEffectType.Slow, 1.5f) });

        Save(so, "SlowTower");
    }

    // ───────── Rapid Tower ─────────
    // Main: fast fire rate attack, shorter range
    // Sub A (Enhance): pierce bonus   Sub B (Spread): splash conversion
    private static void CreateRapidTower()
    {
        var so = ScriptableObject.CreateInstance<TowerUpgradeData>();
        so.towerName = "RapidTower";

        so.main1 = Level("Rapid Shooter", 200, "Fast fire rate single-target attack",
            interval: 0.5f, range: 2.5f, pierce: 1);
        so.main2 = Level("Rapid Shooter", 220, "SPD, RNG up",
            interval: -0.03f, range: 0.25f, pierce: 1);
        so.main3 = Level("Rapid Shooter", 260, "Further stat boost",
            interval: -0.03f, range: 0.25f, pierce: 1);
        so.main4 = Level("Rapid Shooter", 300, "Max performance",
            interval: -0.03f, range: 0.25f, pierce: 1);

        so.subA = Level("Enhance", 200, "Increases pierce",
            pierce: 1);
        so.subB = Level("Spread", 200, "Converts to splash area attack",
            splash: 1.0f);

        Save(so, "RapidTower");
    }

    // ───────── Helpers ─────────

    private static StatusEffectEntry Effect(StatusEffectType type, float duration)
    {
        return new StatusEffectEntry { type = type, duration = duration };
    }

    private static UpgradeLevel Level(
        string name, int cost, string desc = "",
        float interval = 0, float range = 0,
        float splash = 0, int pierce = 0,
        StatusEffectEntry[] effects = null)
    {
        return new UpgradeLevel
        {
            upgradeName = name,
            description = desc,
            cost = cost,
            stats = new TowerStats
            {
                attackInterval = interval,
                attackRange = range,
                splashRadius = splash,
                pierceCount = pierce,
            },
            statusEffects = effects ?? new StatusEffectEntry[0],
        };
    }

    private static void Save(TowerUpgradeData so, string fileName)
    {
        string path = $"{Folder}/{fileName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<TowerUpgradeData>(path);
        if (existing != null)
        {
            existing.towerName = so.towerName;
            existing.main1 = so.main1;
            existing.main2 = so.main2;
            existing.main3 = so.main3;
            existing.main4 = so.main4;
            existing.subA = so.subA;
            existing.subB = so.subB;
            EditorUtility.SetDirty(existing);
        }
        else
        {
            AssetDatabase.CreateAsset(so, path);
        }
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Data", "TowerUpgrades");
    }
}
