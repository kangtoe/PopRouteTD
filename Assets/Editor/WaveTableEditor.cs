using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(WaveTable))]
public class WaveTableEditor : Editor
{
    private SerializedProperty wavesProp;
    private ReorderableList waveList;
    private int expandedWave = -1;
    private bool showChart = true;
    private static GUIStyle statsStyle;
    private static GUIStyle miniRight, miniCenter;

    private static readonly string[] LayerShort =
        { "?", "R", "O", "Y", "G", "B", "I", "P" };

    private static readonly string[] VariantShort =
        { "", " Sh", " En", " ES" };

    private void OnEnable()
    {
        wavesProp = serializedObject.FindProperty("waves");
        waveList = new ReorderableList(serializedObject, wavesProp, true, true, true, true);
        waveList.drawHeaderCallback = rect =>
            EditorGUI.LabelField(rect, $"Waves ({wavesProp.arraySize})");
        waveList.elementHeightCallback = GetWaveHeight;
        waveList.drawElementCallback = DrawWave;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDifficultyChart();
        EditorGUILayout.Space(4);
        waveList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawDifficultyChart()
    {
        showChart = EditorGUILayout.Foldout(showChart, "난이도 차트", true);
        if (!showChart) return;

        int n = wavesProp.arraySize;
        if (n == 0) return;

        if (miniRight == null)
        {
            miniRight = new GUIStyle(EditorStyles.miniLabel)
                { alignment = TextAnchor.MiddleRight };
            miniCenter = new GUIStyle(EditorStyles.miniLabel)
                { alignment = TextAnchor.UpperCenter };
        }

        // gather stats
        int[] dmgs = new int[n];
        float[] dps = new float[n];
        int maxDmg = 1;
        float maxDps = 1f;
        for (int i = 0; i < n; i++)
        {
            var groups = wavesProp.GetArrayElementAtIndex(i)
                .FindPropertyRelative("groups");
            CalcWaveStats(groups, out int d, out _, out float t);
            dmgs[i] = d;
            dps[i] = t > 0 ? d / t : 0f;
            if (d > maxDmg) maxDmg = d;
            if (dps[i] > maxDps) maxDps = dps[i];
        }

        // layout
        const float chartH = 130f, yLabelW = 50f, xLabelH = 14f, legendH = 14f;
        Rect area = GUILayoutUtility.GetRect(0, legendH + chartH + xLabelH + 8,
            GUILayout.ExpandWidth(true));
        Rect chart = new Rect(area.x + yLabelW, area.y + legendH + 2,
            area.width - yLabelW - 10, chartH);

        // legend
        float lx = chart.x;
        EditorGUI.DrawRect(new Rect(lx, area.y + 3, 10, 8),
            new Color(0.3f, 0.7f, 0.4f));
        EditorGUI.LabelField(new Rect(lx + 12, area.y, 50, legendH),
            "Total HP", EditorStyles.miniLabel);
        lx += 70;
        EditorGUI.DrawRect(new Rect(lx, area.y + 3, 10, 8),
            new Color(1f, 0.55f, 0f));
        EditorGUI.LabelField(new Rect(lx + 12, area.y, 30, legendH),
            "DPS", EditorStyles.miniLabel);

        // background
        EditorGUI.DrawRect(chart, new Color(0.15f, 0.15f, 0.15f));

        // Y grid + labels
        for (int g = 1; g <= 4; g++)
        {
            float gy = chart.yMax - chart.height * g / 4f;
            EditorGUI.DrawRect(new Rect(chart.x, gy, chart.width, 1),
                new Color(0.3f, 0.3f, 0.3f, 0.5f));
            EditorGUI.LabelField(
                new Rect(area.x, gy - 7, yLabelW - 4, 14),
                (maxDmg * g / 4).ToString(), miniRight);
        }

        // bars
        float barW = chart.width / n;
        for (int i = 0; i < n; i++)
        {
            float ratio = (float)dmgs[i] / maxDmg;
            float barH = ratio * chart.height;
            Color c = ratio < 0.5f
                ? Color.Lerp(new Color(0.3f, 0.7f, 0.4f),
                    new Color(0.9f, 0.8f, 0.2f), ratio * 2f)
                : Color.Lerp(new Color(0.9f, 0.8f, 0.2f),
                    new Color(0.9f, 0.25f, 0.2f), (ratio - 0.5f) * 2f);
            EditorGUI.DrawRect(new Rect(
                chart.x + i * barW + 1, chart.yMax - barH,
                Mathf.Max(barW - 2, 1), barH), c);
        }

        // DPS line
        Handles.color = new Color(1f, 0.55f, 0f, 0.9f);
        var points = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            points[i] = new Vector3(
                chart.x + (i + 0.5f) * barW,
                chart.yMax - (dps[i] / maxDps) * chart.height, 0);
        }
        Handles.DrawAAPolyLine(2.5f, points);

        // X axis labels
        float xLabelY = chart.yMax + 1;
        int step = n <= 10 ? 1 : 5;
        for (int i = 0; i < n; i++)
        {
            if (i > 0 && (i + 1) % step != 0 && i != n - 1) continue;
            EditorGUI.LabelField(new Rect(
                chart.x + (i + 0.5f) * barW - 12, xLabelY, 24, xLabelH),
                (i + 1).ToString(), miniCenter);
        }
    }

