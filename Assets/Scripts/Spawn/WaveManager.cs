using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [SerializeField] private WaypointPath waypointPath;

    [Header("스폰 설정")]
    [SerializeField] private int baseCount = 10;
    [SerializeField] private float baseInterval = 0.8f;
    [SerializeField] private float minInterval = 0.2f;

    public bool IsSpawningComplete { get; private set; }

    private Coroutine spawnCoroutine;

    public void StartWave(int waveNumber)
    {
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(SpawnWaveCoroutine(waveNumber));
    }

    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnWaveCoroutine(int wave)
    {
        IsSpawningComplete = false;

        var groups = GenerateWave(wave);
        foreach (var group in groups)
        {
            for (int i = 0; i < group.count; i++)
            {
                BalloonSpawner.Instance.SpawnBalloon(group.layer, waypointPath);
                yield return new WaitForSeconds(group.interval);
            }
        }

        IsSpawningComplete = true;
        GameManager.Instance.OnSpawningComplete();
    }

    /// <summary>웨이브 번호 기반 자동 생성 (설계 문서 난이도 스케일링)</summary>
    private List<SpawnEntry> GenerateWave(int wave)
    {
        var entries = new List<SpawnEntry>();
        BalloonLayer maxLayer = GetMaxLayer(wave);
        float interval = Mathf.Max(minInterval, baseInterval - wave * 0.03f);
        int totalCount = baseCount + wave * 2;

        if (maxLayer >= BalloonLayer.Orange)
        {
            int highCount = Mathf.Max(2, wave / 2);
            entries.Add(new SpawnEntry(maxLayer, highCount, interval));
            totalCount -= highCount;
        }

        BalloonLayer fillLayer = maxLayer > BalloonLayer.Red ? maxLayer - 1 : BalloonLayer.Red;
        if (totalCount > 0)
        {
            entries.Add(new SpawnEntry(fillLayer, totalCount, interval));
        }

        return entries;
    }

    private static BalloonLayer GetMaxLayer(int wave)
    {
        if (wave >= 23) return BalloonLayer.Purple;
        if (wave >= 17) return BalloonLayer.Indigo;
        if (wave >= 12) return BalloonLayer.Blue;
        if (wave >= 8) return BalloonLayer.Green;
        if (wave >= 5) return BalloonLayer.Yellow;
        if (wave >= 3) return BalloonLayer.Orange;
        return BalloonLayer.Red;
    }

    private struct SpawnEntry
    {
        public BalloonLayer layer;
        public int count;
        public float interval;

        public SpawnEntry(BalloonLayer layer, int count, float interval)
        {
            this.layer = layer;
            this.count = count;
            this.interval = interval;
        }
    }
}
