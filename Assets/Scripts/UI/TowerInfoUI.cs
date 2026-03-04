using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TowerInfoUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
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
    [SerializeField] private TowerButtonUI mainUpgradeButton;
    [SerializeField] private TowerButtonUI subAButton;
    [SerializeField] private TowerButtonUI subBButton;
    [SerializeField] private TowerButtonUI detailCardPrefab;

    private Tower selectedTower;
    private bool isPreview;
    private Image[] priorityImages;
    private static readonly Color normalColor = new(0.3f, 0.3f, 0.4f);
    private static readonly Color activeColor = new(0.2f, 0.6f, 0.3f);

    private Sprite mainUpgradeIcon;
    private Sprite subAIcon;
    private Sprite subBIcon;

    private TowerButtonUI detailCard;
    private bool eventsInitialized;

    // Hold-to-confirm
    private const float HoldDuration = 0.5f;
    private TowerButtonUI holdButton;
    private float holdTimer;
    private Action holdAction;

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

        InitUpgradeEvents();
        HideUpgradeButtons();
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

    private void Update()
    {
        if (holdButton == null) return;

        holdTimer += Time.deltaTime;
        holdButton.SetHoldProgress(holdTimer / HoldDuration);

        if (holdTimer >= HoldDuration)
        {
            var action = holdAction;
            ResetHold();
            action?.Invoke();
        }
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
        ResetHold();
        HideDetailCard();
        HideUpgradeButtons();
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
        ResetHold();
        HideDetailCard();
        var tower = selectedTower;

        SetPriorityButtonsVisible(true);

        // 판매
        sellButton.gameObject.SetActive(true);
        sellButtonText.text = $"Sell (+{tower.SellRefund})";

        // 업그레이드 아이콘 생성
        GenerateUpgradeIcons();

        // 업그레이드 버튼 갱신
        mainUpgradeButton.gameObject.SetActive(true);
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

    private void InitUpgradeEvents()
    {
        if (eventsInitialized) return;
        eventsInitialized = true;

        AddUpgradeEvents(mainUpgradeButton, () =>
        {
            if (selectedTower != null && selectedTower.UpgradeMain())
                UpdateDisplay();
        });
        AddHoverEvents(mainUpgradeButton, 0);

        AddUpgradeEvents(subAButton, () =>
        {
            if (selectedTower != null && selectedTower.SelectSub(UpgradeTrack.A))
                UpdateDisplay();
        });
        AddHoverEvents(subAButton, 1);

        AddUpgradeEvents(subBButton, () =>
        {
            if (selectedTower != null && selectedTower.SelectSub(UpgradeTrack.B))
                UpdateDisplay();
        });
        AddHoverEvents(subBButton, 2);
    }

    private void HideUpgradeButtons()
    {
        ResetHold();
        mainUpgradeButton.gameObject.SetActive(false);
        subAButton.gameObject.SetActive(false);
        subBButton.gameObject.SetActive(false);
    }

    private void AddUpgradeEvents(TowerButtonUI btn, Action action)
    {
        var trigger = btn.gameObject.GetComponent<EventTrigger>() ?? btn.gameObject.AddComponent<EventTrigger>();

        var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        down.callback.AddListener(_ => StartHold(btn, action));
        trigger.triggers.Add(down);

        var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        up.callback.AddListener(_ => ResetHold());
        trigger.triggers.Add(up);
    }

    /// <param name="hoverIndex">0=Main, 1=SubA, 2=SubB</param>
    private void AddHoverEvents(TowerButtonUI btn, int hoverIndex)
    {
        var trigger = btn.gameObject.GetComponent<EventTrigger>() ?? btn.gameObject.AddComponent<EventTrigger>();

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => OnButtonHover(hoverIndex));
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => HideDetailCard());
        trigger.triggers.Add(exit);
    }

    private void OnButtonHover(int hoverIndex)
    {
        if (selectedTower == null) return;

        string label;
        string desc;
        TowerButtonUI anchor;

        switch (hoverIndex)
        {
            case 0:
                anchor = mainUpgradeButton;
                var mainInfo = selectedTower.CanUpgradeMain
                    ? selectedTower.GetNextMainInfo()
                    : selectedTower.GetCurrentMainInfo();
                if (mainInfo == null) return;
                label = mainInfo.upgradeName;
                desc = mainInfo.description;
                break;
            case 1:
                anchor = subAButton;
                var subAInfo = selectedTower.GetSubAInfo();
                if (subAInfo == null) return;
                label = subAInfo.upgradeName;
                desc = subAInfo.description;
                break;
            case 2:
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
        else
            subAButton.SetAvailable(subA.upgradeName, subA.cost, subA.description, subAIcon);

        if (selectedTower.HasSubB)
            subBButton.SetSelected(subB.upgradeName, subB.description, subBIcon);
        else
            subBButton.SetAvailable(subB.upgradeName, subB.cost, subB.description, subBIcon);
    }

    private void StartHold(TowerButtonUI btn, Action action)
    {
        if (!btn.Button.interactable) return;
        holdButton = btn;
        holdTimer = 0f;
        holdAction = action;
    }

    private void ResetHold()
    {
        if (holdButton != null)
            holdButton.SetHoldProgress(0f);
        holdButton = null;
        holdTimer = 0f;
        holdAction = null;
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
            detailCard = DetailCardHelper.Create(detailCardPrefab, panel.transform);
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
