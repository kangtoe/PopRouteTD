using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    [SerializeField] private Text goldText;
    [SerializeField] private Text livesText;
    [SerializeField] private Text waveText;
    [SerializeField] private Text stateText;
    [SerializeField] private Button startWaveButton;
    private Text startWaveButtonText;


    [Header("준비 단계 타이머")]
    [SerializeField] private Image prepareTimerFill;
    [SerializeField] private Text earlyStartBonusText;

    private void Start()
    {
        ResourceManager.Instance.OnGoldChanged += UpdateGold;
        ResourceManager.Instance.OnLivesChanged += UpdateLives;
        GameManager.Instance.OnWaveChanged += UpdateWave;
        GameManager.Instance.OnStateChanged += UpdateState;
        GameManager.Instance.OnPrepareTimerChanged += UpdatePrepareTimer;

        startWaveButtonText = startWaveButton.GetComponentInChildren<Text>();
        startWaveButton.onClick.AddListener(OnStartWaveClicked);

        UpdateGold(ResourceManager.Instance.Gold);
        UpdateLives(ResourceManager.Instance.Lives);
        UpdateWave(0);
        UpdateState(GameState.Prepare);

        // 첫 준비 단계에서는 타이머/보너스 숨김
        if (prepareTimerFill != null) prepareTimerFill.fillAmount = 0f;
        if (earlyStartBonusText != null) earlyStartBonusText.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnGoldChanged -= UpdateGold;
            ResourceManager.Instance.OnLivesChanged -= UpdateLives;
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWaveChanged -= UpdateWave;
            GameManager.Instance.OnStateChanged -= UpdateState;
            GameManager.Instance.OnPrepareTimerChanged -= UpdatePrepareTimer;
        }
    }

    private void UpdateGold(int value)
    {
        if (goldText != null) goldText.text = $"{value}";
    }

    private void UpdateLives(int value)
    {
        if (livesText != null) livesText.text = $"{value}";
    }

    private void UpdateWave(int value)
    {
        if (waveText != null) waveText.text = $"{value}";
    }

    private void UpdateState(GameState state)
    {
        if (stateText != null) stateText.text = state.ToString();
        if (startWaveButton != null)
        {
            bool canStart = state == GameState.Prepare;
            startWaveButton.interactable = canStart;
            startWaveButton.image.color = canStart ? GameConstants.UIColorActive : GameConstants.UIColorNormal;
            if (startWaveButtonText != null)
                startWaveButtonText.text = canStart ? "Start Wave!" : "On Wave";
        }

        if (state != GameState.Prepare)
        {
            if (prepareTimerFill != null) prepareTimerFill.fillAmount = 0f;
            if (earlyStartBonusText != null) earlyStartBonusText.gameObject.SetActive(false);
        }
    }

    private void UpdatePrepareTimer(float remaining, float total)
    {
        if (prepareTimerFill != null)
        {
            prepareTimerFill.fillAmount = 1f - remaining / total;
        }

        if (earlyStartBonusText != null)
        {
            int bonus = Mathf.RoundToInt(
                (Mathf.Max(0f, remaining) / total) * GameConstants.EarlyStartBonusMax);
            earlyStartBonusText.gameObject.SetActive(bonus > 0);
            earlyStartBonusText.text = $"+{bonus}";
        }
    }

    private void OnStartWaveClicked()
    {
        GameManager.Instance.StartWave();
    }
}