    private float GetWaveHeight(int index)
    {
        if (index != expandedWave) return EditorGUIUtility.singleLineHeight + 2;
        var wave = wavesProp.GetArrayElementAtIndex(index);
        var groups = wave.FindPropertyRelative("groups");
        // header + each group (4 fields) + add button
        return EditorGUIUtility.singleLineHeight * (2 + groups.arraySize) + 6;
    }

    private void DrawWave(Rect rect, int index, bool isActive, bool isFocused)
    {
        var wave = wavesProp.GetArrayElementAtIndex(index);
        var groups = wave.FindPropertyRelative("groups");
        float line = EditorGUIUtility.singleLineHeight;
        var headerRect = new Rect(rect.x + 10, rect.y, rect.width - 10, line);

        string summary = BuildSummary(groups);
        int totalCount = GetTotalCount(groups);
        CalcWaveStats(groups, out int totalDmg, out int totalGold, out float totalTime);
        int clearReward = GameConstants.GetWaveClearReward(index + 1);
        int waveTotal = totalGold + clearReward;

        int cumGold = GameConstants.StartGold + waveTotal;
        for (int i = 0; i < index; i++)
        {
            var prevGroups = wavesProp.GetArrayElementAtIndex(i)
                .FindPropertyRelative("groups");
            CalcWaveStats(prevGroups, out _, out int prevGold, out _);
            cumGold += prevGold + GameConstants.GetWaveClearReward(i + 1);
        }

        // 왼쪽: 웨이브 정보
        bool wasExpanded = (expandedWave == index);
        bool expanded = EditorGUI.Foldout(headerRect, wasExpanded,
            $"Wave {index + 1}  {summary}", true);

        // 오른쪽: 피해량/골드 (고정폭 폰트, 5자리 정렬)
        if (statsStyle == null)
        {
            statsStyle = new GUIStyle(EditorStyles.label)
            {
                font = Font.CreateDynamicFontFromOSFont("Consolas", 11),
                alignment = TextAnchor.MiddleRight
            };
        }
        float dps = totalTime > 0 ? totalDmg / totalTime : 0f;
        float statsWidth = 480;
        var statsRect = new Rect(
            headerRect.xMax - statsWidth, headerRect.y, statsWidth, line);
        EditorGUI.LabelField(statsRect,
            string.Format("{0,3}체  |  DMG {1,5}  |  {2,5:F1}s  |  DPS {3,5:F1}  |  GD {4,5} + {5,3}  ({6,6})",
                totalCount, totalDmg, totalTime, dps, totalGold, clearReward, cumGold),
            statsStyle);

        if (expanded != wasExpanded)
        {
            expandedWave = expanded ? index : -1;
            waveList.elementHeightCallback = GetWaveHeight;
        }

        if (!expanded) return;

        // ── Groups ──
        float y = rect.y + line + 2;
        float fieldW = (rect.width - 30) / 4;

        for (int i = 0; i < groups.arraySize; i++)
        {
            var group = groups.GetArrayElementAtIndex(i);
            float x = rect.x + 12;

            EditorGUI.PropertyField(new Rect(x, y, fieldW, line),
                group.FindPropertyRelative("layer"), GUIContent.none);
            x += fieldW + 2;
            EditorGUI.PropertyField(new Rect(x, y, fieldW, line),
                group.FindPropertyRelative("variant"), GUIContent.none);
            x += fieldW + 2;
            EditorGUI.PropertyField(new Rect(x, y, fieldW * 0.6f, line),
                group.FindPropertyRelative("count"), GUIContent.none);
            x += fieldW * 0.6f + 2;
            EditorGUI.PropertyField(new Rect(x, y, fieldW * 0.6f, line),
                group.FindPropertyRelative("interval"), GUIContent.none);
            x += fieldW * 0.6f + 2;

            if (GUI.Button(new Rect(x, y, 18, line), "−"))
            {
                groups.DeleteArrayElementAtIndex(i);
                break;
            }
            y += line;
        }

        if (GUI.Button(new Rect(rect.x + 12, y, 60, line), "+ 그룹"))
        {
            groups.InsertArrayElementAtIndex(groups.arraySize);
        }
    }

