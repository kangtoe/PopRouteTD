using System;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    public int Energy { get; private set; }
    public int Lives { get; private set; }

    public event Action<int> OnEnergyChanged;
    public event Action<int> OnLivesChanged;
    public event Action OnLivesZero;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Initialize(int startEnergy, int startLives)
    {
        Energy = startEnergy;
        Lives = startLives;
        OnEnergyChanged?.Invoke(Energy);
        OnLivesChanged?.Invoke(Lives);
    }

    public bool SpendEnergy(int amount)
    {
        if (Energy < amount) return false;
        Energy -= amount;
        OnEnergyChanged?.Invoke(Energy);
        return true;
    }

    public void AddEnergy(int amount)
    {
        Energy += amount;
        OnEnergyChanged?.Invoke(Energy);
    }

    public void LoseLife(int amount)
    {
        Lives = Mathf.Max(0, Lives - amount);
        OnLivesChanged?.Invoke(Lives);
        if (Lives <= 0)
        {
            OnLivesZero?.Invoke();
        }
    }
}
