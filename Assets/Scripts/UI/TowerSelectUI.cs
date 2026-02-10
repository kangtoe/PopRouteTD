using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TowerSelectUI : MonoBehaviour
{
    [SerializeField] private List<GameObject> towerPrefabs;
    [SerializeField] private Transform buttonParent;
    [SerializeField] private GameObject towerButtonPrefab;

    private void Start()
    {
        CreateButtons();
    }

    private void CreateButtons()
    {
        foreach (var prefab in towerPrefabs)
        {
            var tower = prefab.GetComponent<Tower>();
            if (tower == null) continue;

            var btnObj = Instantiate(towerButtonPrefab, buttonParent);
            var text = btnObj.GetComponentInChildren<Text>();

            if (text != null)
            {
                text.text = $"{tower.TowerName}\n({tower.Cost})";
            }

            // PointerDown으로 드래그 배치 시작
            var p = prefab;
            var trigger = btnObj.GetComponent<EventTrigger>() ?? btnObj.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entry.callback.AddListener(_ => InputManager.Instance.BeginDrag(p));
            trigger.triggers.Add(entry);
        }
    }
}
