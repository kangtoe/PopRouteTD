using UnityEngine;
using UnityEngine.UI;

public class TowerInfoUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Text towerNameText;
    [SerializeField] private Text towerStatsText;

    [Header("Targeting")]
    [SerializeField] private Button firstButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button weakButton;
    [SerializeField] private Button strongButton;

    [Header("Sell")]
    [SerializeField] private Button sellButton;
    [SerializeField] private Text sellButtonText;

    private Tower selectedTower;
    private Image[] priorityImages;
    private static readonly Color normalColor = Color.white;
    private static readonly Color selectedColor = new Color(0.4f, 0.8f, 1f);

    private void Start()
    {
        priorityImages = new[]
        {
            firstButton.GetComponent<Image>(),
            closeButton.GetComponent<Image>(),
            weakButton.GetComponent<Image>(),
            strongButton.GetComponent<Image>()
        };

        InputManager.Instance.OnTowerClicked += Show;
        InputManager.Instance.OnEmptyClicked += Hide;

        firstButton.onClick.AddListener(() => OnPriorityClicked(TargetPriority.First));
        closeButton.onClick.AddListener(() => OnPriorityClicked(TargetPriority.Close));
        weakButton.onClick.AddListener(() => OnPriorityClicked(TargetPriority.Weak));
        strongButton.onClick.AddListener(() => OnPriorityClicked(TargetPriority.Strong));
        sellButton.onClick.AddListener(OnSellClicked);

        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnTowerClicked -= Show;
            InputManager.Instance.OnEmptyClicked -= Hide;
        }
    }

    private void Show(Tower tower)
    {
        if (selectedTower != null) selectedTower.ShowRange(false);

        selectedTower = tower;
        panel.SetActive(true);

        towerNameText.text = tower.TowerName;

        if (tower.IsAttacker)
        {
            var stats = $"ATK {tower.AttackDamage}  SPD {tower.AttackInterval:0.#}s  RNG {tower.AttackRange:0.#}";
            if (tower.SplashRadius > 0f) stats += $"  SPLASH {tower.SplashRadius:0.#}";
            towerStatsText.text = stats;
            SetPriorityButtonsVisible(true);
        }
        else
        {
            towerStatsText.text = "Energy Generator";
            SetPriorityButtonsVisible(false);
        }

        sellButtonText.text = $"Sell (+{tower.SellRefund})";
        selectedTower.ShowRange(true);
        UpdatePriorityHighlight();
    }

    public void Hide()
    {
        if (selectedTower != null)
        {
            selectedTower.ShowRange(false);
            selectedTower = null;
        }
        panel.SetActive(false);
    }

    private void OnPriorityClicked(TargetPriority priority)
    {
        if (selectedTower == null) return;
        selectedTower.SetTargetPriority(priority);
        UpdatePriorityHighlight();
    }

    private void OnSellClicked()
    {
        if (selectedTower == null) return;
        selectedTower.Sell();
        selectedTower = null;
        panel.SetActive(false);
    }

    private void UpdatePriorityHighlight()
    {
        if (selectedTower == null) return;
        int current = (int)selectedTower.Priority;
        for (int i = 0; i < priorityImages.Length; i++)
        {
            priorityImages[i].color = i == current ? selectedColor : normalColor;
        }
    }

    private void SetPriorityButtonsVisible(bool visible)
    {
        firstButton.gameObject.SetActive(visible);
        closeButton.gameObject.SetActive(visible);
        weakButton.gameObject.SetActive(visible);
        strongButton.gameObject.SetActive(visible);
    }
}
