using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameSceneSetup
{
    [MenuItem("PopRouteTD/풍선 스포너 단일 프리팹 전환")]
    public static void MigrateBalloonSpawner()
    {
        var spawner = Object.FindAnyObjectByType<BalloonSpawner>();
        if (spawner == null)
        {
            Debug.LogError("[PopRouteTD] 씬에 BalloonSpawner가 없습니다.");
            return;
        }

        var so = new SerializedObject(spawner);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Balloon.prefab");
        if (prefab == null)
        {
            Debug.LogError("[PopRouteTD] Balloon.prefab이 없습니다. 'PopRouteTD/프리팹 에셋 생성'을 먼저 실행하세요.");
            return;
        }

        so.FindProperty("balloonPrefab").objectReferenceValue = prefab;
        so.ApplyModifiedProperties();
        Debug.Log("[PopRouteTD] BalloonSpawner 단일 프리팹 전환 완료!");
    }

    [MenuItem("PopRouteTD/씬 자동 구성")]
    public static void SetupGameScene()
    {
        // 카메라 설정
        var cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 10;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.transform.rotation = Quaternion.identity;
            cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        // 기존 Directional Light 제거
        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var light in lights) Object.DestroyImmediate(light.gameObject);

        // Map
        var map = CreateEmpty("Map");

        // 웨이포인트 경로 (S자형)
        var wpPath = CreateEmpty("WaypointPath", map.transform);
        var waypointPath = wpPath.AddComponent<WaypointPath>();

        Vector3[] wpPositions =
        {
            new(-8, 6, 0),
            new(8, 6, 0),
            new(8, 2, 0),
            new(-8, 2, 0),
            new(-8, -2, 0),
            new(8, -2, 0),
            new(8, -6, 0),
            new(-8, -6, 0)
        };

        for (int i = 0; i < wpPositions.Length; i++)
        {
            var wp = CreateEmpty($"WP_{i}", wpPath.transform);
            wp.transform.position = wpPositions[i];
        }

        // 부모 Transform
        var enemies = CreateEmpty("Enemies");
        var projectiles = CreateEmpty("Projectiles");
        var towers = CreateEmpty("Towers");
        var items = CreateEmpty("Items");

        // Managers
        var managers = CreateEmpty("Managers");

        var gmObj = CreateEmpty("GameManager", managers.transform);
        var gameManager = gmObj.AddComponent<GameManager>();
        gmObj.AddComponent<ResourceManager>();

        // BalloonSpawner - 프리팹 참조 자동 연결
        var spawnerObj = CreateEmpty("BalloonSpawner", managers.transform);
        var spawner = spawnerObj.AddComponent<BalloonSpawner>();
        WireBalloonSpawner(spawner, enemies.transform);

        // ProjectilePool - 투사체 풀
        var projPoolObj = CreateEmpty("ProjectilePool", managers.transform);
        var projPool = projPoolObj.AddComponent<ProjectilePool>();
        WireProjectilePool(projPool, projectiles.transform);

        // WaveManager - 웨이포인트 참조 연결
        var waveObj = CreateEmpty("WaveManager", managers.transform);
        var waveManager = waveObj.AddComponent<WaveManager>();
        WireWaveManager(waveManager, waypointPath);

        // GameManager - WaveManager 참조 연결
        var gmSo = new SerializedObject(gameManager);
        gmSo.FindProperty("waveManager").objectReferenceValue = waveManager;
        gmSo.ApplyModifiedPropertiesWithoutUndo();

        // ItemPool - 아이템 풀
        var itemPoolObj = CreateEmpty("ItemPool", managers.transform);
        var itemPool = itemPoolObj.AddComponent<ItemPool>();
        WireItemPool(itemPool, items.transform);

        // ItemSpawner - 아이템 스폰
        var itemSpawnerObj = CreateEmpty("ItemSpawner", managers.transform);
        itemSpawnerObj.AddComponent<ItemSpawner>();

        // InputManager - 카메라/웨이포인트/타워 부모 연결
        var inputObj = CreateEmpty("InputManager", managers.transform);
        var inputManager = inputObj.AddComponent<InputManager>();
        WireInputManager(inputManager, cam, waypointPath, towers.transform);

        // UI
        Canvas canvas;
        if (Object.FindAnyObjectByType<Canvas>() == null)
        {
            var canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvas = Object.FindAnyObjectByType<Canvas>();
        }

        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        CreateHUD(canvas.transform);
        CreateSpeedControlUI(canvas.transform);
        CreateTowerSelectUI(canvas.transform);
        CreateGameOverUI(canvas.transform);

        Debug.Log("[PopRouteTD] 씬 구성 완료! 먼저 'PopRouteTD/프리팹 에셋 생성'을 실행한 뒤, Inspector에서 누락된 참조를 확인하세요.");
    }

    private static void WireBalloonSpawner(BalloonSpawner spawner, Transform enemyParent)
    {
        var so = new SerializedObject(spawner);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Balloon.prefab");
        so.FindProperty("balloonPrefab").objectReferenceValue = prefab;
        so.FindProperty("enemyParent").objectReferenceValue = enemyParent;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireProjectilePool(ProjectilePool pool, Transform projectileParent)
    {
        var so = new SerializedObject(pool);
        var projPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Towers/Projectile.prefab");
        so.FindProperty("projectilePrefab").objectReferenceValue = projPrefab;
        so.FindProperty("projectileParent").objectReferenceValue = projectileParent;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireItemPool(ItemPool pool, Transform itemParent)
    {
        var so = new SerializedObject(pool);
        var itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Items/EnergyItem.prefab");
        so.FindProperty("itemPrefab").objectReferenceValue = itemPrefab;
        so.FindProperty("itemParent").objectReferenceValue = itemParent;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireWaveManager(WaveManager waveManager, WaypointPath path)
    {
        var so = new SerializedObject(waveManager);
        so.FindProperty("waypointPath").objectReferenceValue = path;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireInputManager(InputManager inputManager, Camera cam, WaypointPath path, Transform towerParent)
    {
        var so = new SerializedObject(inputManager);
        so.FindProperty("mainCamera").objectReferenceValue = cam;
        so.FindProperty("waypointPath").objectReferenceValue = path;
        so.FindProperty("towerParent").objectReferenceValue = towerParent;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateHUD(Transform canvasTransform)
    {
        var hudObj = CreateEmpty("HUD", canvasTransform);
        var hud = hudObj.AddComponent<GameHUD>();
        var hudRect = hudObj.AddComponent<RectTransform>();
        StretchFull(hudRect);

        // 상단 좌측 정보 텍스트
        var energyText = CreateUIText("EnergyText", hudObj.transform, new Vector2(20, -20), "Energy: 100");
        var livesText = CreateUIText("LivesText", hudObj.transform, new Vector2(20, -60), "Lives: 20");
        var waveText = CreateUIText("WaveText", hudObj.transform, new Vector2(20, -100), "Wave: 0");
        var stateText = CreateUIText("StateText", hudObj.transform, new Vector2(20, -140), "Prepare");

        // 웨이브 시작 버튼
        var startBtnObj = CreateButton("StartWaveButton", hudObj.transform, new Vector2(0, -480), "Start Wave");
        var startBtnRect = startBtnObj.GetComponent<RectTransform>();
        startBtnRect.anchorMin = new Vector2(0.5f, 1f);
        startBtnRect.anchorMax = new Vector2(0.5f, 1f);
        startBtnRect.sizeDelta = new Vector2(200, 50);

        var so = new SerializedObject(hud);
        so.FindProperty("energyText").objectReferenceValue = energyText.GetComponent<Text>();
        so.FindProperty("livesText").objectReferenceValue = livesText.GetComponent<Text>();
        so.FindProperty("waveText").objectReferenceValue = waveText.GetComponent<Text>();
        so.FindProperty("stateText").objectReferenceValue = stateText.GetComponent<Text>();
        so.FindProperty("startWaveButton").objectReferenceValue = startBtnObj.GetComponent<Button>();
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("PopRouteTD/배속 버튼 추가")]
    public static void AddSpeedControlUI()
    {
        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[PopRouteTD] 씬에 Canvas가 없습니다. '씬 자동 구성'을 먼저 실행하세요.");
            return;
        }
        CreateSpeedControlUI(canvas.transform);
        Debug.Log("[PopRouteTD] 배속 버튼 추가 완료!");
    }

    private static void CreateSpeedControlUI(Transform canvasTransform)
    {
        var panelObj = CreateEmpty("SpeedControlPanel", canvasTransform);
        var speedUI = panelObj.AddComponent<SpeedControlUI>();
        var panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(1, 1);
        panelRect.anchoredPosition = new Vector2(-20, -20);
        panelRect.sizeDelta = new Vector2(200, 40);

        var hlg = panelObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 5;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        string[] labels = { "x1", "x2", "x4" };
        var buttons = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            var btnObj = new GameObject(labels[i]);
            btnObj.transform.SetParent(panelObj.transform);
            var btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(60, 40);

            var img = btnObj.AddComponent<Image>();
            img.color = i == 0 ? new Color(0.2f, 0.6f, 0.3f) : new Color(0.3f, 0.3f, 0.4f);
            buttons[i] = btnObj.AddComponent<Button>();

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform);
            var textRect = textObj.AddComponent<RectTransform>();
            StretchFull(textRect);
            var t = textObj.AddComponent<Text>();
            t.text = labels[i];
            t.fontSize = 18;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
        }

        var so = new SerializedObject(speedUI);
        var prop = so.FindProperty("speedButtons");
        prop.arraySize = 3;
        for (int i = 0; i < 3; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateTowerSelectUI(Transform canvasTransform)
    {
        var panelObj = CreateEmpty("TowerSelectPanel", canvasTransform);
        var towerUI = panelObj.AddComponent<TowerSelectUI>();
        var panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(1, 0);
        panelRect.pivot = new Vector2(0.5f, 0);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(0, 80);

        var hlg = panelObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.padding = new RectOffset(10, 10, 5, 5);

        var bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.5f);

        // 타워 버튼 템플릿 프리팹 생성 (Assets/Prefabs에 저장)
        var templateBtn = CreateTowerButtonTemplate();

        // 타워 프리팹 참조
        var shooterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Towers/Tower_BasicShooter.prefab");
        var generatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Towers/Tower_EnergyGenerator.prefab");

        var so = new SerializedObject(towerUI);
        so.FindProperty("buttonParent").objectReferenceValue = panelRect;

        var towerPrefabs = so.FindProperty("towerPrefabs");
        towerPrefabs.arraySize = 2;
        towerPrefabs.GetArrayElementAtIndex(0).objectReferenceValue = shooterPrefab;
        towerPrefabs.GetArrayElementAtIndex(1).objectReferenceValue = generatorPrefab;

        so.FindProperty("towerButtonPrefab").objectReferenceValue = templateBtn;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateTowerButtonTemplate()
    {
        var path = "Assets/Prefabs/UI/TowerButton.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        EnsureFolder("Assets/Prefabs/UI");

        var btnObj = new GameObject("TowerButton");
        var rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120, 60);

        var img = btnObj.AddComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.4f);
        btnObj.AddComponent<Button>();

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform);
        var textRect = textObj.AddComponent<RectTransform>();
        StretchFull(textRect);
        var t = textObj.AddComponent<Text>();
        t.text = "Tower";
        t.fontSize = 16;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;

        var prefab = PrefabUtility.SaveAsPrefabAsset(btnObj, path);
        Object.DestroyImmediate(btnObj);
        return prefab;
    }

    private static void CreateGameOverUI(Transform canvasTransform)
    {
        var panelObj = CreateEmpty("GameOverPanel", canvasTransform);
        var gameOverUI = panelObj.AddComponent<GameOverUI>();
        var panelRect = panelObj.AddComponent<RectTransform>();
        StretchFull(panelRect);

        // 어두운 오버레이 패널
        var overlayObj = CreateEmpty("Overlay", panelObj.transform);
        var overlayRect = overlayObj.AddComponent<RectTransform>();
        StretchFull(overlayRect);
        var overlayImg = overlayObj.AddComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.7f);

        // 게임 오버 텍스트
        var titleObj = CreateUIText("GameOverTitle", overlayObj.transform, new Vector2(0, 60), "GAME OVER");
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleObj.GetComponent<Text>().fontSize = 48;
        titleObj.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

        var finalWaveObj = CreateUIText("FinalWaveText", overlayObj.transform, new Vector2(0, 0), "Wave 0");
        var fwRect = finalWaveObj.GetComponent<RectTransform>();
        fwRect.anchorMin = new Vector2(0.5f, 0.5f);
        fwRect.anchorMax = new Vector2(0.5f, 0.5f);
        finalWaveObj.GetComponent<Text>().fontSize = 24;
        finalWaveObj.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

        // 재시작 버튼
        var restartBtn = CreateButton("RestartButton", overlayObj.transform, new Vector2(0, -60), "Restart");
        var restartRect = restartBtn.GetComponent<RectTransform>();
        restartRect.anchorMin = new Vector2(0.5f, 0.5f);
        restartRect.anchorMax = new Vector2(0.5f, 0.5f);

        var so = new SerializedObject(gameOverUI);
        so.FindProperty("gameOverPanel").objectReferenceValue = overlayObj;
        so.FindProperty("finalWaveText").objectReferenceValue = finalWaveObj.GetComponent<Text>();
        so.FindProperty("restartButton").objectReferenceValue = restartBtn.GetComponent<Button>();
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateUIText(string name, Transform parent, Vector2 pos, string text)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(300, 40);

        var t = obj.AddComponent<Text>();
        t.text = text;
        t.fontSize = 24;
        t.color = Color.white;
        return obj;
    }

    private static GameObject CreateButton(string name, Transform parent, Vector2 pos, string label)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(200, 50);

        var img = obj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.5f, 0.2f);
        obj.AddComponent<Button>();

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform);
        var textRect = textObj.AddComponent<RectTransform>();
        StretchFull(textRect);

        var t = textObj.AddComponent<Text>();
        t.text = label;
        t.fontSize = 20;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;

        return obj;
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static GameObject CreateEmpty(string name, Transform parent = null)
    {
        var obj = new GameObject(name);
        if (parent != null) obj.transform.SetParent(parent);
        return obj;
    }

    private static void EnsureFolder(string path)
    {
        var parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
