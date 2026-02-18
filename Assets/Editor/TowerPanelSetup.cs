using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// TowerSelectUI(하단 빌드 패널)와 TowerInfoUI(우측 정보 패널)를
/// 하나의 공유 패널로 통합하는 에디터 자동화 스크립트.
/// 이미 실행된 상태에서도 재실행 가능 (idempotent).
/// 실행: Tools > Setup Shared Tower Panel
/// </summary>
public static class TowerPanelSetup
{
    private const float NameTextHeight = 60f;

    [MenuItem("Tools/Setup Shared Tower Panel")]
    public static void Execute()
    {
        var panel = GameObject.Find("TowerInfoUiPanel");
        if (panel == null)
        {
            Debug.LogError("[TowerPanelSetup] TowerInfoUiPanel을 찾을 수 없습니다.");
            return;
        }

        Undo.SetCurrentGroupName("Setup Shared Tower Panel");
        int undoGroup = Undo.GetCurrentGroup();

        // ── 이미 실행된 적 있는지 감지 ──
        var existingBuild = panel.transform.Find("BuildContent");
        var existingInfo = panel.transform.Find("InfoContent");
        bool alreadySetup = existingBuild != null && existingInfo != null;

        if (alreadySetup)
            FixExisting(panel, existingBuild, existingInfo);
        else
            SetupFresh(panel);

        // TowerStatsText 삭제 (어디에 있든)
        DestroyStatsText(panel.transform);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[TowerPanelSetup] 공유 타워 패널 설정 완료!");
    }

    /// <summary>이미 실행된 상태에서 레이아웃 문제만 수정</summary>
    private static void FixExisting(GameObject panel, Transform buildT, Transform infoT)
    {
        // 공용 텍스트 순서 정리
        var nameT = FindChildTrimmed(panel.transform, "TowerNameText");
        if (nameT != null) nameT.SetSiblingIndex(0);
        buildT.SetSiblingIndex(1);
        infoT.SetSiblingIndex(2);

        // BuildContent / InfoContent 레이아웃 수정
        FixContentRect(buildT.GetComponent<RectTransform>());
        FixContentRect(infoT.GetComponent<RectTransform>());

        // TowerSelectUI에 공용 텍스트 참조 연결
        var selectUI = panel.GetComponent<TowerSelectUI>();
        if (selectUI != null && nameT != null)
        {
            var so = new SerializedObject(selectUI);
            so.FindProperty("nameText").objectReferenceValue = nameT.GetComponent<Text>();
            so.ApplyModifiedProperties();
        }
    }

