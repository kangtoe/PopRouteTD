using UnityEngine;

public class TowerEnergyGenerator : MonoBehaviour
{
    private int energyPerItem;
    private int itemCount;
    private bool initialized;
    private bool waveActive;

    public void Initialize(float energyPerTick, float interval)
    {
        energyPerItem = (int)energyPerTick;
        itemCount = (int)interval;
        initialized = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += OnGameStateChanged;
            if (GameManager.Instance.CurrentState == GameState.Wave)
                waveActive = true;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState newState)
    {
        if (!initialized) return;

        if (newState == GameState.Wave)
        {
            waveActive = true;
        }
        else if (newState == GameState.Prepare && waveActive)
        {
            waveActive = false;
            SpawnItems();
        }
    }

    private void SpawnItems()
    {
        if (ItemSpawner.Instance == null) return;

        for (int i = 0; i < itemCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 0.5f;
            Vector3 pos = transform.position + (Vector3)offset;
            ItemSpawner.Instance.SpawnItem(pos, energyPerItem);
        }
    }
}
