using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카메라 뷰포트를 풀 해상도로 유지하면서 목표 비율을 강제합니다.
/// Viewport Rect 축소 대신 Overlay UI 패널로 레터박스/필러박스를 구현하여
/// 렌더링 해상도 손실 없이 비율을 유지합니다.
/// </summary>
[RequireComponent(typeof(Camera))]
public class AspectRatioEnforcer : MonoBehaviour
{
    [SerializeField] private float targetAspect = 16f / 9f;

    private Camera cam;
    private float designOrthoSize;
    private int lastScreenWidth;
    private int lastScreenHeight;

    private RectTransform[] bars = new RectTransform[4];

    private void Awake()
    {
        cam = GetComponent<Camera>();
        designOrthoSize = cam.orthographicSize;
        CreateBars();
        Apply();
    }

    private void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            Apply();
    }

    private void Apply()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        // 카메라는 항상 전체 화면을 풀 해상도로 렌더링
        cam.rect = new Rect(0f, 0f, 1f, 1f);

        float screenAspect = (float)lastScreenWidth / lastScreenHeight;

        if (screenAspect < targetAspect)
        {
            // 화면이 더 세로로 길다 → 레터박스 (상하 검정)
            // orthoSize를 늘려 디자인 너비를 유지하고, 초과된 상하 영역을 패널로 가림
            cam.orthographicSize = designOrthoSize * targetAspect / screenAspect;
            float half = (1f - screenAspect / targetAspect) / 2f;
            SetBars(top: half, bottom: half, left: 0f, right: 0f);
        }
        else
        {
            // 화면이 더 가로로 길다 → 필러박스 (좌우 검정)
            cam.orthographicSize = designOrthoSize;
            float half = (1f - targetAspect / screenAspect) / 2f;
            SetBars(top: 0f, bottom: 0f, left: half, right: half);
        }
    }

    private void CreateBars()
    {
        var canvasObj = new GameObject("LetterboxCanvas");
        canvasObj.transform.SetParent(transform);

        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        for (int i = 0; i < 4; i++)
        {
            var barObj = new GameObject($"Bar_{i}");
            barObj.transform.SetParent(canvasObj.transform, false);

            var img = barObj.AddComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = false;

            bars[i] = barObj.GetComponent<RectTransform>();
        }
    }

    private void SetBars(float top, float bottom, float left, float right)
    {
        // Top
        bars[0].gameObject.SetActive(top > 0f);
        bars[0].anchorMin = new Vector2(0f, 1f - top);
        bars[0].anchorMax = Vector2.one;
        bars[0].offsetMin = Vector2.zero;
        bars[0].offsetMax = Vector2.zero;

        // Bottom
        bars[1].gameObject.SetActive(bottom > 0f);
        bars[1].anchorMin = Vector2.zero;
        bars[1].anchorMax = new Vector2(1f, bottom);
        bars[1].offsetMin = Vector2.zero;
        bars[1].offsetMax = Vector2.zero;

        // Left
        bars[2].gameObject.SetActive(left > 0f);
        bars[2].anchorMin = Vector2.zero;
        bars[2].anchorMax = new Vector2(left, 1f);
        bars[2].offsetMin = Vector2.zero;
        bars[2].offsetMax = Vector2.zero;

        // Right
        bars[3].gameObject.SetActive(right > 0f);
        bars[3].anchorMin = new Vector2(1f - right, 0f);
        bars[3].anchorMax = Vector2.one;
        bars[3].offsetMin = Vector2.zero;
        bars[3].offsetMax = Vector2.zero;
    }
}
