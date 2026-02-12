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
    public event Action<float, float> OnPrepareTimerChanged;

    private int activeEnemyCount;
    private float postSpawnTimer;
    private bool postSpawnTimerActive;
    private float prepareTimer;
    private bool prepareCountdownActive;

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

    private void Update()
    {
        if (CurrentState == GameState.GameOver) return;

        // 스폰 종료 후 타임아웃: 시간 경과 시 준비 단계로 전환
        if (postSpawnTimerActive)
        {
            postSpawnTimer -= Time.deltaTime;
            if (postSpawnTimer <= 0f)
            {
                postSpawnTimerActive = false;
                TransitionToPrepare();
            }
        }

        // 준비 단계 카운트다운: 만료 시 자동 시작
        if (prepareCountdownActive)
        {
            prepareTimer -= Time.deltaTime;
            OnPrepareTimerChanged?.Invoke(prepareTimer, GameConstants.PrepareDuration);
            if (prepareTimer <= 0f)
            {
                prepareTimer = 0f;
                prepareCountdownActive = false;
                StartWave();
            }
        }
    }

    public void StartWave()
    {
        if (CurrentState != GameState.Prepare) return;

        // 조기 시작 보너스: 남은 시간에 비례
        if (prepareCountdownActive && prepareTimer > 0f)
        {
            int bonus = Mathf.RoundToInt(
                (prepareTimer / GameConstants.PrepareDuration) * GameConstants.EarlyStartBonusMax);
            if (bonus > 0)
            {
                ResourceManager.Instance.AddEnergy(bonus);
            }
        }
        prepareCountdownActive = false;

        CurrentWave++;
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
        TryCompleteWave();
    }

    /// <summary>WaveManager에서 스폰 완료 시 호출</summary>
    public void OnSpawningComplete()
    {
        if (!TryCompleteWave())
        {
            postSpawnTimer = GameConstants.PostSpawnTimeout;
            postSpawnTimerActive = true;
        }
    }

    private bool TryCompleteWave()
    {
        if (activeEnemyCount <= 0 && waveManager.IsSpawningComplete && CurrentState == GameState.Wave)
        {
            postSpawnTimerActive = false;
            TransitionToPrepare();
            return true;
        }
        return false;
    }

    private void TransitionToPrepare()
    {
        SetState(GameState.Prepare);
        prepareTimer = GameConstants.PrepareDuration;
        prepareCountdownActive = true;
        OnPrepareTimerChanged?.Invoke(prepareTimer, GameConstants.PrepareDuration);
    }

    private void HandleGameOver()
    {
        if (CurrentState == GameState.GameOver) return;
        postSpawnTimerActive = false;
        prepareCountdownActive = false;
        waveManager.StopSpawning();
        Time.timeScale = 1f;
        SetState(GameState.GameOver);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void SetState(GameState newState)
    {
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }
}
