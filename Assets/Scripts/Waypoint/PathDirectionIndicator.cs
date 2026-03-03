using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Prepare 상태에서 경로 입구에서 화살표가 생성되어 경로를 따라 이동하며 페이드 아웃됩니다.
/// WaypointPath와 같은 GameObject에 추가하세요.
/// </summary>
[RequireComponent(typeof(WaypointPath))]
public class PathDirectionIndicator : MonoBehaviour
{
    [Header("화살표 설정")]
    [SerializeField] private float arrowSpacing = 2.5f;     // 화살표 간격 (유닛)
    [SerializeField] private float arrowSize = 0.38f;       // 화살표 크기
    [SerializeField] private Color arrowColor = new(1f, 1f, 1f, 0.85f);
    [SerializeField] private float flowSpeed = 2f;          // 흐르는 속도 (유닛/초)
    [SerializeField] private float fadeDuration = 0.5f;     // 전체 페이드 인/아웃 시간
    [SerializeField] private float cornerRadius = 0.6f;     // 코너 베지어 반경 (0 = 꺾임 그대로)

    private static readonly int ColorPropID = Shader.PropertyToID("_Color");

    private WaypointPath waypointPath;
    private Material sharedMaterial;
    private readonly List<Transform> arrowTransforms = new();
    private readonly List<MeshRenderer> arrowRenderers = new();
    private MaterialPropertyBlock[] propBlocks;
    private float[] arrowOffsets;
    private float[] segmentLengths;
    private Vector3[] waypoints;
    private float totalLength;

