using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WaveTable", menuName = "Data/Wave Table")]
public class WaveTable : ScriptableObject
{
    [SerializeField] private List<WaveData> waves = new();

    public int WaveCount => waves.Count;

    public WaveData GetWave(int waveNumber)
    {
        int index = waveNumber - 1;
        if (index >= 0 && index < waves.Count) return waves[index];
        return GenerateWave(waveNumber);
    }

    private WaveData GenerateWave(int waveNumber)
    {
        int offset = waveNumber - waves.Count;
        float budget = GameConstants.WaveBaseBudget
            * Mathf.Pow(GameConstants.WaveScalingRate, offset);
        int pattern = (offset - 1) % 3;
        int tier = (offset - 1) / 6;

        var wave = new WaveData();

        switch (pattern)
        {
            case 0: // Rush - 대량의 향상 적
            {
                var layer = ClampLayer(2 + tier);
                AddGroup(wave, layer, EnemyVariant.Enhanced, budget);
                break;
            }
            case 1: // Mixed - 다양한 향상 구성
            {
                var low = ClampLayer(3 + tier);
                var high = ClampLayer(5 + tier);
                var highVariant = tier < 3 ? EnemyVariant.Enhanced
                    : EnemyVariant.EnhancedShielded;
                AddGroup(wave, low, EnemyVariant.Enhanced, budget * 0.6f);
                AddGroup(wave, high, highVariant, budget * 0.4f);
                break;
            }
            case 2: // Elite - 소수의 극강 적
            {
                var layer = ClampLayer(5 + tier);
                AddGroup(wave, layer, EnemyVariant.EnhancedShielded, budget);
                break;
            }
        }

        // DPS 정규화: 목표 웨이브 시간 기반 interval 역산
        int totalCount = 0;
        foreach (var g in wave.groups) totalCount += g.count;
        float interval = Mathf.Max(GameConstants.WaveMinInterval,
            GameConstants.WaveBaseDuration / totalCount);
        foreach (var g in wave.groups) g.interval = interval;

        return wave;
    }

    private static EnemyLayer ClampLayer(int value)
        => (EnemyLayer)Mathf.Clamp(value, 1, 7);

    private static int CalcEnemyCost(EnemyLayer layer, EnemyVariant variant)
    {
        bool enhanced = variant == EnemyVariant.Enhanced
            || variant == EnemyVariant.EnhancedShielded;
        bool shielded = variant == EnemyVariant.Shielded
            || variant == EnemyVariant.EnhancedShielded;
        return (int)layer * (enhanced ? GameConstants.EnhancedMultiplier : 1)
            + (shielded ? GameConstants.DefaultShieldHp : 0);
    }

    private static void AddGroup(WaveData wave, EnemyLayer layer, EnemyVariant variant,
        float budget)
    {
        int cost = CalcEnemyCost(layer, variant);
        int count = Mathf.Max(1, Mathf.RoundToInt(budget / cost));
        wave.groups.Add(new SpawnGroupData
        {
            layer = layer, variant = variant, count = count
        });
    }
}

[Serializable]
public class WaveData
{
    public List<SpawnGroupData> groups = new();
}

[Serializable]
public class SpawnGroupData
{
    public EnemyLayer layer;
    public EnemyVariant variant;
    public int count;
    public float interval = 0.5f;
}
