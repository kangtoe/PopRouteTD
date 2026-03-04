using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TowerUpgradeData))]
public class TowerUpgradeDataEditor : Editor
{
    private bool showAnalysis = true;
    private float enemySpeed = 1.0f;
    private GUIStyle _header;
    private GUIStyle _cell;
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

        DrawAnalysis(data, mains);
    }

    // ─── 밸런스 분석 테이블 ─────────────────────────

    private void DrawAnalysis(TowerUpgradeData data, UpgradeLevel[] mains)
    {
        var cum = CumStats(mains);
        var costs = CumCosts(mains);
        int baseCost = mains[0].cost;

        bool sp = false;
        foreach (var s in cum) { sp |= s.splashRadius > 0; }
        if (data.subA?.stats != null) { sp |= data.subA.stats.splashRadius > 0; }
        if (data.subB?.stats != null) { sp |= data.subB.stats.splashRadius > 0; }

        float scoutDps = 0f, scoutRange = 0f;
        int scoutCost = 0;
        if (baseCost > 0) GetScoutTowerInfo(out scoutDps, out scoutRange, out scoutCost);
        float basicDwell = enemySpeed > 0 ? 2f * scoutRange / enemySpeed : 0;
        float scoutExpDmg = scoutDps * basicDwell;
        float baselineEff = scoutCost > 0 ? scoutExpDmg / scoutCost : 0;
        float[] dpsArr = new float[4];
        float[] cumExpDmg = new float[4];
        for (int i = 0; i < 4; i++)
        {
            dpsArr[i] = CalcDps(cum[i]);
            float dwell = enemySpeed > 0 ? 2f * cum[i].attackRange / enemySpeed : 0;
            cumExpDmg[i] = EffDps(cum[i], dpsArr[i]) * dwell;
        }

        // 적 속도 슬라이더
        EditorGUILayout.Space(5);
        enemySpeed = EditorGUILayout.Slider("적 이동속도", enemySpeed, 1.0f, 4.0f);

        string speedRef = enemySpeed <= 1.0f ? "(빨강)" :
                          enemySpeed <= 1.5f ? "(주황)" :
                          enemySpeed <= 2.0f ? "(노랑)" :
                          enemySpeed <= 2.5f ? "(초록)" :
                          enemySpeed <= 3.0f ? "(파랑)" :
                          enemySpeed <= 3.5f ? "(남색)" : "(보라)";
        EditorGUILayout.LabelField(
            $"  체류시간 = 2 × 사거리 / {enemySpeed:F1} {speedRef}    |    기준: Scout Lv1 기대피해={scoutExpDmg:F2}, 기대피해/비용×100={baselineEff * 100:F2}",
            EditorStyles.miniLabel);
        EditorGUILayout.Space(3);

        // ── 단일 (레벨별) 테이블 ──
        EditorGUILayout.LabelField("▸ 단일 (레벨별)", EditorStyles.boldLabel);

        DrawTableHeader(sp);

        for (int i = 0; i < 4; i++)
        {
            var ls = mains[i].stats;
            if (ls == null) continue;
            string fxText = LevelEffects(mains[i]);

            var row = EditorGUILayout.BeginHorizontal();
            if (i % 2 == 1) EditorGUI.DrawRect(row, new Color(0.5f, 0.5f, 0.5f, 0.06f));

            C($"{i + 1}", 28);
            C(FormatDelta(ls.pierceCount, i == 0), 30);
            C(FormatDelta(ls.attackInterval, i == 0), 36);
            C(FormatDelta(ls.attackRange, i == 0), 38);
            if (sp)
            {
                C(ls.splashRadius != 0 ? FormatDelta(ls.splashRadius, i == 0) : "-", 32);
                C(ls.areaTargets != 0 ? FormatDelta(ls.areaTargets, i == 0) : "-", 30);
            }
            C($"{mains[i].cost}", 42);

            float deltaDps = dpsArr[i] - (i > 0 ? dpsArr[i - 1] : 0);
            float deltaEffMaxDmg = cumExpDmg[i] - (i > 0 ? cumExpDmg[i - 1] : 0);

            C(FormatDelta(deltaDps, i == 0, "F2"), 40);
            C(FormatDelta(deltaEffMaxDmg, i == 0, "F1"), 48);
            C(VsDps(deltaDps, scoutDps, i == 0), 50);
            C(VsExpDmg(deltaEffMaxDmg, scoutExpDmg, i == 0), 50);
            DrawVsBaseline(deltaEffMaxDmg, mains[i].cost, baselineEff, 50);
            DrawEffectLabel(fxText);

            EditorGUILayout.EndHorizontal();
        }

        if (data.subA != null || data.subB != null)
        {
            EditorGUILayout.Space(2);
            DrawSingleSubRow("A", data.subA, cum[3], dpsArr[3], cumExpDmg[3], baselineEff, scoutDps, scoutExpDmg, sp);
            DrawSingleSubRow("B", data.subB, cum[3], dpsArr[3], cumExpDmg[3], baselineEff, scoutDps, scoutExpDmg, sp);
        }

        // ── 누적 테이블 ──
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("▸ 누적", EditorStyles.boldLabel);

        DrawTableHeader(sp);

        for (int i = 0; i < 4; i++)
        {
            var s = cum[i];
            float dps = dpsArr[i];
            float effMaxDmg = cumExpDmg[i];

            float deltaEffMaxDmg = cumExpDmg[i] - (i > 0 ? cumExpDmg[i - 1] : 0);
            int levelCost = mains[i].cost;
            string fxText = AccumEffects(mains, i);

            var row = EditorGUILayout.BeginHorizontal();
            if (i % 2 == 1) EditorGUI.DrawRect(row, new Color(0.5f, 0.5f, 0.5f, 0.06f));

            C($"{i + 1}", 28); C($"{s.pierceCount}", 30);
            C($"{s.attackInterval:F2}", 36); C($"{s.attackRange:F1}", 38);
            if (sp)
            {
                C(s.splashRadius > 0 ? $"{s.splashRadius:F1}" : "-", 32);
                C(s.areaTargets > 0 ? $"{s.areaTargets}" : "-", 30);
            }
            C($"{costs[i]}", 42); C($"{dps:F2}", 40);
            C(float.IsNaN(effMaxDmg) ? "ERR" : $"{effMaxDmg:F1}", 48);
            C(scoutDps > 0 ? $"{dps / scoutDps:F2}\ubc30" : "-", 50);
            C(VsExpDmg(effMaxDmg, scoutExpDmg, true), 50);

            DrawVsBaseline(deltaEffMaxDmg, levelCost, baselineEff, 50);
            DrawEffectLabel(fxText);

            EditorGUILayout.EndHorizontal();
        }

        // ── 서브 모듈 행 (누적) ──
        if (data.subA != null || data.subB != null)
        {
            EditorGUILayout.Space(3);
            var lv4 = cum[3];
            int lv4Cost = costs[3];

            if (data.subA != null)
            {
                var s = Clone(lv4);
                if (data.subA.stats != null) s.Add(data.subA.stats);
                SubAnalysisRow("+A", s, lv4Cost + data.subA.cost,
                    data.subA.cost, cumExpDmg[3], baselineEff, scoutDps, scoutExpDmg, sp, AccumEffects(mains, 3, data.subA));
            }
            if (data.subB != null)
            {
                var s = Clone(lv4);
                if (data.subB.stats != null) s.Add(data.subB.stats);
                SubAnalysisRow("+B", s, lv4Cost + data.subB.cost,
                    data.subB.cost, cumExpDmg[3], baselineEff, scoutDps, scoutExpDmg, sp, AccumEffects(mains, 3, data.subB));
            }
            if (data.subA != null && data.subB != null)
            {
                var s = Clone(lv4);
                if (data.subA.stats != null) s.Add(data.subA.stats);
                if (data.subB.stats != null) s.Add(data.subB.stats);
                int abCost = data.subA.cost + data.subB.cost;
                SubAnalysisRow("+AB", s, lv4Cost + abCost,
                    abCost, cumExpDmg[3], baselineEff, scoutDps, scoutExpDmg, sp, AccumEffects(mains, 3, data.subA, data.subB));
            }
        }

        // ── 비고 ──
        EditorGUILayout.Space(3);

        var fxAccum = AccumEffects(mains, 3);
        if (fxAccum != "-")
            EditorGUILayout.LabelField($"  상태이상 (Lv4 누적): {fxAccum}", EditorStyles.miniLabel);

        // ── 경고 ──
        bool missingAreaTargets = sp && cum[3].splashRadius > 0 && cum[3].areaTargets <= 0;
        if (missingAreaTargets)
        {
            EditorGUILayout.Space(3);
            EditorGUILayout.HelpBox(
                "스플래시 타워에 적중수(areaTargets)가 설정되지 않았습니다.\n" +
                "게임에서 범위 내 모든 적에게 무제한 피해가 적용됩니다. 적중수를 설정하세요.",
                MessageType.Warning);
        }

        EditorGUILayout.Space(3);
        EditorGUILayout.HelpBox(
            "DPS = 관통수 / 주기\n" +
            "기대피해 = DPS × 적중수 × 체류시간    (체류시간 = 2 × 사거리 / 적속도)\n" +
            "투자효율 = (Δ기대피해 / 비용) / 기준    (< 1.0 → 신규 배치보다 비효율적)",
            MessageType.Info);
    }

    private void SubAnalysisRow(string label, TowerStats s, int totalCost,
        int subCost, float lv4ExpDmg, float baselineEff, float scoutDps, float scoutExpDmg, bool sp, string fxText)
    {
        float dps = CalcDps(s);
        float dwell = enemySpeed > 0 ? 2f * s.attackRange / enemySpeed : 0;
        float effMaxDmg = EffDps(s, dps) * dwell;

        EditorGUILayout.BeginHorizontal();
        C(label, 28); C($"{s.pierceCount}", 30);
        C($"{s.attackInterval:F2}", 36); C($"{s.attackRange:F1}", 38);
        if (sp)
        {
            C(s.splashRadius > 0 ? $"{s.splashRadius:F1}" : "-", 32);
            C(s.areaTargets > 0 ? $"{s.areaTargets}" : "-", 30);
        }
        C($"{totalCost}", 42); C($"{dps:F2}", 40);
        C(float.IsNaN(effMaxDmg) ? "ERR" : $"{effMaxDmg:F1}", 48);
        C(scoutDps > 0 ? $"{dps / scoutDps:F2}\ubc30" : "-", 50);
        C(VsExpDmg(effMaxDmg, scoutExpDmg, true), 50);
        float deltaEffMaxDmg = effMaxDmg - lv4ExpDmg;
        DrawVsBaseline(deltaEffMaxDmg, subCost, baselineEff, 50);
        DrawEffectLabel(fxText);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawVsBaseline(float deltaExpDmg, int cost, float baseEff, float width)
    {
        if (float.IsNaN(deltaExpDmg))
        {
            C("ERR", width);
            return;
        }
        if (deltaExpDmg > 0.01f)
        {
            float marginal = cost > 0 ? deltaExpDmg / cost : 0;
            float ratio = baseEff > 0 ? marginal / baseEff : 0;
            bool ok = ratio >= 1.0f;
            string color = ok ? "#4CAF50" : "#F44336";
            string icon = ok ? "\u2713" : "\u2717";
            GUILayout.Label($"<color={color}>{icon}{ratio:F2}\ubc30</color>", RichStyle, GUILayout.Width(width));
        }
        else
        {
            C("-", width);
        }
    }

    private void DrawEffectLabel(string fxText)
    {
        bool hasFx = !string.IsNullOrEmpty(fxText);
        GUILayout.Label(hasFx ? $"<color=#2196F3>{fxText}</color>" : "-", RichStyle);
    }

    private static void GetScoutTowerInfo(out float scoutDps, out float scoutRange, out int scoutCost)
    {
        scoutDps = 0f; scoutRange = 0f; scoutCost = 0;
        const string ScoutTowerName = "Scout";
        var guids = AssetDatabase.FindAssets("t:TowerUpgradeData");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var td = AssetDatabase.LoadAssetAtPath<TowerUpgradeData>(path);
            if (td == null || td.towerName != ScoutTowerName) continue;
            if (td.main1 == null || td.main1.stats == null || td.main1.cost <= 0) return;
            scoutDps = CalcDps(td.main1.stats);
            scoutRange = td.main1.stats.attackRange;
            scoutCost = td.main1.cost;
            return;
        }
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

    private void DrawSingleSubRow(string label, UpgradeLevel sub,
        TowerStats lv4, float lv4Dps, float lv4ExpDmg, float baselineEff, float scoutDps, float scoutExpDmg, bool sp)
    {
        if (sub == null) return;
        var ls = sub.stats;
        string fxText = LevelEffects(sub);

        EditorGUILayout.BeginHorizontal();
        C($"+{label}", 28);
        if (ls != null)
        {
            C(ls.pierceCount != 0 ? FormatDelta(ls.pierceCount, false) : "-", 30);
            C(ls.attackInterval != 0 ? FormatDelta(ls.attackInterval, false) : "-", 36);
            C(ls.attackRange != 0 ? FormatDelta(ls.attackRange, false) : "-", 38);
            if (sp)
            {
                C(ls.splashRadius != 0 ? FormatDelta(ls.splashRadius, false) : "-", 32);
                C(ls.areaTargets != 0 ? FormatDelta(ls.areaTargets, false) : "-", 30);
            }
        }
        else
        {
            C("-", 30); C("-", 36); C("-", 38);
            if (sp) { C("-", 32); C("-", 30); }
        }
        C($"{sub.cost}", 42);

        var merged = Clone(lv4);
        if (ls != null) merged.Add(ls);
        float mergedDps = CalcDps(merged);
        float deltaDps = mergedDps - lv4Dps;
        float dwell = enemySpeed > 0 ? 2f * merged.attackRange / enemySpeed : 0;
        float mergedExpDmg = EffDps(merged, mergedDps) * dwell;
        float deltaEffMaxDmg = mergedExpDmg - lv4ExpDmg;

        C(FormatDelta(deltaDps, false, "F2"), 40);
        C(FormatDelta(deltaEffMaxDmg, false, "F1"), 48);
        C(VsDps(deltaDps, scoutDps, false), 50);
        C(VsExpDmg(deltaEffMaxDmg, scoutExpDmg, false), 50);
        DrawVsBaseline(deltaEffMaxDmg, sub.cost, baselineEff, 50);
        DrawEffectLabel(fxText);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTableHeader(bool sp)
    {
        EditorGUILayout.BeginHorizontal();
        H("Lv", 28); H("관통", 30); H("주기", 36); H("사거리", 38);
        if (sp) { H("스플", 32); H("적중", 30); }
        H("비용", 42); H("DPS", 40); H("기대피해", 48);
        H("vs DPS", 50); H("vs 기대", 50); H("투자효율", 50);
        GUILayout.Label("효과", HeaderStyle);
        EditorGUILayout.EndHorizontal();
    }

    // ─── 유틸리티 ────────────────────────────────────

    private void H(string text, float w) => GUILayout.Label(text, HeaderStyle, GUILayout.Width(w));
    private void C(string text, float w) => GUILayout.Label(text, CellStyle, GUILayout.Width(w));

    private static string FormatDelta(float v, bool isBase, string fmt = "G4")
    {
        if (float.IsNaN(v)) return "ERR";
        if (isBase) return v.ToString(fmt);
        if (v == 0) return "-";
        return v > 0 ? $"+{v.ToString(fmt)}" : v.ToString(fmt);
    }

    private static string FormatDelta(int v, bool isBase)
    {
        if (isBase) return $"{v}";
        if (v == 0) return "-";
        return v > 0 ? $"+{v}" : $"{v}";
    }

    private static string VsDps(float value, float scoutDps, bool isBase)
    {
        if (scoutDps <= 0) return "-";
        float ratio = value / scoutDps;
        string s = FormatDelta(ratio, isBase, "F2");
        return s == "-" ? "-" : s + "\ubc30";
    }

    private static string VsExpDmg(float value, float scoutExpDmg, bool isBase)
    {
        if (float.IsNaN(value) || scoutExpDmg <= 0) return "-";
        float ratio = value / scoutExpDmg;
        string s = FormatDelta(ratio, isBase, "F2");
        return s == "-" ? "-" : s + "\ubc30";
    }

    private static float CalcDps(TowerStats s) =>
        s.attackInterval > 0 ? s.pierceCount / s.attackInterval : 0;

    private static float EffDps(TowerStats s, float dps)
    {
        if (s.splashRadius > 0 && s.areaTargets > 0) return dps * s.areaTargets;
        if (s.splashRadius > 0) return float.NaN;
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
        attackInterval = s.attackInterval,
        attackRange = s.attackRange,
        splashRadius = s.splashRadius,
        pierceCount = s.pierceCount,
        areaTargets = s.areaTargets
    };

    private static string AccumEffects(UpgradeLevel[] mains, int upTo, params UpgradeLevel[] subs)
    {
        int count = System.Enum.GetValues(typeof(StatusEffectType)).Length;
        float[] dur = new float[count];

        for (int i = 0; i <= upTo && i < mains.Length; i++)
        {
            if (mains[i]?.statusEffects == null) continue;
            foreach (var e in mains[i].statusEffects)
                dur[(int)e.type] += e.duration;
        }

        foreach (var sub in subs)
        {
            if (sub?.statusEffects == null) continue;
            foreach (var e in sub.statusEffects)
                dur[(int)e.type] += e.duration;
        }

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
