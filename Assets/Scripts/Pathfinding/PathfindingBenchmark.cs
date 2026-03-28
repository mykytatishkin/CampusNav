using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PathfindingBenchmark : MonoBehaviour
{
    [SerializeField] private RouteDatabase routeDatabase;
    [SerializeField] private bool useElevators = true;

    [Header("Graph Settings")]
    [SerializeField] private float maxConnectionDistance = 15f;

    [Header("Visualization")]
    [SerializeField] private bool showAStarPath = true;
    [SerializeField] private bool showDijkstraPath;
    [SerializeField] private bool showNavMeshPath;

    [Header("Path Colors")]
    [SerializeField] private Color aStarColor = Color.green;
    [SerializeField] private Color dijkstraColor = Color.blue;
    [SerializeField] private Color navMeshColor = Color.cyan;

    NavigationGraph graph;
    AStarPathfinder astar;
    DijkstraPathfinder dijkstra;
    NavMeshPathfinder navMeshPf;

    List<PathResult> lastResults = new();
    List<BenchmarkEntry> benchmarkLog = new();

    public struct BenchmarkEntry
    {
        public string FromLabel;
        public string ToLabel;
        public PathResult NavMeshResult;
        public PathResult AStarResult;
        public PathResult DijkstraResult;
    }

    public List<BenchmarkEntry> BenchmarkLog => benchmarkLog;
    public List<PathResult> LastResults => lastResults;

    void Start()
    {
        RebuildGraph();
    }

    public void RebuildGraph()
    {
        if (routeDatabase == null || routeDatabase.allPoints.Count == 0)
        {
            Debug.LogWarning("[Benchmark] RouteDatabase is empty. Graph not built.");
            return;
        }

        graph = new NavigationGraph();
        graph.BuildFromRoutePoints(routeDatabase.allPoints, maxConnectionDistance);

        astar = new AStarPathfinder(graph, useElevators);
        dijkstra = new DijkstraPathfinder(graph, useElevators);
        navMeshPf = new NavMeshPathfinder();

        Debug.Log($"[Benchmark] Graph built: {graph.Nodes.Count} nodes");
    }

    public List<PathResult> CompareAlgorithms(Vector3 from, Vector3 to)
    {
        lastResults.Clear();

        if (graph == null || graph.Nodes.Count == 0)
        {
            Debug.LogWarning("[Benchmark] Graph not built yet.");
            return lastResults;
        }

        lastResults.Add(navMeshPf.FindPath(from, to));
        lastResults.Add(astar.FindPath(from, to));
        lastResults.Add(dijkstra.FindPath(from, to));

        // Log
        var fromNode = graph.FindNearest(from);
        var toNode = graph.FindNearest(to);

        var entry = new BenchmarkEntry
        {
            FromLabel = fromNode?.Label ?? from.ToString(),
            ToLabel = toNode?.Label ?? to.ToString(),
            NavMeshResult = lastResults[0],
            AStarResult = lastResults[1],
            DijkstraResult = lastResults[2]
        };
        benchmarkLog.Add(entry);

        LogComparison(entry);
        return lastResults;
    }

    public void RunFullBenchmark(int samplePairs = 10)
    {
        if (routeDatabase == null || routeDatabase.allPoints.Count < 2) return;

        benchmarkLog.Clear();
        var points = routeDatabase.allPoints;
        int pairs = Mathf.Min(samplePairs, points.Count * (points.Count - 1) / 2);

        var tested = new HashSet<(int, int)>();
        int attempts = 0;

        while (benchmarkLog.Count < pairs && attempts < pairs * 10)
        {
            attempts++;
            int a = Random.Range(0, points.Count);
            int b = Random.Range(0, points.Count);
            if (a == b) continue;
            if (points[a] == null || points[b] == null) continue;

            var key = (Mathf.Min(a, b), Mathf.Max(a, b));
            if (tested.Contains(key)) continue;
            tested.Add(key);

            CompareAlgorithms(points[a].worldPosition, points[b].worldPosition);
        }

        Debug.Log($"[Benchmark] Full benchmark complete: {benchmarkLog.Count} route pairs tested.");
        PrintSummaryTable();
    }

    void LogComparison(BenchmarkEntry entry)
    {
        Debug.Log($"[Benchmark] {entry.FromLabel} -> {entry.ToLabel}\n" +
            $"  NavMesh: {FormatResult(entry.NavMeshResult)}\n" +
            $"  A*:      {FormatResult(entry.AStarResult)}\n" +
            $"  Dijkstra:{FormatResult(entry.DijkstraResult)}");
    }

    static string FormatResult(PathResult r)
    {
        if (!r.Found) return "NOT FOUND";
        return $"dist={r.TotalDistance:F1}m, nodes={r.NodesExplored}, time={r.ElapsedMs:F3}ms, waypoints={r.Waypoints.Count}";
    }

    public void PrintSummaryTable()
    {
        if (benchmarkLog.Count == 0)
        {
            Debug.Log("[Benchmark] No data to summarize.");
            return;
        }

        float navMeshAvgTime = 0, astarAvgTime = 0, dijkstraAvgTime = 0;
        float navMeshAvgDist = 0, astarAvgDist = 0, dijkstraAvgDist = 0;
        int navMeshAvgNodes = 0, astarAvgNodes = 0, dijkstraAvgNodes = 0;
        int navMeshFound = 0, astarFound = 0, dijkstraFound = 0;

        foreach (var e in benchmarkLog)
        {
            if (e.NavMeshResult.Found) { navMeshAvgTime += (float)e.NavMeshResult.ElapsedMs; navMeshAvgDist += e.NavMeshResult.TotalDistance; navMeshFound++; }
            if (e.AStarResult.Found) { astarAvgTime += (float)e.AStarResult.ElapsedMs; astarAvgDist += e.AStarResult.TotalDistance; astarAvgNodes += e.AStarResult.NodesExplored; astarFound++; }
            if (e.DijkstraResult.Found) { dijkstraAvgTime += (float)e.DijkstraResult.ElapsedMs; dijkstraAvgDist += e.DijkstraResult.TotalDistance; dijkstraAvgNodes += e.DijkstraResult.NodesExplored; dijkstraFound++; }
        }

        int total = benchmarkLog.Count;

        string table = "\n=== PATHFINDING COMPARISON TABLE ===\n" +
            $"Total route pairs tested: {total}\n\n" +
            $"{"Algorithm",-16} | {"Found",-8} | {"Avg Time (ms)",-14} | {"Avg Dist (m)",-13} | {"Avg Nodes",-10}\n" +
            new string('-', 72) + "\n" +
            FormatRow("NavMesh", navMeshFound, total, navMeshAvgTime, navMeshAvgDist, -1) +
            FormatRow("A*", astarFound, total, astarAvgTime, astarAvgDist, astarAvgNodes) +
            FormatRow("Dijkstra", dijkstraFound, total, dijkstraAvgTime, dijkstraAvgDist, dijkstraAvgNodes) +
            "\n=== ALGORITHM PROPERTIES ===\n" +
            "NavMesh (Unity): Optimized C++ mesh-based, guaranteed shortest on navmesh, no node count\n" +
            "A*:              Heuristic-guided graph search, explores fewer nodes than Dijkstra\n" +
            "Dijkstra:        Exhaustive shortest-path, explores all reachable nodes up to target\n";

        Debug.Log(table);
    }

    static string FormatRow(string name, int found, int total, float totalTime, float totalDist, int totalNodes)
    {
        string avgTime = found > 0 ? $"{totalTime / found:F3}" : "N/A";
        string avgDist = found > 0 ? $"{totalDist / found:F1}" : "N/A";
        string avgNodes = totalNodes >= 0 && found > 0 ? $"{totalNodes / found}" : "N/A";
        return $"{name,-16} | {found}/{total,-5} | {avgTime,-14} | {avgDist,-13} | {avgNodes,-10}\n";
    }

    void OnDrawGizmos()
    {
        if (lastResults == null) return;

        for (int r = 0; r < lastResults.Count; r++)
        {
            var result = lastResults[r];
            if (!result.Found || result.Waypoints.Count < 2) continue;

            bool show = r switch
            {
                0 => showNavMeshPath,
                1 => showAStarPath,
                2 => showDijkstraPath,
                _ => false
            };
            if (!show) continue;

            Color col = r switch
            {
                0 => navMeshColor,
                1 => aStarColor,
                2 => dijkstraColor,
                _ => Color.white
            };

            Gizmos.color = col;
            float yOffset = r * 0.3f;
            for (int i = 1; i < result.Waypoints.Count; i++)
            {
                Vector3 a = result.Waypoints[i - 1] + Vector3.up * yOffset;
                Vector3 b = result.Waypoints[i] + Vector3.up * yOffset;
                Gizmos.DrawLine(a, b);
                Gizmos.DrawSphere(a, 0.3f);
            }
            Gizmos.DrawSphere(result.Waypoints[^1] + Vector3.up * yOffset, 0.3f);
        }

        // Draw graph nodes
        if (graph != null)
        {
            Gizmos.color = new Color(1, 1, 0, 0.2f);
            foreach (var node in graph.Nodes)
            {
                Gizmos.DrawWireSphere(node.Position, 0.5f);
                foreach (var edge in node.Edges)
                    Gizmos.DrawLine(node.Position, edge.To.Position);
            }
        }
    }
}
