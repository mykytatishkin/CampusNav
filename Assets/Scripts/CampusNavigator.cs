using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CampusNavigator : MonoBehaviour
{
    public enum PathfindingMode { NavMesh, AStar, Dijkstra }

    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private RouteDatabase routeDatabase;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform arrow;
    [SerializeField] private PathfindingMode pathfindingMode = PathfindingMode.NavMesh;
    [SerializeField] private bool useElevators = true;

    [Header("Graph Settings (A*/Dijkstra)")]
    [SerializeField] private float maxConnectionDistance = 15f;

    RoutePoint currentDestination;
    NavigationGraph graph;
    List<Vector3> graphPath;

    public RouteDatabase Database => routeDatabase;
    public RoutePoint CurrentDestination => currentDestination;
    public bool HasPath => agent != null && (agent.hasPath || (graphPath != null && graphPath.Count > 1));
    public PathfindingMode ActiveMode => pathfindingMode;
    public NavMeshAgent Agent => agent;

    public void SetRouteDatabase(RouteDatabase database)
    {
        routeDatabase = database;
        BuildGraph();
    }

    public void SetPathfindingMode(PathfindingMode mode)
    {
        pathfindingMode = mode;
        // Re-route if we have a destination
        if (currentDestination != null)
            SetDestination(currentDestination);
    }

    void Start()
    {
        BuildGraph();
    }

    void BuildGraph()
    {
        if (routeDatabase == null || routeDatabase.allPoints.Count == 0) return;
        graph = new NavigationGraph();
        graph.BuildFromRoutePoints(routeDatabase.allPoints, maxConnectionDistance);
    }

    public void SetDestination(RoutePoint point)
    {
        if (point == null || agent == null) return;
        currentDestination = point;
        graphPath = null;

        if (pathfindingMode == PathfindingMode.NavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(point.worldPosition);
        }
        else
        {
            if (graph == null) BuildGraph();
            if (graph == null || graph.Nodes.Count == 0) return;

            IPathfinder pathfinder = pathfindingMode == PathfindingMode.AStar
                ? new AStarPathfinder(graph, useElevators)
                : new DijkstraPathfinder(graph, useElevators);

            var result = pathfinder.FindPath(agent.transform.position, point.worldPosition);
            if (result.Found)
            {
                graphPath = result.Waypoints;
                agent.ResetPath();
                agent.isStopped = false;
                // Move along waypoints using NavMeshAgent for actual movement
                if (graphPath.Count > 0)
                    agent.SetDestination(graphPath[^1]);
            }
        }

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
        graphPath = null;
        if (lineRenderer != null) lineRenderer.positionCount = 0;
        if (arrow != null) arrow.gameObject.SetActive(false);
    }

    void Update()
    {
        if (agent == null || lineRenderer == null) return;
        if (!HasPath) return;
        UpdatePath();
    }

    void UpdatePath()
    {
        Vector3[] corners;

        if (graphPath != null && graphPath.Count > 1)
        {
            corners = graphPath.ToArray();
        }
        else
        {
            corners = agent.path.corners;
        }

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

    public void SetUseElevators(bool value)
    {
        useElevators = value;
        if (currentDestination != null && pathfindingMode != PathfindingMode.NavMesh)
            SetDestination(currentDestination);
    }
}
