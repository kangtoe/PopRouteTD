using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    [SerializeField] private Text energyText;
    [SerializeField] private Text livesText;
    [SerializeField] private Text waveText;
    [SerializeField] private Text stateText;
    [SerializeField] private Button startWaveButton;

    [Header("준비 단계 타이머")]
    [SerializeField] private Text prepareTimerText;
    [SerializeField] private Text earlyStartBonusText;

    private void Start()
    {
        ResourceManager.Instance.OnEnergyChanged += UpdateEnergy;
        ResourceManager.Instance.OnLivesChanged += UpdateLives;
        GameManager.Instance.OnWaveChanged += UpdateWave;
        GameManager.Instance.OnStateChanged += UpdateState;
        GameManager.Instance.OnPrepareTimerChanged += UpdatePrepareTimer;

        startWaveButton.onClick.AddListener(OnStartWaveClicked);

        UpdateEnergy(ResourceManager.Instance.Energy);
        UpdateLives(ResourceManager.Instance.Lives);
        UpdateWave(0);
        UpdateState(GameState.Prepare);

        // 첫 준비 단계에서는 타이머/보너스 숨김
        if (prepareTimerText != null) prepareTimerText.gameObject.SetActive(false);
        if (earlyStartBonusText != null) earlyStartBonusText.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnEnergyChanged -= UpdateEnergy;
            ResourceManager.Instance.OnLivesChanged -= UpdateLives;
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWaveChanged -= UpdateWave;
            GameManager.Instance.OnStateChanged -= UpdateState;
            GameManager.Instance.OnPrepareTimerChanged -= UpdatePrepareTimer;
        }
    }

    private void UpdateEnergy(int value)
    {
        if (energyText != null) energyText.text = $"Energy: {value}";
    }

    private void UpdateLives(int value)
    {
        if (livesText != null) livesText.text = $"Lives: {value}";
    }

    private void UpdateWave(int value)
    {
        if (waveText != null) waveText.text = $"Wave: {value}";
    }

    private void UpdateState(GameState state)
    {
        if (stateText != null) stateText.text = state.ToString();
        if (startWaveButton != null) startWaveButton.gameObject.SetActive(state == GameState.Prepare);

        if (state != GameState.Prepare)
        {
            if (prepareTimerText != null) prepareTimerText.gameObject.SetActive(false);
            if (earlyStartBonusText != null) earlyStartBonusText.gameObject.SetActive(false);
        }
    }

    private void UpdatePrepareTimer(float remaining, float total)
    {
        int seconds = Mathf.CeilToInt(Mathf.Max(0f, remaining));

        if (prepareTimerText != null)
        {
            prepareTimerText.gameObject.SetActive(true);
            prepareTimerText.text = $"{seconds}s";
        }

        if (earlyStartBonusText != null)
        {
            int bonus = Mathf.RoundToInt(
                (Mathf.Max(0f, remaining) / total) * GameConstants.EarlyStartBonusMax);
            earlyStartBonusText.gameObject.SetActive(bonus > 0);
            earlyStartBonusText.text = $"Bonus: +{bonus}";
        }
    }

    private void OnStartWaveClicked()
    {
        GameManager.Instance.StartWave();
    }
}
