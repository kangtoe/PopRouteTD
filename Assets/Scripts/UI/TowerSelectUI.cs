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
    private readonly List<(TowerButtonUI button, int cost, string label, Sprite icon)> towerButtons = new();
    private readonly List<(TowerButtonUI button, int cost, string label, Sprite icon)> bombButtons = new();

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

    public void Hide() => content.SetActive(false);

    private void CreateButtons()
    {
        foreach (var prefab in towerPrefabs)
        {
            var tower = prefab.GetComponent<Tower>();
            if (tower == null) continue;

            var tb = Instantiate(towerButtonPrefab, buttonParent);

            string label = tower.TowerName;
            var icon = TowerIconGenerator.GenerateIcon(prefab);
            generatedIcons.Add(icon);
            tb.SetAvailable(label, tower.Cost, icon: icon);
            towerButtons.Add((tb, tower.Cost, label, icon));

            // PointerDown으로 드래그 배치 시작
            var p = prefab;
            var btnObj = tb.gameObject;
            var trigger = btnObj.GetComponent<EventTrigger>() ?? btnObj.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entry.callback.AddListener(_ => InputManager.Instance.BeginDrag(p));
            trigger.triggers.Add(entry);
        }

        var bombParent = bombButtonParent != null ? bombButtonParent : buttonParent;
        foreach (var prefab in bombPrefabs)
        {
            var bomb = prefab.GetComponent<Bomb>();
            if (bomb == null) continue;

            var tb = Instantiate(towerButtonPrefab, bombParent);

            string label = bomb.BombName;
            var icon = TowerIconGenerator.GenerateIcon(prefab);
            generatedIcons.Add(icon);
            tb.SetAvailable(label, bomb.Cost, icon: icon);
            bombButtons.Add((tb, bomb.Cost, label, icon));

            var p = prefab;
            var btnObj = tb.gameObject;
            var trigger = btnObj.GetComponent<EventTrigger>() ?? btnObj.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entry.callback.AddListener(_ => InputManager.Instance.BeginDrag(p));
            trigger.triggers.Add(entry);
        }
    }

    private void OnGoldChanged(int _)
    {
        foreach (var (button, cost, label, icon) in towerButtons)
            button.SetAvailable(label, cost, icon: icon);
        foreach (var (button, cost, label, icon) in bombButtons)
            button.SetAvailable(label, cost, icon: icon);
    }
}