    /// <summary>최초 실행: 전체 구조 생성</summary>
    private static void SetupFresh(GameObject panel)
    {
        var oldSelectPanel = GameObject.Find("TowerSelectPanel");
        var towerInfoUI = Object.FindObjectOfType<TowerInfoUI>(true);

        if (oldSelectPanel == null || towerInfoUI == null)
        {
            Debug.LogError("[TowerPanelSetup] TowerSelectPanel 또는 TowerInfoUI를 찾을 수 없습니다.");
            return;
        }

        var oldSelectUI = oldSelectPanel.GetComponent<TowerSelectUI>();
        if (oldSelectUI == null)
        {
            Debug.LogError("[TowerPanelSetup] TowerSelectPanel에 TowerSelectUI 컴포넌트가 없습니다.");
            return;
        }

        // 기존 자식 분류 (TowerNameText만 공용, 나머지는 InfoContent로)
        Transform nameTextT = null;
        var infoChildren = new List<Transform>();

        for (int i = 0; i < panel.transform.childCount; i++)
        {
            var child = panel.transform.GetChild(i);
            string trimmed = child.name.Trim();
            if (trimmed == "TowerNameText")
                nameTextT = child;
            else if (trimmed == "TowerStatsText")
                continue; // 삭제 대상 — 이동하지 않음
            else
                infoChildren.Add(child);
        }

        // BuildContent 생성
        var buildContent = CreateContentObject("BuildContent", panel.transform);
        var buildLayout = buildContent.AddComponent<VerticalLayoutGroup>();
        buildLayout.spacing = 10;
        buildLayout.padding = new RectOffset(10, 10, 10, 10);
        buildLayout.childAlignment = TextAnchor.UpperCenter;
        buildLayout.childControlWidth = true;
        buildLayout.childControlHeight = false;
        buildLayout.childForceExpandWidth = true;
        buildLayout.childForceExpandHeight = false;

        // InfoContent 생성, 정보 전용 자식 이동
        var infoContent = CreateContentObject("InfoContent", panel.transform);
        foreach (var child in infoChildren)
            Undo.SetTransformParent(child, infoContent.transform, "Move to InfoContent");

        // 순서 정리: TowerNameText(0), BuildContent(1), InfoContent(2)
        if (nameTextT != null) nameTextT.SetSiblingIndex(0);
        buildContent.transform.SetSiblingIndex(1);
        infoContent.transform.SetSiblingIndex(2);

        // 기존 TowerSelectUI 프리팹 참조 읽기
        var oldSO = new SerializedObject(oldSelectUI);
        var prefabsProp = oldSO.FindProperty("towerPrefabs");
        var buttonPrefabProp = oldSO.FindProperty("towerButtonPrefab");

        var prefabRefs = new Object[prefabsProp.arraySize];
        for (int i = 0; i < prefabsProp.arraySize; i++)
            prefabRefs[i] = prefabsProp.GetArrayElementAtIndex(i).objectReferenceValue;
        var buttonPrefabRef = buttonPrefabProp.objectReferenceValue;

        // 공유 패널에 새 TowerSelectUI 추가
        Undo.AddComponent<TowerSelectUI>(panel);
        var newSelectUI = panel.GetComponent<TowerSelectUI>();
        var newSO = new SerializedObject(newSelectUI);

        newSO.FindProperty("content").objectReferenceValue = buildContent;
        var newPrefabsProp = newSO.FindProperty("towerPrefabs");
        newPrefabsProp.arraySize = prefabRefs.Length;
        for (int i = 0; i < prefabRefs.Length; i++)
            newPrefabsProp.GetArrayElementAtIndex(i).objectReferenceValue = prefabRefs[i];
        newSO.FindProperty("towerButtonPrefab").objectReferenceValue = buttonPrefabRef;
        newSO.FindProperty("buttonParent").objectReferenceValue = buildContent.transform;

        if (nameTextT != null)
            newSO.FindProperty("nameText").objectReferenceValue = nameTextT.GetComponent<Text>();
        newSO.ApplyModifiedProperties();

        // TowerInfoUI panel 참조 변경
        var infoSO = new SerializedObject(towerInfoUI);
        infoSO.FindProperty("panel").objectReferenceValue = infoContent;
        infoSO.ApplyModifiedProperties();

        // 초기 활성 상태
        Undo.RecordObject(buildContent, "Set BuildContent active");
        buildContent.SetActive(true);
        Undo.RecordObject(infoContent, "Set InfoContent inactive");
        infoContent.SetActive(false);

        // 기존 TowerSelectPanel 제거
        Undo.DestroyObjectImmediate(oldSelectPanel);
    }

    private static GameObject CreateContentObject(string name, Transform parent)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        FixContentRect(rect);
        return go;
    }

    /// <summary>TowerNameText 아래부터 하단까지 채우는 RectTransform 설정</summary>
    private static void FixContentRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = new Vector2(0, -NameTextHeight);
    }

    /// <summary>TowerStatsText를 패널 계층 어디서든 찾아 삭제</summary>
    private static void DestroyStatsText(Transform root)
    {
        var found = FindChildTrimmed(root, "TowerStatsText");
        if (found != null)
        {
            Undo.DestroyObjectImmediate(found.gameObject);
            return;
        }

        // InfoContent 안에 있을 수도 있음
        var info = root.Find("InfoContent");
        if (info != null)
        {
            found = FindChildTrimmed(info, "TowerStatsText");
            if (found != null)
                Undo.DestroyObjectImmediate(found.gameObject);
        }
    }

    private static Transform FindChildTrimmed(Transform parent, string trimmedName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name.Trim() == trimmedName)
                return child;
        }
        return null;
    }
}
