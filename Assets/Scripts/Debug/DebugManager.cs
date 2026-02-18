using System;
using UnityEngine;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
public class DebugManager : MonoBehaviour
{
    [SerializeField] private WaypointPath waypointPath;
    [SerializeField] private WaveTable waveTable;

    [SerializeField] bool showPanel;
    private BalloonLayer selectedLayer = BalloonLayer.Red;
    private EnemyVariant selectedVariant = EnemyVariant.Normal;
    private string jumpWaveInput = "1";
    private string energyInput = GameConstants.StartEnergy.ToString();

    private readonly string[] layerNames = Enum.GetNames(typeof(BalloonLayer));
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
                selectedLayer = (BalloonLayer)i;
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
            if (BalloonSpawner.Instance != null && waypointPath != null)
                BalloonSpawner.Instance.SpawnBalloon(selectedLayer, waypointPath, selectedVariant);
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
        energyInput = GUILayout.TextField(energyInput, GUILayout.Width(50));
        if (GUILayout.Button("Set E"))
        {
            if (int.TryParse(energyInput, out int energy) && energy >= 0)
            {
                int diff = energy - ResourceManager.Instance.Energy;
                ResourceManager.Instance.AddEnergy(diff);
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        int cumulative = CalcCumulativeEnergy(GameManager.Instance.CurrentWave);
        GUILayout.BeginHorizontal();
        GUILayout.Label($"~W{GameManager.Instance.CurrentWave}: {cumulative}E");
        if (GUILayout.Button("Auto"))
        {
            energyInput = cumulative.ToString();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        if (GUILayout.Button("Clear Enemies"))
        {
            GameManager.Instance.DebugClearEnemies();
        }

        GUILayout.EndArea();
    }

    private int CalcCumulativeEnergy(int upToWave)
    {
        int total = GameConstants.StartEnergy;
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

        int perBalloon = 0;
        for (int l = (int)group.layer; l >= (int)BalloonLayer.Red; l--)
        {
            int reward = GameConstants.BaseLayerReward;
            if (enhanced) reward *= GameConstants.EnhancedMultiplier;
            perBalloon += reward;
        }
        if (shielded) perBalloon += GameConstants.DefaultShieldHp;

        return perBalloon * group.count;
    }
}
#endif
