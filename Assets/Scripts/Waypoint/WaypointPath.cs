using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class WaypointPath : MonoBehaviour
{
    [Header("경로 설정")]
    [SerializeField] private float pathWidth = 1f;
    [SerializeField] private Color pathColor = new(0.3f, 0.25f, 0.2f, 1f);

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

        var lr = GetComponent<LineRenderer>();
        lr.positionCount = transform.childCount;
        lr.startWidth = pathWidth;
        lr.endWidth = pathWidth;
        lr.useWorldSpace = true;
        lr.sortingLayerName = GameConstants.SortPath;
        lr.sortingOrder = 0;
        lr.numCornerVertices = 4;
        lr.numCapVertices = 4;

        // 단색 머티리얼
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = pathColor;
        lr.endColor = pathColor;

        for (int i = 0; i < transform.childCount; i++)
        {
            lr.SetPosition(i, transform.GetChild(i).position);
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
