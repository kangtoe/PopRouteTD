using System;
using UnityEngine;

public class WaypointFollower : MonoBehaviour
{
    private Vector3[] waypoints;
    private int currentIndex;
    private float speed;
    private float totalPathLength;
    private float distanceTraveled;

    public float Progress => totalPathLength > 0 ? distanceTraveled / totalPathLength : 0f;
    public bool IsActive { get; private set; }

    public event Action OnReachedEnd;

    public void Initialize(WaypointPath path, float moveSpeed)
    {
        waypoints = path.GetWaypoints();
        speed = moveSpeed;
        currentIndex = 0;
        distanceTraveled = 0f;
        IsActive = true;

        totalPathLength = CalculateTotalLength();

        if (waypoints.Length > 0)
        {
            transform.position = waypoints[0];
        }
    }

    public void InitializeAtProgress(WaypointPath path, float moveSpeed, int waypointIndex, float fractionToNext)
    {
        waypoints = path.GetWaypoints();
        speed = moveSpeed;
        currentIndex = waypointIndex;
        IsActive = true;

        totalPathLength = CalculateTotalLength();

        // 현재 위치를 waypointIndex와 다음 지점 사이의 보간으로 설정
        if (waypointIndex < waypoints.Length - 1)
        {
            transform.position = Vector3.Lerp(waypoints[waypointIndex], waypoints[waypointIndex + 1], fractionToNext);
        }
        else
        {
            transform.position = waypoints[waypoints.Length - 1];
        }

        // 이동한 거리 계산
        distanceTraveled = 0f;
        for (int i = 0; i < waypointIndex && i < waypoints.Length - 1; i++)
        {
            distanceTraveled += Vector3.Distance(waypoints[i], waypoints[i + 1]);
        }
        if (waypointIndex < waypoints.Length - 1)
        {
            distanceTraveled += Vector3.Distance(waypoints[waypointIndex], waypoints[waypointIndex + 1]) * fractionToNext;
        }
    }

    private void Update()
    {
        if (!IsActive || waypoints == null || waypoints.Length == 0) return;

        MoveAlongPath();
    }

    private void MoveAlongPath()
    {
        if (currentIndex >= waypoints.Length - 1)
        {
            ReachEnd();
            return;
        }

        var target = waypoints[currentIndex + 1];
        var step = speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target, step);
        distanceTraveled += step;

        if (Vector3.Distance(transform.position, target) < 0.01f)
        {
            currentIndex++;
            if (currentIndex >= waypoints.Length - 1)
            {
                ReachEnd();
            }
        }
    }

    private void ReachEnd()
    {
        IsActive = false;
        OnReachedEnd?.Invoke();
    }

    public int CurrentWaypointIndex => currentIndex;

    public float FractionToNextWaypoint
    {
        get
        {
            if (waypoints == null || currentIndex >= waypoints.Length - 1) return 0f;
            float segmentLength = Vector3.Distance(waypoints[currentIndex], waypoints[currentIndex + 1]);
            if (segmentLength < 0.001f) return 0f;
            float distFromCurrent = Vector3.Distance(waypoints[currentIndex], transform.position);
            return distFromCurrent / segmentLength;
        }
    }

    private float CalculateTotalLength()
    {
        float length = 0f;
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            length += Vector3.Distance(waypoints[i], waypoints[i + 1]);
        }
        return length;
    }
}
