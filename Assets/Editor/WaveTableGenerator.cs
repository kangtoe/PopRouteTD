using UnityEngine;
using UnityEditor;

public static class WaveTableGenerator
{
    private const string AssetPath = "Assets/Data/WaveTable.asset";

    [MenuItem("Tools/Generate Wave Table")]
    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");

        var table = AssetDatabase.LoadAssetAtPath<WaveTable>(AssetPath);
        if (table == null)
        {
            table = ScriptableObject.CreateInstance<WaveTable>();
            AssetDatabase.CreateAsset(table, AssetPath);
        }

        var waves = new System.Collections.Generic.List<WaveData>();

        // ── Wave 1~5 ──
        waves.Add(Wave(1, G(L.Red, 20)));
        waves.Add(Wave(2, G(L.Red, 35)));
        waves.Add(Wave(3, G(L.Red, 25), G(L.Orange, 5)));
        waves.Add(Wave(4, G(L.Red, 35), G(L.Orange, 18)));
        waves.Add(Wave(5, G(L.Red, 5), G(L.Orange, 27)));

        // ── Wave 6~10 ──
        waves.Add(Wave(6, G(L.Red, 15), G(L.Orange, 15), G(L.Yellow, 4)));
        waves.Add(Wave(7, G(L.Red, 20), G(L.Orange, 20), G(L.Yellow, 5)));
        waves.Add(Wave(8, G(L.Red, 10), G(L.Orange, 20), G(L.Yellow, 14)));
        waves.Add(Wave(9, G(L.Yellow, 30)));
        waves.Add(Wave(10, G(L.Orange, 102))); // Rush: 102 Blues

        // ── Wave 11~15 ──
        waves.Add(Wave(11, G(L.Red, 10), G(L.Orange, 10), G(L.Yellow, 12), G(L.Green, 3)));
        waves.Add(Wave(12, G(L.Orange, 15), G(L.Yellow, 10), G(L.Green, 5)));
        waves.Add(Wave(13, G(L.Orange, 50), G(L.Yellow, 23)));
        waves.Add(Wave(14, G(L.Red, 49), G(L.Orange, 15), G(L.Yellow, 10), G(L.Green, 9)));
        waves.Add(Wave(15, G(L.Red, 20), G(L.Orange, 15), G(L.Yellow, 12),
            G(L.Green, 10), G(L.Blue, 5)));

        // ── Wave 16~20 ──
        waves.Add(Wave(16, G(L.Yellow, 40), G(L.Green, 8)));
        waves.Add(Wave(17, G(L.Green, 12, V.Enhanced))); // Regrow Yellows
        waves.Add(Wave(18, G(L.Yellow, 80)));
        waves.Add(Wave(19, G(L.Yellow, 10), G(L.Green, 4),
            G(L.Green, 5, V.Enhanced), G(L.Blue, 15)));
        waves.Add(Wave(20, G(L.Indigo, 6))); // Elite: Blacks

        // ── Wave 21~25 ──
        waves.Add(Wave(21, G(L.Green, 40), G(L.Blue, 14)));
        waves.Add(Wave(22, G(L.Indigo, 16))); // Whites
        waves.Add(Wave(23, G(L.Indigo, 14))); // Blacks 7 + Whites 7
        waves.Add(Wave(24, G(L.Orange, 20), G(L.Yellow, 1, V.Shielded))); // Camo Green
        waves.Add(Wave(25, G(L.Green, 25, V.Enhanced), G(L.Purple, 10))); // Regrow Yellows + Purples

        // ── Wave 26~30 ──
        waves.Add(Wave(26, G(L.Blue, 23), G(L.Indigo, 4))); // Pinks + Zebras
        waves.Add(Wave(27, G(L.Red, 100), G(L.Orange, 60),
            G(L.Yellow, 45), G(L.Green, 45))); // Rush
        waves.Add(Wave(28, G(L.Purple, 6, V.Shielded))); // Leads
        waves.Add(Wave(29, G(L.Green, 50), G(L.Green, 15, V.Enhanced))); // Yellows + Regrow Yellows
        waves.Add(Wave(30, G(L.Purple, 9, V.Shielded))); // Elite: Leads

        // ── Wave 31~36 ──
        waves.Add(Wave(31, G(L.Indigo, 24), G(L.Indigo, 2, V.Enhanced))); // B+W+Z + Regrow Zebras
        waves.Add(Wave(32, G(L.Indigo, 35), G(L.Purple, 10)));
        waves.Add(Wave(33, G(L.Red, 20, V.Shielded), G(L.Green, 13, V.Shielded))); // Camo
        waves.Add(Wave(34, G(L.Green, 160), G(L.Indigo, 6)));
        waves.Add(Wave(35, G(L.Blue, 35), G(L.Indigo, 55), G(L.Purple, 5)));
        waves.Add(Wave(36, G(L.Blue, 140),
            G(L.Yellow, 20, V.EnhancedShielded))); // Camo Regrow Greens

        // ── Wave 37~40 ──
        waves.Add(Wave(37, G(L.Indigo, 60), G(L.Indigo, 7, V.Shielded),
            G(L.Purple, 15, V.Shielded))); // B+W+Z, Camo Whites, Leads
        waves.Add(Wave(38, G(L.Blue, 42), G(L.Indigo, 27),
            G(L.Purple, 14, V.Shielded), G(L.Purple, 2, V.Enhanced))); // Pinks,W+Z,Leads,Ceramics
        waves.Add(Wave(39, G(L.Indigo, 40), G(L.Purple, 18),
            G(L.Purple, 2, V.Enhanced))); // B+W+Z, Rainbows, Regrow Rainbows
        waves.Add(Wave(40, G(L.Purple, 1, V.EnhancedShielded))); // MOAB

        // SO의 waves 필드에 리플렉션으로 설정 (private 필드)
        var field = typeof(WaveTable).GetField("waves",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field.SetValue(table, waves);

        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[WaveTableGenerator] Generated {waves.Count} waves at {AssetPath}");
    }

    // ── Aliases ──
    private static class L
    {
        public const BalloonLayer Red = BalloonLayer.Red;
        public const BalloonLayer Orange = BalloonLayer.Orange;
        public const BalloonLayer Yellow = BalloonLayer.Yellow;
        public const BalloonLayer Green = BalloonLayer.Green;
        public const BalloonLayer Blue = BalloonLayer.Blue;
        public const BalloonLayer Indigo = BalloonLayer.Indigo;
        public const BalloonLayer Purple = BalloonLayer.Purple;
    }

    private static class V
    {
        public const EnemyVariant Normal = EnemyVariant.Normal;
        public const EnemyVariant Shielded = EnemyVariant.Shielded;
        public const EnemyVariant Enhanced = EnemyVariant.Enhanced;
        public const EnemyVariant EnhancedShielded = EnemyVariant.EnhancedShielded;
    }

    // ── Helpers ──

    private static SpawnGroupData G(BalloonLayer layer, int count,
        EnemyVariant variant = EnemyVariant.Normal)
    {
        return new SpawnGroupData
        {
            layer = layer,
            variant = variant,
            count = count,
            interval = -1f,
        };
    }

    private static WaveData Wave(int waveNumber, params SpawnGroupData[] groups)
    {
        float interval = Mathf.Max(0.2f, 0.8f - (waveNumber - 1) * 0.02f);

        var wave = new WaveData();
        foreach (var g in groups)
        {
            if (g.interval < 0) g.interval = interval;
            wave.groups.Add(g);
        }
        return wave;
    }
}
