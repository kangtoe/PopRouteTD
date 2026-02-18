using System;
using UnityEngine;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
public class DebugManager : MonoBehaviour
{
    [SerializeField] private WaypointPath waypointPath;
    [SerializeField] private WaveTable waveTable;

    [SerializeField] bool showPanel;
    private EnemyLayer selectedLayer = EnemyLayer.Red;
    private EnemyVariant selectedVariant = EnemyVariant.Normal;
    private string jumpWaveInput = "1";
    private string goldInput = GameConstants.StartGold.ToString();

    private readonly string[] layerNames = Enum.GetNames(typeof(EnemyLayer));
    private readonly string[] variantNames = Enum.GetNames(typeof(EnemyVariant));

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote))
            showPanel = !showPanel;
    }

    private void OnGUI()
    {
        if (!showPanel) return;

        float sw = Screen.width;
        GUILayout.BeginArea(new Rect(sw - 210, 10, 200, 400), "Debug", GUI.skin.window);

        GUILayout.Label("Layer");
        int layerIndex = (int)selectedLayer;
        // None(0)은 건너뛰고 1부터 표시
        for (int i = 1; i < layerNames.Length; i++)
        {
            bool selected = layerIndex == i;
            if (GUILayout.Toggle(selected, layerNames[i], GUI.skin.button) && !selected)
                selectedLayer = (EnemyLayer)i;
        }

        GUILayout.Space(8);

        GUILayout.Label("Variant");
        int variantIndex = (int)selectedVariant;
        for (int i = 0; i < variantNames.Length; i++)
        {
            bool selected = variantIndex == i;
            if (GUILayout.Toggle(selected, variantNames[i], GUI.skin.button) && !selected)
                selectedVariant = (EnemyVariant)i;
        }

        GUILayout.Space(8);

        if (GUILayout.Button("Spawn"))
        {
            if (EnemySpawner.Instance != null && waypointPath != null)
                EnemySpawner.Instance.SpawnEnemy(selectedLayer, waypointPath, selectedVariant);
        }

        GUILayout.EndArea();

        // Wave Jump
        GUILayout.BeginArea(new Rect(sw - 380, 10, 160, 190), "Wave", GUI.skin.window);

        GUILayout.BeginHorizontal();
        jumpWaveInput = GUILayout.TextField(jumpWaveInput, GUILayout.Width(50));
        if (GUILayout.Button("Jump"))
        {
            if (int.TryParse(jumpWaveInput, out int wave) && wave > 0)
                GameManager.Instance.DebugJumpToWave(wave);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        GUILayout.BeginHorizontal();
        goldInput = GUILayout.TextField(goldInput, GUILayout.Width(50));
        if (GUILayout.Button("Set G"))
        {
            if (int.TryParse(goldInput, out int gold) && gold >= 0)
            {
                int diff = gold - ResourceManager.Instance.Gold;
                ResourceManager.Instance.AddGold(diff);
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        int cumulative = CalcCumulativeGold(GameManager.Instance.CurrentWave);
        GUILayout.BeginHorizontal();
        GUILayout.Label($"~W{GameManager.Instance.CurrentWave}: {cumulative}G");
        if (GUILayout.Button("Auto"))
        {
            goldInput = cumulative.ToString();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        if (GUILayout.Button("Clear Enemies"))
        {
            GameManager.Instance.DebugClearEnemies();
        }

        GUILayout.EndArea();
    }

    private int CalcCumulativeGold(int upToWave)
    {
        int total = GameConstants.StartGold;
        if (waveTable == null) return total;

        for (int w = 1; w <= upToWave; w++)
        {
            WaveData wave = waveTable.GetWave(w);
            if (wave == null) continue;
            foreach (var group in wave.groups)
                total += CalcGroupReward(group);
            total += GameConstants.GetWaveClearReward(w);
        }
        return total;
    }

    private static int CalcGroupReward(SpawnGroupData group)
    {
        bool enhanced = group.variant is EnemyVariant.Enhanced or EnemyVariant.EnhancedShielded;
        bool shielded = group.variant is EnemyVariant.Shielded or EnemyVariant.EnhancedShielded;

        int perEnemy = 0;
        for (int l = (int)group.layer; l >= (int)EnemyLayer.Red; l--)
        {
            int reward = GameConstants.BaseLayerReward;
            if (enhanced) reward *= GameConstants.EnhancedMultiplier;
            perEnemy += reward;
        }
        if (shielded) perEnemy += GameConstants.DefaultShieldHp;

        return perEnemy * group.count;
    }
}
#endif
