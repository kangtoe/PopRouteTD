using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [SerializeField] private WaypointPath waypointPath;
    [SerializeField] private WaveTable waveTable;

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

    private IEnumerator SpawnWaveCoroutine(int waveNumber)
    {
        IsSpawningComplete = false;

        WaveData wave = waveTable.GetWave(waveNumber);
        if (wave != null)
        {
            foreach (var group in wave.groups)
            {
                for (int i = 0; i < group.count; i++)
                {
                    BalloonSpawner.Instance.SpawnBalloon(group.layer, waypointPath, group.variant);
                    yield return new WaitForSeconds(group.interval);
                }
            }
        }

        IsSpawningComplete = true;
        GameManager.Instance.OnSpawningComplete();
    }
}
