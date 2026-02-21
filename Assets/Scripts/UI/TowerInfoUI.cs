using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TowerInfoUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Text towerNameText;

    [Header("Targeting")]
    [SerializeField] private Button firstButton;
    [FormerlySerializedAs("weakButton")]
    [SerializeField] private Button lastButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button strongButton;

    [Header("Sell")]
    [SerializeField] private Button sellButton;
    [SerializeField] private Text sellButtonText;

    [Header("Upgrade")]
    [SerializeField] private TowerButtonUI upgradeButtonPrefab;
    [SerializeField] private Transform upgradeButtonParent;

    private TowerButtonUI mainUpgradeButton;
    private TowerButtonUI subAButton;
    private TowerButtonUI subBButton;

    private Tower selectedTower;
    private bool isPreview;
    private Image[] priorityImages;
    private static readonly Color normalColor = Color.white;
    private static readonly Color selectedColor = new Color(0.4f, 0.8f, 1f);

    private Sprite mainUpgradeIcon;
    private Sprite subAIcon;
    private Sprite subBIcon;

    private void Start()
    {
        priorityImages = new[]
        {
            firstButton.GetComponent<Image>(),
            lastButton.GetComponent<Image>(),
            closeButton.GetComponent<Image>(),
            strongButton.GetComponent<Image>()
        };

        InputManager.Instance.OnTowerClicked += Show;
        InputManager.Instance.OnEmptyClicked += Hide;
        InputManager.Instance.OnDragStarted += ShowPreview;
        InputManager.Instance.OnDragEnded += HidePreview;
        ResourceManager.Instance.OnGoldChanged += OnGoldChanged;

        firstButton.onClick.AddListener(() => OnPriorityClicked(TargetPriority.First));
        lastButton.onClick.AddListener(() => OnPriorityClicked(TargetPriority.Last));
        closeButton.onClick.AddListener(() => OnPriorityClicked(TargetPriority.Close));
        strongButton.onClick.AddListener(() => OnPriorityClicked(TargetPriority.Strong));
        sellButton.onClick.AddListener(OnSellClicked);

        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        CleanupIcons();
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnTowerClicked -= Show;
            InputManager.Instance.OnEmptyClicked -= Hide;
            InputManager.Instance.OnDragStarted -= ShowPreview;
            InputManager.Instance.OnDragEnded -= HidePreview;
        }
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnGoldChanged -= OnGoldChanged;
    }

    private void Show(Tower tower)
    {
        if (selectedTower != null) selectedTower.ShowRange(false);

        selectedTower = tower;
        TowerSelectUI.Instance?.Hide();
        panel.SetActive(true);

        UpdateDisplay();
        selectedTower.ShowRange(true);
    }

    public void Hide()
    {
        isPreview = false;
        ClearUpgradeButtons();
        CleanupIcons();
        if (selectedTower != null)
        {
            selectedTower.ShowRange(false);
            selectedTower = null;
        }
        panel.SetActive(false);
        TowerSelectUI.Instance?.Show();
    }

    private void ShowPreview(Tower prefabTower)
    {
        if (selectedTower != null) selectedTower.ShowRange(false);
        selectedTower = null;
        isPreview = true;

        var data = prefabTower.UpgradeData;
        if (data == null) return;

        towerNameText.text = data.towerName;
    }

    private void HidePreview()
    {
        if (!isPreview) return;
        isPreview = false;
        TowerSelectUI.Instance?.Show();
    }

    public void RefreshAfterUpgrade()
    {
        if (selectedTower != null)
            UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        var tower = selectedTower;

        towerNameText.text = tower.TowerName;

        SetPriorityButtonsVisible(true);

        // 판매
        sellButton.gameObject.SetActive(true);
        sellButtonText.text = $"Sell (+{tower.SellRefund})";

        // 업그레이드 아이콘 생성
        GenerateUpgradeIcons();

        // 업그레이드 버튼 동적 생성 및 갱신
        CreateUpgradeButtons();
        UpdateMainButton();
        UpdateSubButtons();

        // 우선순위 하이라이트
        UpdatePriorityHighlight();

        // 사거리 표시 갱신
        tower.ShowRange(true);
    }

    private void OnGoldChanged(int _)
    {
        if (isPreview || selectedTower == null || !panel.activeSelf) return;
        UpdateMainButton();
        UpdateSubButtons();
    }

    private void CreateUpgradeButtons()
    {
        ClearUpgradeButtons();

        mainUpgradeButton = Instantiate(upgradeButtonPrefab, upgradeButtonParent);
        mainUpgradeButton.Button.onClick.AddListener(OnUpgradeClicked);

        var subA = selectedTower.GetSubAInfo();
        var subB = selectedTower.GetSubBInfo();
        if (subA != null && subB != null)
        {
            subAButton = Instantiate(upgradeButtonPrefab, upgradeButtonParent);
            subAButton.Button.onClick.AddListener(() => OnSubClicked(UpgradeTrack.A));

            subBButton = Instantiate(upgradeButtonPrefab, upgradeButtonParent);
            subBButton.Button.onClick.AddListener(() => OnSubClicked(UpgradeTrack.B));
        }
    }

    private void ClearUpgradeButtons()
    {
        if (mainUpgradeButton != null) { Destroy(mainUpgradeButton.gameObject); mainUpgradeButton = null; }
        if (subAButton != null) { Destroy(subAButton.gameObject); subAButton = null; }
        if (subBButton != null) { Destroy(subBButton.gameObject); subBButton = null; }
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
            mainUpgradeButton.SetAvailable("Upgrade", nextInfo.cost, nextInfo.description, mainUpgradeIcon);
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
            subAButton.SetSelected(subA.levelName, subA.description, subAIcon);
        else
            subAButton.SetAvailable(subA.levelName, subA.cost, subA.description, subAIcon);

        if (selectedTower.HasSubB)
            subBButton.SetSelected(subB.levelName, subB.description, subBIcon);
        else
            subBButton.SetAvailable(subB.levelName, subB.cost, subB.description, subBIcon);
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
        Hide();
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

    private void GenerateUpgradeIcons()
    {
        CleanupIcons();

        if (selectedTower.CanUpgradeMain)
            mainUpgradeIcon = TowerIconGenerator.GenerateUpgradeIcon(selectedTower, selectedTower.MainLevel + 1);

        if (!selectedTower.HasSubA)
            subAIcon = TowerIconGenerator.GenerateSubIcon(selectedTower, UpgradeTrack.A);

        if (!selectedTower.HasSubB)
            subBIcon = TowerIconGenerator.GenerateSubIcon(selectedTower, UpgradeTrack.B);
    }

    private void CleanupIcons()
    {
        DestroyIcon(ref mainUpgradeIcon);
        DestroyIcon(ref subAIcon);
        DestroyIcon(ref subBIcon);
    }

    private void DestroyIcon(ref Sprite icon)
    {
        if (icon == null) return;
        Destroy(icon.texture);
        Destroy(icon);
        icon = null;
    }

    private void SetPriorityButtonsVisible(bool visible)
    {
        firstButton.gameObject.SetActive(visible);
        lastButton.gameObject.SetActive(visible);
        closeButton.gameObject.SetActive(visible);
        strongButton.gameObject.SetActive(visible);
    }
}
