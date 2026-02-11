using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TowerSelectUI : MonoBehaviour
{
    public static TowerSelectUI Instance { get; private set; }

    [SerializeField] private List<GameObject> towerPrefabs;
    [SerializeField] private Transform buttonParent;
    [SerializeField] private TowerButton towerButtonPrefab;

    private readonly Dictionary<GameObject, CooldownState> cooldowns = new();

    private struct CooldownState
    {
        public TowerButton ui;
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

            var tb = Instantiate(towerButtonPrefab, buttonParent);

            string label = $"{tower.TowerName}\n({tower.Cost})";
            if (tb.LabelText != null) tb.LabelText.text = label;

            if (tb.CooldownOverlay != null) tb.CooldownOverlay.fillAmount = 0f;
            if (tb.CooldownText != null) tb.CooldownText.gameObject.SetActive(false);

            // PointerDown으로 드래그 배치 시작
            var p = prefab;
            var btnObj = tb.gameObject;
            var trigger = btnObj.GetComponent<EventTrigger>() ?? btnObj.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entry.callback.AddListener(_ => InputManager.Instance.BeginDrag(p));
            trigger.triggers.Add(entry);

            cooldowns[prefab] = new CooldownState
            {
                ui = tb,
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
            cd.ui.Button.interactable = false;
            if (cd.ui.CooldownOverlay != null) cd.ui.CooldownOverlay.fillAmount = 1f;
            if (cd.ui.CooldownText != null)
            {
                cd.ui.CooldownText.gameObject.SetActive(true);
                cd.ui.CooldownText.text = Mathf.CeilToInt(cd.timer).ToString();
            }
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
                cd.ui.Button.interactable = true;
                if (cd.ui.CooldownOverlay != null) cd.ui.CooldownOverlay.fillAmount = 0f;
                if (cd.ui.CooldownText != null) cd.ui.CooldownText.gameObject.SetActive(false);
            }
            else
            {
                if (cd.ui.CooldownOverlay != null)
                    cd.ui.CooldownOverlay.fillAmount = cd.timer / GameConstants.PlacementCooldown;
                if (cd.ui.CooldownText != null)
                    cd.ui.CooldownText.text = Mathf.CeilToInt(cd.timer).ToString();
            }
            cooldowns[key] = cd;
        }
    }
}
