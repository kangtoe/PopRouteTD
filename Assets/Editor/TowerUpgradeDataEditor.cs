using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TowerUpgradeData))]
public class TowerUpgradeDataEditor : Editor
{
    private bool showAnalysis = true;
    private GUIStyle _header;
    private GUIStyle _cell;
    private GUIStyle _cellLeft;
    private GUIStyle _rich;

    private GUIStyle HeaderStyle => _header ??= new GUIStyle(EditorStyles.miniLabel)
    {
        fontStyle = FontStyle.Bold,
        alignment = TextAnchor.MiddleCenter
    };

    private GUIStyle CellStyle => _cell ??= new GUIStyle(EditorStyles.miniLabel)
    {
        alignment = TextAnchor.MiddleCenter
    };

    private GUIStyle CellLeftStyle => _cellLeft ??= new GUIStyle(EditorStyles.miniLabel)
    {
        alignment = TextAnchor.MiddleLeft
    };

    private GUIStyle RichStyle => _rich ??= new GUIStyle(EditorStyles.label)
    {
        richText = true
    };

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var data = (TowerUpgradeData)target;
        if (data.main1?.stats == null) return;

        EditorGUILayout.Space(15);
        DrawSeparator();
        EditorGUILayout.Space(3);

        showAnalysis = EditorGUILayout.Foldout(showAnalysis, "밸런스 분석", true, EditorStyles.foldoutHeader);
        if (!showAnalysis) return;

        UpgradeLevel[] mains = { data.main1, data.main2, data.main3, data.main4 };

