using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshPathfinder : IPathfinder
{
    public string AlgorithmName => "NavMesh (Unity)";

    public PathResult FindPath(Vector3 start, Vector3 end)
    {
        var sw = Stopwatch.StartNew();

        var path = new NavMeshPath();
        bool found = NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path);

        sw.Stop();

        if (!found || path.status != NavMeshPathStatus.PathComplete)
        {
            return new PathResult
            {
                Found = false,
                Waypoints = new List<Vector3>(),
                TotalDistance = 0,
                NodesExplored = -1, // NavMesh doesn't expose this
                ElapsedMs = sw.Elapsed.TotalMilliseconds,
                AlgorithmName = AlgorithmName
            };
        }

        var waypoints = new List<Vector3>(path.corners);
        float totalDist = 0;
        for (int i = 1; i < waypoints.Count; i++)
            totalDist += Vector3.Distance(waypoints[i - 1], waypoints[i]);

        return new PathResult
        {
            Found = true,
            Waypoints = waypoints,
            TotalDistance = totalDist,
            NodesExplored = -1, // NavMesh is a black box
            ElapsedMs = sw.Elapsed.TotalMilliseconds,
            AlgorithmName = AlgorithmName
        };
    }
}
