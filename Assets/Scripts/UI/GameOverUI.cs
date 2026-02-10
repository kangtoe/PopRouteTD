using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Text finalWaveText;
    [SerializeField] private Button restartButton;

    private void Start()
    {
        gameOverPanel.SetActive(false);
        GameManager.Instance.OnStateChanged += OnStateChanged;
        restartButton.onClick.AddListener(OnRestartClicked);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnStateChanged;
    }

    private void OnStateChanged(GameState state)
    {
        if (state == GameState.GameOver)
        {
            gameOverPanel.SetActive(true);
            if (finalWaveText != null)
                finalWaveText.text = $"Wave {GameManager.Instance.CurrentWave}";
        }
    }

    private void OnRestartClicked()
    {
        GameManager.Instance.RestartGame();
    }
}