    // 0 = 완전 투명, 1 = 완전 불투명 (Prepare 시 페이드 인/아웃 제어)
    private float globalFade;
    private bool flowing;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        waypointPath = GetComponent<WaypointPath>();
    }

    private void Start()
    {
        BuildArrows();
        GameManager.Instance.OnStateChanged += OnStateChanged;
        OnStateChanged(GameManager.Instance.CurrentState);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnStateChanged;
    }

    private void Update()
    {
        // 완전 투명하면 업데이트 불필요
        if (globalFade <= 0f || arrowOffsets == null) return;

        if (flowing)
        {
            float delta = flowSpeed * Time.deltaTime;
            for (int i = 0; i < arrowOffsets.Length; i++)
                arrowOffsets[i] = Mathf.Repeat(arrowOffsets[i] + delta, totalLength);
        }

        UpdateTransforms();
    }

    private void OnStateChanged(GameState state)
    {
        bool show = state == GameState.Prepare;
        flowing = show;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        if (gameObject.activeInHierarchy)
            fadeCoroutine = StartCoroutine(FadeGlobal(show ? 1f : 0f));
    }

    private void BuildArrows()
    {
        waypoints = BuildSmoothedWaypoints(waypointPath.GetWaypoints());
        if (waypoints.Length < 2) return;

        segmentLengths = new float[waypoints.Length - 1];
        totalLength = 0f;
        for (int i = 0; i < segmentLengths.Length; i++)
        {
            segmentLengths[i] = Vector3.Distance(waypoints[i], waypoints[i + 1]);
            totalLength += segmentLengths[i];
        }
        if (totalLength <= 0f) return;

        // 입구부터 간격에 맞게 분포 (스태거)
        int count = Mathf.Max(2, Mathf.FloorToInt(totalLength / arrowSpacing));
        arrowOffsets = new float[count];
        propBlocks = new MaterialPropertyBlock[count];
        for (int i = 0; i < count; i++)
        {
            arrowOffsets[i] = (float)i / count * totalLength;
            propBlocks[i] = new MaterialPropertyBlock();
        }

        sharedMaterial = new Material(Shader.Find("Sprites/Default"));

        var arrowMesh = BuildCircleMesh();
        for (int i = 0; i < count; i++)
        {
            var obj = new GameObject($"PathArrow_{i}");
            obj.transform.SetParent(transform);

            obj.AddComponent<MeshFilter>().mesh = arrowMesh;
            var mr = obj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = sharedMaterial;
            mr.sortingLayerName = GameConstants.SortPath;
            mr.sortingOrder = 1;

            arrowTransforms.Add(obj.transform);
            arrowRenderers.Add(mr);
        }

        UpdateTransforms();
    }

    private void UpdateTransforms()
    {
        for (int i = 0; i < arrowTransforms.Count; i++)
        {
            SamplePathPosition(arrowOffsets[i], out Vector3 pos);
            arrowTransforms[i].position = pos;

            // 입구(offset=0)에서 최대 불투명, 경로 끝(offset=totalLength)에서 완전 투명
            float posAlpha = 1f - Mathf.Clamp01(arrowOffsets[i] / totalLength);
            float finalAlpha = arrowColor.a * posAlpha * globalFade;
            propBlocks[i].SetColor(ColorPropID,
                new Color(arrowColor.r, arrowColor.g, arrowColor.b, finalAlpha));
            arrowRenderers[i].SetPropertyBlock(propBlocks[i]);
        }
    }

    private void SamplePathPosition(float dist, out Vector3 pos)
    {
        float rem = dist;
        for (int i = 0; i < segmentLengths.Length; i++)
        {
            if (rem <= segmentLengths[i] || i == segmentLengths.Length - 1)
            {
                float t = segmentLengths[i] > 0f ? Mathf.Clamp01(rem / segmentLengths[i]) : 0f;
                pos = Vector3.Lerp(waypoints[i], waypoints[i + 1], t);
                return;
            }
            rem -= segmentLengths[i];
        }
        pos = waypoints[^1];
    }

    /// <summary>
    /// 각 코너를 2차 베지어 곡선으로 치환해 점이 코너 안쪽을 따라 돌게 합니다.
    /// cornerRadius 만큼 코너 전후를 잘라 곡선 샘플로 채웁니다.
    /// </summary>
    private Vector3[] BuildSmoothedWaypoints(Vector3[] raw)
    {
        if (raw.Length < 3 || cornerRadius <= 0f) return raw;

        const int bezierSamples = 10;
        var result = new List<Vector3> { raw[0] };

        for (int i = 1; i < raw.Length - 1; i++)
        {
            Vector3 dirIn  = (raw[i] - raw[i - 1]).normalized;
            Vector3 dirOut = (raw[i + 1] - raw[i]).normalized;
            float segIn    = Vector3.Distance(raw[i - 1], raw[i]);
            float segOut   = Vector3.Distance(raw[i], raw[i + 1]);
            float r        = Mathf.Min(cornerRadius, segIn * 0.49f, segOut * 0.49f);

            Vector3 pBefore = raw[i] - dirIn  * r;
            Vector3 pAfter  = raw[i] + dirOut * r;

            result.Add(pBefore);
            // 제어점 = 코너 꼭짓점, t=0이 pBefore, t=1이 pAfter
            for (int j = 1; j <= bezierSamples; j++)
            {
                float t = (float)j / bezierSamples;
                float mt = 1f - t;
                result.Add(mt * mt * pBefore + 2f * mt * t * raw[i] + t * t * pAfter);
            }
        }

        result.Add(raw[^1]);
        return result.ToArray();
    }

    private Mesh BuildCircleMesh()
    {
        const int segments = 16;
        float r = arrowSize;
        var mesh = new Mesh { name = "PathDotMesh" };

        var verts = new Vector3[segments + 1];
        var tris = new int[segments * 3];
        verts[0] = Vector3.zero;
        for (int i = 0; i < segments; i++)
        {
            float angle = 2f * Mathf.PI * i / segments;
            verts[i + 1] = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f);
            tris[i * 3]     = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = (i + 1) % segments + 1;
        }

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        return mesh;
    }

    /// <summary>전체 표시 강도를 target으로 페이드. Update가 실제 렌더링을 담당.</summary>
    private IEnumerator FadeGlobal(float target)
    {
        float start = globalFade;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            globalFade = Mathf.Lerp(start, target, elapsed / fadeDuration);
            yield return null;
        }
        globalFade = target;
        UpdateTransforms(); // 루프 종료 후 최종값(0 or 목표) 즉시 반영
    }
}
