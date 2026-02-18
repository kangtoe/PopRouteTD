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

    [Header("Upgrade")]
    [SerializeField] private UpgradeButtonUI mainUpgradeButton;
    [SerializeField] private UpgradeButtonUI subAButton;
    [SerializeField] private UpgradeButtonUI subBButton;

    private Tower selectedTower;
    private bool isPreview;
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
        InputManager.Instance.OnDragStarted += ShowPreview;
        InputManager.Instance.OnDragEnded += HidePreview;
        ResourceManager.Instance.OnEnergyChanged += OnEnergyChanged;

        firstButton.onClick.AddListener(() => OnPriorityClicked(TargetPriority.First));
        closeButton.onClick.AddListener(() => OnPriorityClicked(TargetPriority.Close));
        weakButton.onClick.AddListener(() => OnPriorityClicked(TargetPriority.Weak));
        strongButton.onClick.AddListener(() => OnPriorityClicked(TargetPriority.Strong));
        sellButton.onClick.AddListener(OnSellClicked);

        if (mainUpgradeButton != null)
            mainUpgradeButton.Button.onClick.AddListener(OnUpgradeClicked);
        if (subAButton != null)
            subAButton.Button.onClick.AddListener(() => OnSubClicked(UpgradeTrack.A));
        if (subBButton != null)
            subBButton.Button.onClick.AddListener(() => OnSubClicked(UpgradeTrack.B));

        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnTowerClicked -= Show;
            InputManager.Instance.OnEmptyClicked -= Hide;
            InputManager.Instance.OnDragStarted -= ShowPreview;
            InputManager.Instance.OnDragEnded -= HidePreview;
        }
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnEnergyChanged -= OnEnergyChanged;
    }

    private void Show(Tower tower)
    {
        if (selectedTower != null) selectedTower.ShowRange(false);

        selectedTower = tower;
        panel.SetActive(true);

        UpdateDisplay();
        selectedTower.ShowRange(true);
    }

    public void Hide()
    {
        isPreview = false;
        if (selectedTower != null)
        {
            selectedTower.ShowRange(false);
            selectedTower = null;
        }
        panel.SetActive(false);
    }

    private void ShowPreview(Tower prefabTower)
    {
        if (selectedTower != null) selectedTower.ShowRange(false);
        selectedTower = null;
        isPreview = true;
        panel.SetActive(true);

        var data = prefabTower.UpgradeData;
        if (data == null) return;

        // 이름 (레벨 표시 없음)
        towerNameText.text = data.towerName;

        // 기본 스탯
        var s = data.main1.stats;
        var stats = $"ATK {s.attackDamage}  SPD {s.attackInterval:0.#}s  RNG {s.attackRange:0.#}";
        if (s.splashRadius > 0f) stats += $"  SPLASH {s.splashRadius:0.#}";
        towerStatsText.text = stats;

        // 타겟 지정·판매 숨기기
        SetPriorityButtonsVisible(false);
        sellButton.gameObject.SetActive(false);

        // 업그레이드 버튼: 잠금 상태로 표시
        UpdatePreviewUpgradeButtons(prefabTower);
    }

    private void UpdatePreviewUpgradeButtons(Tower prefabTower)
    {
        if (mainUpgradeButton != null)
        {
            var nextInfo = prefabTower.GetNextMainInfo();
            if (nextInfo != null)
                mainUpgradeButton.SetLocked("Upgrade", nextInfo.description);
            else
                mainUpgradeButton.SetMax();
        }

        if (subAButton != null && subBButton != null)
        {
            var subA = prefabTower.GetSubAInfo();
            var subB = prefabTower.GetSubBInfo();
            bool hasData = subA != null && subB != null;

            subAButton.gameObject.SetActive(hasData);
            subBButton.gameObject.SetActive(hasData);

            if (hasData)
            {
                subAButton.SetLocked($"A: {subA.levelName}", subA.description);
                subBButton.SetLocked($"B: {subB.levelName}", subB.description);
            }
        }
    }

    private void HidePreview()
    {
        if (!isPreview) return;
        isPreview = false;
        panel.SetActive(false);
    }

    public void RefreshAfterUpgrade()
    {
        if (selectedTower != null)
            UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        var tower = selectedTower;

        // 타워 이름 + 주 모듈 레벨
        towerNameText.text = tower.CanUpgradeMain || tower.MainLevel > 1
            ? $"{tower.TowerName} Lv.{tower.MainLevel}"
            : tower.TowerName;

        // 스탯
        var stats = $"ATK {tower.AttackDamage}  SPD {tower.AttackInterval:0.#}s  RNG {tower.AttackRange:0.#}";
        if (tower.SplashRadius > 0f) stats += $"  SPLASH {tower.SplashRadius:0.#}";
        towerStatsText.text = stats;
        SetPriorityButtonsVisible(true);

        // 판매
        sellButton.gameObject.SetActive(true);
        sellButtonText.text = $"Sell (+{tower.SellRefund})";

        // 업그레이드 버튼
        UpdateMainButton();
        UpdateSubButtons();

        // 우선순위 하이라이트
        UpdatePriorityHighlight();

        // 사거리 표시 갱신
        tower.ShowRange(true);
    }

    private void OnEnergyChanged(int _)
    {
        if (isPreview || selectedTower == null || !panel.activeSelf) return;
        UpdateMainButton();
        UpdateSubButtons();
    }

    private void UpdateMainButton()
    {
        if (mainUpgradeButton == null) return;

        if (!selectedTower.CanUpgradeMain)
        {
            mainUpgradeButton.SetMax();
            return;
        }

        var nextInfo = selectedTower.GetNextMainInfo();
        if (nextInfo != null)
            mainUpgradeButton.SetAvailable("Upgrade", nextInfo.cost, nextInfo.description);
    }

    private void UpdateSubButtons()
    {
        if (subAButton == null || subBButton == null) return;

        var subA = selectedTower.GetSubAInfo();
        var subB = selectedTower.GetSubBInfo();
        bool hasUpgradeData = subA != null && subB != null;

        subAButton.gameObject.SetActive(hasUpgradeData);
        subBButton.gameObject.SetActive(hasUpgradeData);
        if (!hasUpgradeData) return;

        if (selectedTower.HasSubA)
            subAButton.SetSelected($"A: {subA.levelName}", subA.description);
        else
            subAButton.SetAvailable($"A: {subA.levelName}", subA.cost, subA.description);

        if (selectedTower.HasSubB)
            subBButton.SetSelected($"B: {subB.levelName}", subB.description);
        else
            subBButton.SetAvailable($"B: {subB.levelName}", subB.cost, subB.description);
    }

    private void OnUpgradeClicked()
    {
        if (selectedTower == null) return;

        if (selectedTower.UpgradeMain())
            UpdateDisplay();
    }

    private void OnSubClicked(UpgradeTrack sub)
    {
        if (selectedTower == null) return;

        if (selectedTower.SelectSub(sub))
            UpdateDisplay();
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
