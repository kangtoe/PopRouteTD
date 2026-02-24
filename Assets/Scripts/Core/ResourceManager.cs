using System;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    public int Gold { get; private set; }
    public int Lives { get; private set; }

    public event Action<int> OnGoldChanged;
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

    public void Initialize(int startGold, int startLives)
    {
        Gold = startGold;
        Lives = startLives;
        OnGoldChanged?.Invoke(Gold);
        OnLivesChanged?.Invoke(Lives);
    }

    public bool SpendGold(int amount)
    {
        if (Gold < amount) return false;
        Gold -= amount;
        OnGoldChanged?.Invoke(Gold);
        return true;
    }

    public void AddGold(int amount)
    {
        Gold += amount;
        OnGoldChanged?.Invoke(Gold);
    }

    public void LoseLife(int amount)
    {
        if (Lives <= 0) return;

        Lives = Mathf.Max(0, Lives - amount);
        SoundManager.Instance.PlayLifeLost();
        OnLivesChanged?.Invoke(Lives);
        if (Lives <= 0)
        {
            OnLivesZero?.Invoke();
        }
    }
}
