using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private WaveManager waveManager;

    public GameState CurrentState { get; private set; }
    public int CurrentWave { get; private set; }

    public event Action<GameState> OnStateChanged;
    public event Action<int> OnWaveChanged;

    private int activeEnemyCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        ResourceManager.Instance.Initialize(GameConstants.StartEnergy, GameConstants.StartLives);
        ResourceManager.Instance.OnLivesZero += HandleGameOver;

        Balloon.OnBalloonDestroyed += OnBalloonRemoved;
        Balloon.OnBalloonReachedBase += OnBalloonRemoved;

        CurrentWave = 0;
        SetState(GameState.Prepare);
    }

    private void OnDestroy()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnLivesZero -= HandleGameOver;

        Balloon.OnBalloonDestroyed -= OnBalloonRemoved;
        Balloon.OnBalloonReachedBase -= OnBalloonRemoved;
    }

    public void StartWave()
    {
        if (CurrentState != GameState.Prepare) return;

        CurrentWave++;
        activeEnemyCount = 0;
        OnWaveChanged?.Invoke(CurrentWave);
        SetState(GameState.Wave);
        waveManager.StartWave(CurrentWave);
    }

    public void RegisterEnemy()
    {
        activeEnemyCount++;
    }

    private void OnBalloonRemoved(Balloon balloon)
    {
        activeEnemyCount--;
        CheckWaveComplete();
    }

    public void CheckWaveComplete()
    {
        if (activeEnemyCount <= 0 && waveManager.IsSpawningComplete && CurrentState == GameState.Wave)
        {
            HandleWaveCleared();
        }
    }

    private void HandleWaveCleared()
    {
        SetState(GameState.Clear);
        // 짧은 딜레이 후 준비 단계로 전환
        Invoke(nameof(TransitionToPrepare), 1f);
    }

    private void TransitionToPrepare()
    {
        if (CurrentState == GameState.Clear)
        {
            SetState(GameState.Prepare);
        }
    }

    private void HandleGameOver()
    {
        if (CurrentState == GameState.GameOver) return;
        waveManager.StopSpawning();
        SetState(GameState.GameOver);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void SetState(GameState newState)
    {
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }
}
