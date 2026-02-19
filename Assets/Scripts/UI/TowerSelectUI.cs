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
    [SerializeField] private Transform buttonParent;
    [SerializeField] private TowerButton towerButtonPrefab;

    private readonly List<Sprite> generatedIcons = new();

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
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

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
    }

    public void Hide() => content.SetActive(false);

    private void CreateButtons()
    {
        foreach (var prefab in towerPrefabs)
        {
            var tower = prefab.GetComponent<Tower>();
            if (tower == null) continue;

            var tb = Instantiate(towerButtonPrefab, buttonParent);

            string label = $"{tower.TowerName}\n({tower.Cost})";
            if (tb.LabelText != null) tb.LabelText.text = label;

            if (tb.IconImage != null)
            {
                var icon = TowerIconGenerator.GenerateIcon(prefab);
                tb.IconImage.sprite = icon;
                generatedIcons.Add(icon);
            }

            // PointerDown으로 드래그 배치 시작
            var p = prefab;
            var btnObj = tb.gameObject;
            var trigger = btnObj.GetComponent<EventTrigger>() ?? btnObj.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entry.callback.AddListener(_ => InputManager.Instance.BeginDrag(p));
            trigger.triggers.Add(entry);
        }
    }
}
