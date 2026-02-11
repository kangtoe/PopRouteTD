using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [SerializeField] private Camera mainCamera;
    [SerializeField] private WaypointPath waypointPath;
    [SerializeField] private float towerRadius = 0.4f;
    [SerializeField] private Transform towerParent;

    private GameObject dragTowerPrefab;
    private bool isDragging;
    private int towerLayerMask;

    private GameObject previewObj;
    private SpriteRenderer previewRenderer;

    public System.Action<Tower> OnTowerClicked;
    public System.Action OnEmptyClicked;
    public System.Action<GameObject> OnTowerPlaced;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        towerLayerMask = 1 << LayerMask.NameToLayer(GameConstants.LayerTower);
    }

    /// <summary>UI 버튼 PointerDown에서 호출 — 드래그 배치 시작</summary>
    public void BeginDrag(GameObject towerPrefab)
    {
        if (TowerSelectUI.Instance != null && TowerSelectUI.Instance.IsOnCooldown(towerPrefab)) return;
        if (mainCamera == null) mainCamera = Camera.main;

        // 드래그 시작 시 타워 정보 패널 닫기
        OnEmptyClicked?.Invoke();

        dragTowerPrefab = towerPrefab;
        isDragging = true;
        CreatePreview();

        if (mainCamera != null && Mouse.current != null)
        {
            UpdatePreview(GetMouseWorldPos());
        }
    }

    private Vector2 GetMouseWorldPos()
    {
        Vector3 screenPos = Mouse.current.position.ReadValue();
        screenPos.z = -mainCamera.transform.position.z;
        return mainCamera.ScreenToWorldPoint(screenPos);
    }

    private void Update()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector3 mouseScreenPos = mouse.position.ReadValue();
        mouseScreenPos.z = -mainCamera.transform.position.z;
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);

        if (isDragging)
        {
            UpdatePreview(worldPos);

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                EndDrag(worldPos);
            }
            return;
        }

        // 드래그 중이 아닐 때: 기존 타워 클릭
        if (mouse.leftButton.wasPressedThisFrame && !EventSystem.current.IsPointerOverGameObject())
        {
            HandleTowerClick(worldPos);
        }
    }

    private void EndDrag(Vector2 worldPos)
    {
        bool placed = false;

        if (!EventSystem.current.IsPointerOverGameObject() && CanPlaceAt(worldPos))
        {
            placed = PlaceTower(worldPos);
        }

        isDragging = false;
        dragTowerPrefab = null;
        DestroyPreview();
    }

    private void HandleTowerClick(Vector2 worldPos)
    {
        var hit = Physics2D.OverlapPoint(worldPos, towerLayerMask);
        if (hit != null)
        {
            var tower = hit.GetComponent<Tower>();
            if (tower != null) OnTowerClicked?.Invoke(tower);
        }
        else
        {
            OnEmptyClicked?.Invoke();
        }
    }

    private bool CanPlaceAt(Vector2 pos)
    {
        if (IsOnPath(pos)) return false;

        var overlap = Physics2D.OverlapCircle(pos, towerRadius * 2f, towerLayerMask);
        if (overlap != null) return false;

        return true;
    }

    private bool IsOnPath(Vector2 pos)
    {
        if (waypointPath == null) return false;

        var waypoints = waypointPath.GetWaypoints();
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            Vector2 a = waypoints[i];
            Vector2 b = waypoints[i + 1];
            float dist = DistanceToSegment(pos, a, b);
            if (dist < waypointPath.PathWidth) return true;
        }
        return false;
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        Vector2 closest = a + t * ab;
        return Vector2.Distance(p, closest);
    }

    private bool PlaceTower(Vector2 pos)
    {
        var tower = dragTowerPrefab.GetComponent<Tower>();
        if (tower == null) return false;
        if (!ResourceManager.Instance.SpendEnergy(tower.Cost)) return false;

        var parent = towerParent != null ? towerParent : transform;
        var towerObj = Instantiate(dragTowerPrefab, (Vector3)pos, Quaternion.identity, parent);
        towerObj.GetComponent<Tower>().Initialize();
        OnTowerPlaced?.Invoke(dragTowerPrefab);
        return true;
    }

    private void CreatePreview()
    {
        DestroyPreview();
        previewObj = new GameObject("PlacementPreview");
        previewRenderer = previewObj.AddComponent<SpriteRenderer>();

        var sr = dragTowerPrefab.GetComponent<SpriteRenderer>();
        previewRenderer.sprite = sr != null ? sr.sprite : null;
        previewRenderer.sortingLayerName = GameConstants.SortTower;
        previewRenderer.sortingOrder = 100;
        previewObj.transform.localScale = dragTowerPrefab.transform.localScale;
    }

    private void UpdatePreview(Vector2 worldPos)
    {
        if (previewObj == null) return;

        previewObj.transform.position = (Vector3)worldPos;
        bool canPlace = CanPlaceAt(worldPos);
        previewRenderer.color = canPlace
            ? new Color(0.3f, 0.8f, 0.3f, 0.5f)
            : new Color(0.8f, 0.3f, 0.3f, 0.5f);
    }

    private void DestroyPreview()
    {
        if (previewObj != null)
        {
            Destroy(previewObj);
            previewObj = null;
            previewRenderer = null;
        }
    }
}
