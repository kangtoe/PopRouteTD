using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class WaypointPath : MonoBehaviour
{
    [Header("경로 설정")]
    [SerializeField] private float pathWidth = 1f;
    [SerializeField] private Color pathColor = new(0.3f, 0.25f, 0.2f, 1f);
    [SerializeField] private float cornerRadius = 1.2f;

    private Vector3[] cachedSmoothed;

    public float PathWidth => pathWidth;

    public Vector3[] GetWaypoints()
    {
        var waypoints = new Vector3[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            waypoints[i] = transform.GetChild(i).position;
        }
        return waypoints;
    }

    public Vector3[] GetSmoothedWaypoints()
    {
        cachedSmoothed ??= BuildSmoothedWaypoints(GetWaypoints());
        return cachedSmoothed;
    }

    /// <summary>
    /// 각 코너를 2차 베지어 곡선으로 치환해 부드러운 경로를 생성합니다.
    /// </summary>
    private Vector3[] BuildSmoothedWaypoints(Vector3[] raw)
    {
        if (raw.Length < 3 || cornerRadius <= 0f) return raw;

        const int bezierSamples = 10;
        var result = new System.Collections.Generic.List<Vector3> { raw[0] };

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

    public Vector3 GetSpawnPoint()
    {
        if (transform.childCount == 0) return transform.position;
        return transform.GetChild(0).position;
    }

    private void Awake()
    {
        SetupLineRenderer(); 
    }

    private void SetupLineRenderer()
    {
        if (transform.childCount < 2) return;

        var smoothed = GetSmoothedWaypoints();
        var lr = GetComponent<LineRenderer>();
        lr.positionCount = smoothed.Length;
        lr.startWidth = pathWidth;
        lr.endWidth = pathWidth;
        lr.useWorldSpace = true;
        lr.sortingLayerName = GameConstants.SortPath;
        lr.sortingOrder = 0;
        lr.numCornerVertices = 5;
        lr.numCapVertices = 4;

        // 단색 머티리얼
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = pathColor;
        lr.endColor = pathColor;

        for (int i = 0; i < smoothed.Length; i++)
        {
            lr.SetPosition(i, smoothed[i]);
        }
    }

    private void OnDrawGizmos()
    {
        if (transform.childCount < 2) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < transform.childCount; i++)
        {
            var pos = transform.GetChild(i).position;
            Gizmos.DrawSphere(pos, 0.3f);

            if (i < transform.childCount - 1)
            {
                var next = transform.GetChild(i + 1).position;
                Gizmos.DrawLine(pos, next);
            }
        }

        // 시작점 표시
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.GetChild(0).position, 0.5f);

        // 기지(끝점) 표시
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.GetChild(transform.childCount - 1).position, 0.5f);
    }
}
