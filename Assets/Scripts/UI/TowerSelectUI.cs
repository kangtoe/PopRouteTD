using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TowerSelectUI : MonoBehaviour
{
    public static TowerSelectUI Instance { get; private set; }

    [SerializeField] private GameObject content;
    [SerializeField] private Text nameText;
    [SerializeField] private List<GameObject> towerPrefabs;
    [SerializeField] private List<GameObject> bombPrefabs;
    [SerializeField] private Transform buttonParent;
    [SerializeField] private Transform bombButtonParent;
    [SerializeField] private Text bombNameText;
    [SerializeField] private TowerButtonUI towerButtonPrefab;

    private readonly List<Sprite> generatedIcons = new();
    private readonly List<(TowerButtonUI button, int cost, string label, Sprite icon, string desc)> towerButtons = new();
    private readonly List<(TowerButtonUI button, int cost, string label, Sprite icon, string desc)> bombButtons = new();

    private TowerButtonUI detailCard;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (Instance != this) return;
        CreateButtons();
        Show();
        ResourceManager.Instance.OnGoldChanged += OnGoldChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnGoldChanged -= OnGoldChanged;

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
        if (nameText != null) nameText.text = "Towers";
        if (bombNameText != null) bombNameText.text = "Bombs";
    }

    public void Hide()
    {
        HideDetailCard();
        content.SetActive(false);
    }

    private void CreateButtons()
    {
        foreach (var prefab in towerPrefabs)
        {
            var tower = prefab.GetComponent<Tower>();
            if (tower == null) continue;

            var tb = Instantiate(towerButtonPrefab, buttonParent);

            string label = tower.TowerName;
            string desc = tower.UpgradeData != null ? tower.UpgradeData.description : "";
            var icon = TowerIconGenerator.GenerateIcon(prefab);
            generatedIcons.Add(icon);
            tb.SetAvailable(label, tower.Cost, icon: icon);
            towerButtons.Add((tb, tower.Cost, label, icon, desc));

            // PointerDown으로 드래그 배치 시작
            var p = prefab;
            var btnObj = tb.gameObject;
            var trigger = btnObj.GetComponent<EventTrigger>() ?? btnObj.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entry.callback.AddListener(_ => InputManager.Instance.BeginDrag(p));
            trigger.triggers.Add(entry);

            int idx = towerButtons.Count - 1;
            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ => ShowTowerDetailCard(idx));
            trigger.triggers.Add(enterEntry);

            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener(_ => HideDetailCard());
            trigger.triggers.Add(exitEntry);
        }

        var bombParent = bombButtonParent != null ? bombButtonParent : buttonParent;
        foreach (var prefab in bombPrefabs)
        {
            var bomb = prefab.GetComponent<Bomb>();
            if (bomb == null) continue;

            var tb = Instantiate(towerButtonPrefab, bombParent);

            string label = bomb.BombName;
            string desc = bomb.Description;
            var icon = TowerIconGenerator.GenerateIcon(prefab);
            generatedIcons.Add(icon);
            tb.SetAvailable(label, bomb.Cost, icon: icon);
            bombButtons.Add((tb, bomb.Cost, label, icon, desc));

            var p = prefab;
            var btnObj = tb.gameObject;
            var trigger = btnObj.GetComponent<EventTrigger>() ?? btnObj.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entry.callback.AddListener(_ => InputManager.Instance.BeginDrag(p));
            trigger.triggers.Add(entry);

            int idx = bombButtons.Count - 1;
            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ => ShowBombDetailCard(idx));
            trigger.triggers.Add(enterEntry);

            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener(_ => HideDetailCard());
            trigger.triggers.Add(exitEntry);
        }
    }

    private void OnGoldChanged(int _)
    {
        foreach (var (button, cost, label, icon, _) in towerButtons)
            button.SetAvailable(label, cost, icon: icon);
        foreach (var (button, cost, label, icon, _) in bombButtons)
            button.SetAvailable(label, cost, icon: icon);
    }

    private void EnsureDetailCard()
    {
        if (detailCard == null)
            detailCard = DetailCardHelper.Create(towerButtonPrefab, content.transform);
    }

    private void ShowTowerDetailCard(int index)
    {
        if (index < 0 || index >= towerButtons.Count) return;
        var (button, _, label, _, desc) = towerButtons[index];
        if (string.IsNullOrEmpty(desc)) return;
        EnsureDetailCard();
        DetailCardHelper.Show(detailCard, label, desc);
        DetailCardHelper.PositionLeftOf(
            detailCard.GetComponent<RectTransform>(),
            button.GetComponent<RectTransform>(),
            content.GetComponent<RectTransform>());
    }

    private void ShowBombDetailCard(int index)
    {
        if (index < 0 || index >= bombButtons.Count) return;
        var (button, _, label, _, desc) = bombButtons[index];
        if (string.IsNullOrEmpty(desc)) return;
        EnsureDetailCard();
        DetailCardHelper.Show(detailCard, label, desc);
        DetailCardHelper.PositionLeftOf(
            detailCard.GetComponent<RectTransform>(),
            button.GetComponent<RectTransform>(),
            content.GetComponent<RectTransform>());
    }

    private void HideDetailCard()
    {
        DetailCardHelper.Hide(detailCard);
    }
}
