using UnityEngine;
using UnityEngine.UI;

public class SpeedControlUI : MonoBehaviour
{
    [SerializeField] private Button[] speedButtons;

    private static readonly float[] Speeds = { 1f, 2f, 4f };
    private static readonly string[] Labels = { "x1", "x2", "x4" };

    private readonly Color normalColor = new(0.3f, 0.3f, 0.4f);
    private readonly Color activeColor = new(0.2f, 0.6f, 0.3f);

    private int currentIndex;

    private void Start()
    {
        for (int i = 0; i < speedButtons.Length; i++)
        {
            int idx = i;
            speedButtons[i].onClick.AddListener(() => SetSpeed(idx));
        }

        GameManager.Instance.OnStateChanged += OnStateChanged;
        UpdateButtons();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnStateChanged;
    }

    private void OnStateChanged(GameState state)
    {
        if (state == GameState.GameOver)
            SetSpeed(0);
    }

    private void SetSpeed(int index)
    {
        currentIndex = index;
        Time.timeScale = Speeds[index];
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        for (int i = 0; i < speedButtons.Length; i++)
        {
            var img = speedButtons[i].GetComponent<Image>();
            if (img != null)
                img.color = i == currentIndex ? activeColor : normalColor;
        }
    }
}
