using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    [SerializeField] private Text energyText;
    [SerializeField] private Text livesText;
    [SerializeField] private Text waveText;
    [SerializeField] private Text stateText;
    [SerializeField] private Button startWaveButton;

    private void Start()
    {
        ResourceManager.Instance.OnEnergyChanged += UpdateEnergy;
        ResourceManager.Instance.OnLivesChanged += UpdateLives;
        GameManager.Instance.OnWaveChanged += UpdateWave;
        GameManager.Instance.OnStateChanged += UpdateState;

        startWaveButton.onClick.AddListener(OnStartWaveClicked);

        UpdateEnergy(ResourceManager.Instance.Energy);
        UpdateLives(ResourceManager.Instance.Lives);
        UpdateWave(0);
        UpdateState(GameState.Prepare);
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
    }

    private void OnStartWaveClicked()
    {
        GameManager.Instance.StartWave();
    }
}
