using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TowerSelectUI : MonoBehaviour
{
    public static TowerSelectUI Instance { get; private set; }

    [SerializeField] private List<GameObject> towerPrefabs;
    [SerializeField] private Transform buttonParent;
    [SerializeField] private GameObject towerButtonPrefab;

    private readonly Dictionary<GameObject, ButtonCooldown> cooldowns = new();

    private struct ButtonCooldown
    {
        public Button button;
        public Text text;
        public string originalText;
        public float timer;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        CreateButtons();
        InputManager.Instance.OnTowerPlaced += OnTowerPlaced;
    }

    private void OnDestroy()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnTowerPlaced -= OnTowerPlaced;
    }

    public bool IsOnCooldown(GameObject prefab)
    {
        return cooldowns.TryGetValue(prefab, out var cd) && cd.timer > 0f;
    }

    private void CreateButtons()
    {
        foreach (var prefab in towerPrefabs)
        {
            var tower = prefab.GetComponent<Tower>();
            if (tower == null) continue;

            var btnObj = Instantiate(towerButtonPrefab, buttonParent);
            var text = btnObj.GetComponentInChildren<Text>();
            var button = btnObj.GetComponent<Button>();

            string label = $"{tower.TowerName}\n({tower.Cost})";
            if (text != null) text.text = label;

            // PointerDown으로 드래그 배치 시작
            var p = prefab;
            var trigger = btnObj.GetComponent<EventTrigger>() ?? btnObj.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entry.callback.AddListener(_ => InputManager.Instance.BeginDrag(p));
            trigger.triggers.Add(entry);

            cooldowns[prefab] = new ButtonCooldown
            {
                button = button,
                text = text,
                originalText = label,
                timer = 0f
            };
        }
    }

    private void OnTowerPlaced(GameObject prefab)
    {
        if (GameManager.Instance.CurrentState != GameState.Prepare
            && cooldowns.ContainsKey(prefab))
        {
            var cd = cooldowns[prefab];
            cd.timer = GameConstants.PlacementCooldown;
            cd.button.interactable = false;
            cooldowns[prefab] = cd;
        }
    }

    private void Update()
    {
        var keys = new List<GameObject>(cooldowns.Keys);
        foreach (var key in keys)
        {
            var cd = cooldowns[key];
            if (cd.timer <= 0f) continue;

            cd.timer -= Time.deltaTime;
            if (cd.timer <= 0f)
            {
                cd.timer = 0f;
                cd.button.interactable = true;
                if (cd.text != null) cd.text.text = cd.originalText;
            }
            else
            {
                if (cd.text != null) cd.text.text = $"{Mathf.CeilToInt(cd.timer)}s";
            }
            cooldowns[key] = cd;
        }
    }
}