        DrawMainTable(mains);
        DrawSubComparison(data, mains);
        DrawInvestmentCheck(data, mains);
    }

    // ─── 주 모듈 누적 테이블 ─────────────────────────

    private void DrawMainTable(UpgradeLevel[] mains)
    {
        var cum = CumStats(mains);
        var costs = CumCosts(mains);

        bool sp = false, pi = false;
        foreach (var s in cum) { sp |= s.splashRadius > 0; pi |= s.pierceCount > 0; }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("주 모듈 (누적)", EditorStyles.boldLabel);

        // 헤더
        EditorGUILayout.BeginHorizontal();
        H("Lv", 24); H("ATK", 36); H("주기", 38); H("사거리", 42);
        if (sp) H("스플", 36);
        if (pi) H("관통", 34);
        H("누적비용", 52); H("DPS", 44);
        if (sp) H("효율DPS", 50);
        H("DPS/비용", 55);
        EditorGUILayout.EndHorizontal();

        // 데이터 행
        for (int i = 0; i < 4; i++)
        {
            var s = cum[i];
            float dps = CalcDps(s);
            float eff = EffDps(s, dps);
            float dpc = costs[i] > 0 ? dps / costs[i] : 0;

            var row = EditorGUILayout.BeginHorizontal();
            if (i % 2 == 1) EditorGUI.DrawRect(row, new Color(0.5f, 0.5f, 0.5f, 0.06f));

            C($"{i + 1}", 24); C($"{s.attackDamage:F0}", 36);
            C($"{s.attackInterval:F2}", 38); C($"{s.attackRange:F1}", 42);
            if (sp) C(s.splashRadius > 0 ? $"{s.splashRadius:F1}" : "-", 36);
            if (pi) C(s.pierceCount > 0 ? $"{s.pierceCount}" : "-", 34);
            C($"{costs[i]}", 52); C($"{dps:F2}", 44);
            if (sp) C(Mathf.Abs(eff - dps) > 0.01f ? $"{eff:F2}" : "-", 50);
            C($"{dpc:F4}", 55);

            EditorGUILayout.EndHorizontal();
        }

        // 비고
        if (pi) EditorGUILayout.LabelField("  * DPS = ATK × 관통수 / 주기", EditorStyles.miniLabel);
        if (sp) EditorGUILayout.LabelField("  * 효율DPS: DPS × 2.5 (추정 평균 적중)", EditorStyles.miniLabel);

        var fx = AccumEffects(mains, null);
        if (fx != "-")
            EditorGUILayout.LabelField($"  상태이상 (Lv4 누적): {fx}", EditorStyles.miniLabel);
    }

    // ─── 서브 모듈 비교 ──────────────────────────────

    private void DrawSubComparison(TowerUpgradeData data, UpgradeLevel[] mains)
    {
        if (data.subA == null && data.subB == null) return;

        var lv4 = CumStats(mains)[3];
        int lv4Cost = CumCosts(mains)[3];

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("서브 모듈 비교 (Lv4 기준)", EditorStyles.boldLabel);

        // 헤더
        EditorGUILayout.BeginHorizontal();
        H("구성", 78); H("ATK", 36); H("주기", 38);
        H("DPS", 44); H("총비용", 52); H("DPS/비용", 55);
        GUILayout.Label("효과", HeaderStyle);
        EditorGUILayout.EndHorizontal();

        // Lv4 기본
        SubRow("Lv4 기본", lv4, lv4Cost, AccumEffects(mains, null));

        // + Sub A
        if (data.subA != null)
        {
            var s = Clone(lv4);
            if (data.subA.stats != null) s.Add(data.subA.stats);
            SubRow($"+ {data.subA.levelName}", s, lv4Cost + data.subA.cost, AccumEffects(mains, data.subA));
        }

        // + Sub B
        if (data.subB != null)
        {
            var s = Clone(lv4);
            if (data.subB.stats != null) s.Add(data.subB.stats);
            SubRow($"+ {data.subB.levelName}", s, lv4Cost + data.subB.cost, AccumEffects(mains, data.subB));
        }
    }

    private void SubRow(string label, TowerStats s, int cost, string fx)
    {
        float dps = CalcDps(s);
        float dpc = cost > 0 ? dps / cost : 0;

        EditorGUILayout.BeginHorizontal();
        C(label, 78); C($"{s.attackDamage:F0}", 36); C($"{s.attackInterval:F2}", 38);
        C($"{dps:F2}", 44); C($"{cost}", 52); C($"{dpc:F4}", 55);
        GUILayout.Label(fx, CellLeftStyle);
        EditorGUILayout.EndHorizontal();
    }

    // ─── 모듈별 투자 효율 ─────────────────────────────

    private void DrawInvestmentCheck(TowerUpgradeData data, UpgradeLevel[] mains)
    {
        int baseCost = mains[0].cost;
        if (baseCost <= 0) return;

        var cum = CumStats(mains);
        float[] dps = new float[4];
        for (int i = 0; i < 4; i++) dps[i] = CalcDps(cum[i]);

        float baselineEff = GetBasicTowerEfficiency();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("모듈별 투자 효율", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            $"  기준: BasicTower Lv1 = {baselineEff:F4} DPS/비용",
            EditorStyles.miniLabel);
        EditorGUILayout.Space(3);

        // 헤더
        EditorGUILayout.BeginHorizontal();
        H("모듈", 55); H("비용", 38); H("\u0394DPS", 48); H("한계효율", 55);
        GUILayout.Label("vs 기준", HeaderStyle);
        EditorGUILayout.EndHorizontal();

        // Lv1 배치
        EfficiencyRow("Lv1", baseCost, dps[0], baselineEff, LevelEffects(mains[0]));

        // Lv2~4
        for (int i = 1; i < 4; i++)
        {
            if (mains[i] == null) continue;
            float delta = dps[i] - dps[i - 1];
            EfficiencyRow($"Lv{i + 1}", mains[i].cost, delta, baselineEff, LevelEffects(mains[i]));
        }

        // Sub A
        if (data.subA != null)
        {
            var withA = Clone(cum[3]);
            if (data.subA.stats != null) withA.Add(data.subA.stats);
            float delta = CalcDps(withA) - dps[3];
            EfficiencyRow("Sub A", data.subA.cost, delta, baselineEff, LevelEffects(data.subA));
        }

        // Sub B
        if (data.subB != null)
        {
            var withB = Clone(cum[3]);
            if (data.subB.stats != null) withB.Add(data.subB.stats);
            float delta = CalcDps(withB) - dps[3];
            EfficiencyRow("Sub B", data.subB.cost, delta, baselineEff, LevelEffects(data.subB));
        }

        // 총 투자 비율
        EditorGUILayout.Space(5);
        int lv4Cost = CumCosts(mains)[3];
        TotalLine("Lv4 (메인)", lv4Cost, baseCost);
        if (data.subA != null)
            TotalLine($"+ {data.subA.levelName}", lv4Cost + data.subA.cost, baseCost);
        if (data.subB != null)
            TotalLine($"+ {data.subB.levelName}", lv4Cost + data.subB.cost, baseCost);

        EditorGUILayout.Space(3);
        EditorGUILayout.HelpBox(
            $"설계 목표: 총 투자 = 배치비용({baseCost})의 5~6배 = {baseCost * 5}~{baseCost * 6}\n" +
            "한계효율 vs 기준 > 1.0 → 업그레이드가 신규 배치보다 효율적 (밸런스 주의)\n" +
            "기준 = BasicTower Lv1 DPS/비용",
            MessageType.Info);
    }

    private static float GetBasicTowerEfficiency()
    {
        const string BasicTowerName = "BasicTower";
        var guids = AssetDatabase.FindAssets("t:TowerUpgradeData");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var td = AssetDatabase.LoadAssetAtPath<TowerUpgradeData>(path);
            if (td == null || td.towerName != BasicTowerName) continue;
            if (td.main1 == null || td.main1.stats == null || td.main1.cost <= 0) return 0f;
            return CalcDps(td.main1.stats) / td.main1.cost;
        }
        return 0f;
    }

    private void EfficiencyRow(string label, int cost, float deltaDps, float baseEff, string fx)
    {
        bool hasDps = deltaDps > 0.01f;
        bool hasFx = !string.IsNullOrEmpty(fx);

        EditorGUILayout.BeginHorizontal();
        C(label, 55);
        C($"{cost}", 38);

        if (hasDps)
        {
            float marginal = cost > 0 ? deltaDps / cost : 0;
            float ratio = baseEff > 0 ? marginal / baseEff : 0;
            bool ok = ratio <= 1.05f;
            string color = ok ? "#4CAF50" : "#F44336";
            string icon = ok ? "\u2713" : "\u2717";
            string extra = hasFx ? $"  + {fx}" : "";

            C($"+{deltaDps:F2}", 48);
            C($"{marginal:F4}", 55);
            GUILayout.Label($"<color={color}>{icon} {ratio:F2}\ubc30</color>{extra}", RichStyle);
        }
        else
        {
            C("-", 48); C("-", 55);
            GUILayout.Label(hasFx ? $"<color=#2196F3>{fx}</color>" : "-", RichStyle);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void TotalLine(string label, int total, int baseCost)
    {
        float ratio = (float)total / baseCost;
        bool ok = ratio >= 4.5f && ratio <= 6.5f;
        string color = ok ? "#4CAF50" : "#F44336";
        string icon = ok ? "\u2713" : "\u2717";
        EditorGUILayout.LabelField(
            $"  <color={color}>{icon}</color> {label}: {total} / {baseCost} = <b>{ratio:F1}\ubc30</b>",
            RichStyle);
    }

    private static string LevelEffects(UpgradeLevel level)
    {
        if (level?.statusEffects == null || level.statusEffects.Length == 0) return "";
        var sb = new System.Text.StringBuilder();
        foreach (var e in level.statusEffects)
        {
            if (e.type == StatusEffectType.None || e.duration <= 0) continue;
            if (sb.Length > 0) sb.Append(" + ");
            sb.Append($"{e.type} {e.duration:F1}s");
        }
        return sb.ToString();
    }

    // ─── 유틸리티 ────────────────────────────────────

    private void H(string text, float w) => GUILayout.Label(text, HeaderStyle, GUILayout.Width(w));
    private void C(string text, float w) => GUILayout.Label(text, CellStyle, GUILayout.Width(w));

    private static float CalcDps(TowerStats s) =>
        s.attackInterval > 0 ? s.attackDamage * Mathf.Max(1, s.pierceCount) / s.attackInterval : 0;

    private static float EffDps(TowerStats s, float dps)
    {
        if (s.splashRadius > 0) return dps * 2.5f;
        return dps;
    }

    private static TowerStats[] CumStats(UpgradeLevel[] levels)
    {
        var r = new TowerStats[4];
        r[0] = Clone(levels[0].stats);
        for (int i = 1; i < 4; i++)
        {
            r[i] = Clone(r[i - 1]);
            if (levels[i]?.stats != null) r[i].Add(levels[i].stats);
        }
        return r;
    }

    private static int[] CumCosts(UpgradeLevel[] levels)
    {
        var r = new int[4];
        r[0] = levels[0].cost;
        for (int i = 1; i < 4; i++)
            r[i] = r[i - 1] + (levels[i]?.cost ?? 0);
        return r;
    }

    private static TowerStats Clone(TowerStats s) => new()
    {
        attackDamage = s.attackDamage,
        attackInterval = s.attackInterval,
        attackRange = s.attackRange,
        splashRadius = s.splashRadius,
        pierceCount = s.pierceCount
    };

    private static string AccumEffects(UpgradeLevel[] mains, UpgradeLevel sub)
    {
        int count = System.Enum.GetValues(typeof(StatusEffectType)).Length;
        float[] dur = new float[count];

        for (int i = 0; i < 4; i++)
        {
            if (mains[i]?.statusEffects == null) continue;
            foreach (var e in mains[i].statusEffects)
                dur[(int)e.type] += e.duration;
        }

        if (sub?.statusEffects != null)
            foreach (var e in sub.statusEffects)
                dur[(int)e.type] += e.duration;

        var sb = new System.Text.StringBuilder();
        for (int i = 1; i < count; i++)
        {
            if (dur[i] <= 0) continue;
            if (sb.Length > 0) sb.Append(" + ");
            sb.Append($"{(StatusEffectType)i} {dur[i]:F1}s");
        }
        return sb.Length > 0 ? sb.ToString() : "-";
    }

    private static void DrawSeparator()
    {
        var r = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(r, new Color(0.5f, 0.5f, 0.5f, 0.5f));
    }
}
