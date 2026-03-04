using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TowerInfoUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Text towerNameText;
    [SerializeField] private Text descriptionText;

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

    private enum PendingUpgrade { None, Main, SubA, SubB }

    private Tower selectedTower;
    private bool isPreview;
    private PendingUpgrade pendingUpgrade;
    private Image[] priorityImages;
    private static readonly Color normalColor = new(0.3f, 0.3f, 0.4f);
    private static readonly Color activeColor = new(0.2f, 0.6f, 0.3f);

    private Sprite mainUpgradeIcon;
    private Sprite subAIcon;
    private Sprite subBIcon;

    private TowerButtonUI detailCard;

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
        if (detailCard != null) Destroy(detailCard.gameObject);
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
        HideDetailCard();
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
        HideDetailCard();
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

        // 설명 텍스트
        UpdateDescription();

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
        AddHoverEvents(mainUpgradeButton, PendingUpgrade.Main);

        var subA = selectedTower.GetSubAInfo();
        var subB = selectedTower.GetSubBInfo();
        if (subA != null && subB != null)
        {
            subAButton = Instantiate(upgradeButtonPrefab, upgradeButtonParent);
            subAButton.Button.onClick.AddListener(() => OnSubClicked(UpgradeTrack.A));
            AddHoverEvents(subAButton, PendingUpgrade.SubA);

            subBButton = Instantiate(upgradeButtonPrefab, upgradeButtonParent);
            subBButton.Button.onClick.AddListener(() => OnSubClicked(UpgradeTrack.B));
            AddHoverEvents(subBButton, PendingUpgrade.SubB);
        }
    }

    private void AddHoverEvents(TowerButtonUI btn, PendingUpgrade type)
    {
        var trigger = btn.gameObject.AddComponent<EventTrigger>();

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => OnButtonHover(type));
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => { if (pendingUpgrade == PendingUpgrade.None) HideDetailCard(); });
        trigger.triggers.Add(exit);
    }

    private void OnButtonHover(PendingUpgrade type)
    {
        if (selectedTower == null) return;

        string label;
        string desc;
        TowerButtonUI anchor;

        switch (type)
        {
            case PendingUpgrade.Main:
                anchor = mainUpgradeButton;
                var mainInfo = selectedTower.CanUpgradeMain
                    ? selectedTower.GetNextMainInfo()
                    : selectedTower.GetCurrentMainInfo();
                if (mainInfo == null) return;
                label = mainInfo.upgradeName;
                desc = mainInfo.description;
                break;
            case PendingUpgrade.SubA:
                anchor = subAButton;
                var subAInfo = selectedTower.GetSubAInfo();
                if (subAInfo == null) return;
                label = subAInfo.upgradeName;
                desc = subAInfo.description;
                break;
            case PendingUpgrade.SubB:
                anchor = subBButton;
                var subBInfo = selectedTower.GetSubBInfo();
                if (subBInfo == null) return;
                label = subBInfo.upgradeName;
                desc = subBInfo.description;
                break;
            default:
                return;
        }

        ShowDetailCard(anchor, label, desc);
    }

    private void ClearUpgradeButtons()
    {
        pendingUpgrade = PendingUpgrade.None;
        if (mainUpgradeButton != null) { Destroy(mainUpgradeButton.gameObject); mainUpgradeButton = null; }
        if (subAButton != null) { Destroy(subAButton.gameObject); subAButton = null; }
        if (subBButton != null) { Destroy(subBButton.gameObject); subBButton = null; }
    }

    private void CancelPending()
    {
        if (pendingUpgrade == PendingUpgrade.None) return;
        pendingUpgrade = PendingUpgrade.None;
        HideDetailCard();
        UpdateMainButton();
        UpdateSubButtons();
        UpdateDescription();
    }

    private void UpdateDescription()
    {
        if (descriptionText == null) return;
        if (selectedTower == null) { descriptionText.text = ""; return; }

        descriptionText.text = pendingUpgrade switch
        {
            PendingUpgrade.Main => selectedTower.GetNextMainInfo()?.description ?? "",
            PendingUpgrade.SubA => selectedTower.GetSubAInfo()?.description ?? "",
            PendingUpgrade.SubB => selectedTower.GetSubBInfo()?.description ?? "",
            _ => selectedTower.UpgradeData != null ? selectedTower.UpgradeData.description : ""
        };
    }

    private void UpdateMainButton()
    {
        if (mainUpgradeButton == null) return;

        if (!selectedTower.CanUpgradeMain)
        {
            var currentInfo = selectedTower.GetCurrentMainInfo();
            mainUpgradeButton.SetMax(currentInfo?.upgradeName ?? "", mainUpgradeIcon);
            return;
        }

        var nextInfo = selectedTower.GetNextMainInfo();
        if (nextInfo == null) return;

        if (pendingUpgrade == PendingUpgrade.Main)
            mainUpgradeButton.SetConfirm(nextInfo.upgradeName, nextInfo.cost, nextInfo.description, mainUpgradeIcon);
        else
            mainUpgradeButton.SetAvailable(nextInfo.upgradeName, nextInfo.cost, nextInfo.description, mainUpgradeIcon);
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
            subAButton.SetSelected(subA.upgradeName, subA.description, subAIcon);
        else if (pendingUpgrade == PendingUpgrade.SubA)
            subAButton.SetConfirm(subA.upgradeName, subA.cost, subA.description, subAIcon);
        else
            subAButton.SetAvailable(subA.upgradeName, subA.cost, subA.description, subAIcon);

        if (selectedTower.HasSubB)
            subBButton.SetSelected(subB.upgradeName, subB.description, subBIcon);
        else if (pendingUpgrade == PendingUpgrade.SubB)
            subBButton.SetConfirm(subB.upgradeName, subB.cost, subB.description, subBIcon);
        else
            subBButton.SetAvailable(subB.upgradeName, subB.cost, subB.description, subBIcon);
    }

    private void OnUpgradeClicked()
    {
        if (selectedTower == null) return;

        if (pendingUpgrade == PendingUpgrade.Main)
        {
            if (selectedTower.UpgradeMain())
                UpdateDisplay();
        }
        else
        {
            CancelPending();
            pendingUpgrade = PendingUpgrade.Main;
            var info = selectedTower.GetNextMainInfo();
            if (info != null)
            {
                mainUpgradeButton.SetConfirm(info.upgradeName, info.cost, info.description, mainUpgradeIcon);
                ShowDetailCard(mainUpgradeButton, info.upgradeName, info.description);
            }
            UpdateDescription();
        }
    }

    private void OnSubClicked(UpgradeTrack sub)
    {
        if (selectedTower == null) return;

        var track = sub == UpgradeTrack.A ? PendingUpgrade.SubA : PendingUpgrade.SubB;

        if (pendingUpgrade == track)
        {
            if (selectedTower.SelectSub(sub))
                UpdateDisplay();
        }
        else
        {
            CancelPending();
            pendingUpgrade = track;
            var btn = sub == UpgradeTrack.A ? subAButton : subBButton;
            var info = sub == UpgradeTrack.A ? selectedTower.GetSubAInfo() : selectedTower.GetSubBInfo();
            var icon = sub == UpgradeTrack.A ? subAIcon : subBIcon;
            if (info != null)
            {
                btn.SetConfirm(info.upgradeName, info.cost, info.description, icon);
                ShowDetailCard(btn, info.upgradeName, info.description);
            }
            UpdateDescription();
        }
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
            priorityImages[i].color = i == current ? activeColor : normalColor;
        }
    }

    private void GenerateUpgradeIcons()
    {
        CleanupIcons();

        if (selectedTower.CanUpgradeMain)
            mainUpgradeIcon = TowerIconGenerator.GenerateUpgradeIcon(selectedTower, selectedTower.MainLevel + 1);
        else
            mainUpgradeIcon = TowerIconGenerator.GenerateUpgradeIcon(selectedTower, selectedTower.MainLevel);

        subAIcon = TowerIconGenerator.GenerateSubIcon(selectedTower, UpgradeTrack.A);
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

    private void EnsureDetailCard()
    {
        if (detailCard == null)
            detailCard = DetailCardHelper.Create(upgradeButtonPrefab, panel.transform);
    }

    private void ShowDetailCard(TowerButtonUI anchor, string label, string desc)
    {
        EnsureDetailCard();
        DetailCardHelper.Show(detailCard, label, desc);
        DetailCardHelper.PositionLeftOf(
            detailCard.GetComponent<RectTransform>(),
            anchor.GetComponent<RectTransform>(),
            panel.GetComponent<RectTransform>());
    }

    private void HideDetailCard()
    {
        DetailCardHelper.Hide(detailCard);
    }
}
