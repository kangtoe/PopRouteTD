using UnityEngine;

public class TowerEnergyGenerator : MonoBehaviour
{
    private float perTick;
    private float tickInterval;
    private float tickTimer;
    private bool initialized;

    public void Initialize(float energyPerTick, float interval)
    {
        perTick = energyPerTick;
        tickInterval = interval;
        tickTimer = tickInterval;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized) return;

        tickTimer -= Time.deltaTime;
        if (tickTimer <= 0f)
        {
            ResourceManager.Instance.AddEnergy((int)perTick);
            tickTimer = tickInterval;
        }
    }
}
