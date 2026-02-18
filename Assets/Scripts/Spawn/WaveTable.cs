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
        if (index < 0 || index >= waves.Count) return null;
        return waves[index];
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
