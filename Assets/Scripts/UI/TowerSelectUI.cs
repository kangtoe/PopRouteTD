using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TowerSelectUI : MonoBehaviour
{
    public static TowerSelectUI Instance { get; private set; }

    [SerializeField] private GameObject content;

    [Serializable]
    public struct TowerSlot
    {
        public TowerButtonUI button;
        public Tower prefab;
    }

    [Serializable]
    public struct BombSlot
    {
        public TowerButtonUI button;
        public Bomb prefab;
    }

    [Header("Towers")]
    [SerializeField] private TowerSlot towerSlot1;
    [SerializeField] private TowerSlot towerSlot2;
    [SerializeField] private TowerSlot towerSlot3;
    [SerializeField] private TowerSlot towerSlot4;
    [SerializeField] private TowerSlot towerSlot5;

    [Header("Bombs")]
    [SerializeField] private BombSlot bombSlot1;

    [Header("Detail")]
    [SerializeField] private TowerButtonUI detailCardPrefab;

    private TowerSlot[] towerSlots;
    private BombSlot[] bombSlots;

    private readonly List<Sprite> generatedIcons = new();
    private readonly List<(int cost, string label, Sprite icon, string desc)> towerData = new();
    private readonly List<(int cost, string label, Sprite icon, string desc)> bombData = new();

    private TowerButtonUI detailCard;
    private TowerButtonUI draggingButton;
    private bool eventsInitialized;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        towerSlots = new[] { towerSlot1, towerSlot2, towerSlot3, towerSlot4, towerSlot5 };
        bombSlots = new[] { bombSlot1 };
    }

    private void Start()
    {
        if (Instance != this) return;
        InitButtons();
        Show();
        ResourceManager.Instance.OnGoldChanged += OnGoldChanged;
        InputManager.Instance.OnDragEnded += ClearDraggingButton;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnGoldChanged -= OnGoldChanged;
        if (InputManager.Instance != null)
            InputManager.Instance.OnDragEnded -= ClearDraggingButton;

        if (detailCard != null) Destroy(detailCard.gameObject);

        foreach (var icon in generatedIcons)
        {
            if (icon == null) continue;
            Destroy(icon.texture);
            Destroy(icon);
        }
        generatedIcons.Clear();
    }

    public void Show()
    {
        content.SetActive(true);
    }

    public void Hide()
    {
        HideDetailCard();
        content.SetActive(false);
    }

    private void InitButtons()
    {
        if (eventsInitialized) return;
        eventsInitialized = true;

        for (int i = 0; i < towerSlots.Length; i++)
        {
            var slot = towerSlots[i];
            if (slot.button == null || slot.prefab == null) continue;
            var tower = slot.prefab;

            string label = tower.TowerName;
            string desc = tower.UpgradeData != null ? tower.UpgradeData.description : "";
            var icon = TowerIconGenerator.GenerateIcon(tower.gameObject);
            generatedIcons.Add(icon);
            slot.button.SetAvailable(label, tower.Cost, icon: icon);
            towerData.Add((tower.Cost, label, icon, desc));

            var prefabObj = tower.gameObject;
            var btn = slot.button;
            var trigger = btn.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener(_ =>
            {
                InputManager.Instance.BeginDrag(prefabObj);
                if (InputManager.Instance.IsDragging)
                    SetDraggingButton(btn);
            });
            trigger.triggers.Add(down);

            int idx = towerData.Count - 1;
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => ShowDetailCard(idx, false));
            trigger.triggers.Add(enter);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => HideDetailCard());
            trigger.triggers.Add(exit);
        }

        for (int i = 0; i < bombSlots.Length; i++)
        {
            var slot = bombSlots[i];
            if (slot.button == null || slot.prefab == null) continue;
            var bomb = slot.prefab;

            string label = bomb.BombName;
            string desc = bomb.Description;
            var icon = TowerIconGenerator.GenerateIcon(bomb.gameObject);
            generatedIcons.Add(icon);
            slot.button.SetAvailable(label, bomb.Cost, icon: icon);
            bombData.Add((bomb.Cost, label, icon, desc));

            var prefabObj = bomb.gameObject;
            var btn = slot.button;
            var trigger = btn.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener(_ =>
            {
                InputManager.Instance.BeginDrag(prefabObj);
                if (InputManager.Instance.IsDragging)
                    SetDraggingButton(btn);
            });
            trigger.triggers.Add(down);

            int idx = bombData.Count - 1;
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => ShowDetailCard(idx, true));
            trigger.triggers.Add(enter);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => HideDetailCard());
            trigger.triggers.Add(exit);
        }
    }

    private void OnGoldChanged(int _)
    {
        for (int i = 0; i < towerSlots.Length && i < towerData.Count; i++)
            towerSlots[i].button.SetAvailable(towerData[i].label, towerData[i].cost, icon: towerData[i].icon);
        for (int i = 0; i < bombSlots.Length && i < bombData.Count; i++)
            bombSlots[i].button.SetAvailable(bombData[i].label, bombData[i].cost, icon: bombData[i].icon);

        if (draggingButton != null)
            draggingButton.SetDragging(true);
    }

    private void SetDraggingButton(TowerButtonUI button)
    {
        draggingButton = button;
        button.SetDragging(true);
    }

    private void ClearDraggingButton()
    {
        if (draggingButton != null)
        {
            draggingButton.SetDragging(false);
            draggingButton = null;
        }
    }

    private void EnsureDetailCard()
    {
        if (detailCard == null)
            detailCard = DetailCardHelper.Create(detailCardPrefab, content.transform);
    }

    private void ShowDetailCard(int index, bool isBomb)
    {
        var dataList = isBomb ? bombData : towerData;
        if (index < 0 || index >= dataList.Count) return;
        var (_, label, _, desc) = dataList[index];
        if (string.IsNullOrEmpty(desc)) return;

        var anchorButton = isBomb ? bombSlots[index].button : towerSlots[index].button;
        EnsureDetailCard();
        DetailCardHelper.Show(detailCard, label, desc);
        DetailCardHelper.PositionLeftOf(
            detailCard.GetComponent<RectTransform>(),
            anchorButton.GetComponent<RectTransform>(),
            content.GetComponent<RectTransform>());
    }

    private void HideDetailCard()
    {
        DetailCardHelper.Hide(detailCard);
    }
}