    private static string BuildSummary(SerializedProperty groups)
    {
        if (groups.arraySize == 0) return "(비어있음)";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < groups.arraySize; i++)
        {
            if (i > 0) sb.Append(", ");
            var g = groups.GetArrayElementAtIndex(i);
            int layer = g.FindPropertyRelative("layer").enumValueIndex;
            int variant = g.FindPropertyRelative("variant").enumValueIndex;
            int count = g.FindPropertyRelative("count").intValue;

            if (layer >= 0 && layer < LayerShort.Length)
                sb.Append(LayerShort[layer]);
            if (variant >= 0 && variant < VariantShort.Length)
                sb.Append(VariantShort[variant]);
            sb.Append('×').Append(count);
        }
        return sb.ToString();
    }

    private static int GetTotalCount(SerializedProperty groups)
    {
        int total = 0;
        for (int i = 0; i < groups.arraySize; i++)
            total += groups.GetArrayElementAtIndex(i)
                .FindPropertyRelative("count").intValue;
        return total;
    }

    private static void CalcWaveStats(SerializedProperty groups,
        out int totalDmg, out int totalGold, out float totalTime)
    {
        totalDmg = 0;
        totalGold = 0;
        totalTime = 0f;
        for (int i = 0; i < groups.arraySize; i++)
        {
            var g = groups.GetArrayElementAtIndex(i);
            int layer = g.FindPropertyRelative("layer").enumValueIndex;
            int variant = g.FindPropertyRelative("variant").enumValueIndex;
            int count = g.FindPropertyRelative("count").intValue;
            float interval = g.FindPropertyRelative("interval").floatValue;

            bool isEnhanced = variant == (int)EnemyVariant.Enhanced
                || variant == (int)EnemyVariant.EnhancedShielded;
            bool hasShield = variant == (int)EnemyVariant.Shielded
                || variant == (int)EnemyVariant.EnhancedShielded;

            int hpPerLayer = isEnhanced ? GameConstants.EnhancedMultiplier : 1;
            int dmg = layer * hpPerLayer + (hasShield ? GameConstants.DefaultShieldHp : 0);

            int gold = hasShield ? GameConstants.ShieldReward : 0;
            gold += layer * GameConstants.BaseLayerReward;

            totalDmg += dmg * count;
            totalGold += gold * count;
            totalTime += count * interval;
        }
    }
}
