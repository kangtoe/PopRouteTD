using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(WaveTable))]
public class WaveTableEditor : Editor
{
    private SerializedProperty wavesProp;
    private ReorderableList waveList;
    private int expandedWave = -1;
    private static GUIStyle statsStyle;

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
        waveList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
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
        CalcWaveStats(groups, out int totalDmg, out int totalEnergy, out float totalTime);
        int clearReward = GameConstants.GetWaveClearReward(index + 1);
        int waveTotal = totalEnergy + clearReward;

        int cumEnergy = GameConstants.StartEnergy + waveTotal;
        for (int i = 0; i < index; i++)
        {
            var prevGroups = wavesProp.GetArrayElementAtIndex(i)
                .FindPropertyRelative("groups");
            CalcWaveStats(prevGroups, out _, out int prevEnergy, out _);
            cumEnergy += prevEnergy + GameConstants.GetWaveClearReward(i + 1);
        }

        // 왼쪽: 웨이브 정보
        bool wasExpanded = (expandedWave == index);
        bool expanded = EditorGUI.Foldout(headerRect, wasExpanded,
            $"Wave {index + 1}  {summary}", true);

        // 오른쪽: 피해량/에너지 (고정폭 폰트, 5자리 정렬)
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
            string.Format("{0,3}체  |  DMG {1,5}  |  {2,5:F1}s  |  DPS {3,5:F1}  |  EN {4,5} + {5,3}  ({6,6})",
                totalCount, totalDmg, totalTime, dps, totalEnergy, clearReward, cumEnergy),
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
        out int totalDmg, out int totalEnergy, out float totalTime)
    {
        totalDmg = 0;
        totalEnergy = 0;
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

            int energy = hasShield ? GameConstants.DefaultShieldHp : 0;
            for (int l = layer; l >= 1; l--)
            {
                int reward = GameConstants.BaseLayerReward;
                if (isEnhanced) reward *= GameConstants.EnhancedMultiplier;
                energy += reward;
            }

            totalDmg += dmg * count;
            totalEnergy += energy * count;
            totalTime += count * interval;
        }
    }
}
