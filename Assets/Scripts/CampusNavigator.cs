using UnityEngine;
using UnityEngine.AI;

public class CampusNavigator : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private RouteDatabase routeDatabase;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform arrow;

    RoutePoint currentDestination;

    public RouteDatabase Database => routeDatabase;
    public RoutePoint CurrentDestination => currentDestination;
    public bool HasPath => agent != null && agent.hasPath;

    public void SetDestination(RoutePoint point)
    {
        if (point == null || agent == null) return;
        currentDestination = point;
        agent.SetDestination(point.worldPosition);
        UpdatePath();
    }

    public void SetDestination(int index)
    {
        if (routeDatabase == null || routeDatabase.allPoints == null) return;
        if (index < 0 || index >= routeDatabase.allPoints.Count) return;
        SetDestination(routeDatabase.allPoints[index]);
    }

    public void SetDestinationByName(string pointName)
    {
        if (routeDatabase == null) return;
        var point = routeDatabase.FindByName(pointName);
        if (point != null) SetDestination(point);
    }

    public void ClearPath()
    {
        if (agent != null) agent.ResetPath();
        currentDestination = null;
        if (lineRenderer != null) lineRenderer.positionCount = 0;
        if (arrow != null) arrow.gameObject.SetActive(false);
    }

    void Update()
    {
        if (agent == null || lineRenderer == null) return;
        if (!agent.hasPath) return;
        UpdatePath();
    }

    void UpdatePath()
    {
        var corners = agent.path.corners;
        if (corners == null || corners.Length == 0)
        {
            lineRenderer.positionCount = 0;
            if (arrow != null) arrow.gameObject.SetActive(false);
            return;
        }

        lineRenderer.positionCount = corners.Length;
        lineRenderer.SetPositions(corners);

        if (arrow != null && corners.Length > 0)
        {
            arrow.position = corners[0];
            if (corners.Length > 1)
            {
                Vector3 direction = (corners[1] - corners[0]).normalized;
                if (direction.sqrMagnitude > 0f)
                    arrow.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
            arrow.gameObject.SetActive(true);
        }
    }
}
