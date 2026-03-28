using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class AStarPathfinder : IPathfinder
{
    readonly NavigationGraph graph;
    readonly bool useElevators;

    public string AlgorithmName => "A*";

    public AStarPathfinder(NavigationGraph graph, bool useElevators = true)
    {
        this.graph = graph;
        this.useElevators = useElevators;
    }

    public PathResult FindPath(Vector3 start, Vector3 end)
    {
        var sw = Stopwatch.StartNew();

        var startNode = graph.FindNearest(start);
        var endNode = graph.FindNearest(end);

        if (startNode == null || endNode == null)
            return PathResult.Failed(AlgorithmName);

        int count = graph.Nodes.Count;
        float[] gScore = new float[count];
        float[] fScore = new float[count];
        int[] prev = new int[count];
        bool[] closed = new bool[count];

        for (int i = 0; i < count; i++)
        {
            gScore[i] = float.MaxValue;
            fScore[i] = float.MaxValue;
            prev[i] = -1;
        }

        gScore[startNode.Id] = 0;
        fScore[startNode.Id] = Heuristic(startNode.Position, endNode.Position);

        var openSet = new SortedSet<(float f, int id)>(Comparer<(float f, int id)>.Create(
            (a, b) => a.f != b.f ? a.f.CompareTo(b.f) : a.id.CompareTo(b.id)));
        openSet.Add((fScore[startNode.Id], startNode.Id));

        int nodesExplored = 0;

        while (openSet.Count > 0)
        {
            var (_, currentId) = openSet.Min;
            openSet.Remove(openSet.Min);

            if (closed[currentId]) continue;
            closed[currentId] = true;
            nodesExplored++;

            if (currentId == endNode.Id) break;

            var current = graph.Nodes[currentId];
            foreach (var edge in current.Edges)
            {
                if (!useElevators && edge.RequiresElevator) continue;

                int neighborId = edge.To.Id;
                if (closed[neighborId]) continue;

                float tentativeG = gScore[currentId] + edge.Cost;
                if (tentativeG < gScore[neighborId])
                {
                    openSet.Remove((fScore[neighborId], neighborId));
                    prev[neighborId] = currentId;
                    gScore[neighborId] = tentativeG;
                    fScore[neighborId] = tentativeG + Heuristic(edge.To.Position, endNode.Position);
                    openSet.Add((fScore[neighborId], neighborId));
                }
            }
        }

        sw.Stop();

        if (!closed[endNode.Id])
        {
            var fail = PathResult.Failed(AlgorithmName);
            fail.NodesExplored = nodesExplored;
            fail.ElapsedMs = sw.Elapsed.TotalMilliseconds;
            return fail;
        }

        // Reconstruct path
        var path = new List<Vector3>();
        int at = endNode.Id;
        while (at != -1)
        {
            path.Add(graph.Nodes[at].Position);
            at = prev[at];
        }
        path.Reverse();

        float totalDist = 0;
        for (int i = 1; i < path.Count; i++)
            totalDist += Vector3.Distance(path[i - 1], path[i]);

        return new PathResult
        {
            Found = true,
            Waypoints = path,
            TotalDistance = totalDist,
            NodesExplored = nodesExplored,
            ElapsedMs = sw.Elapsed.TotalMilliseconds,
            AlgorithmName = AlgorithmName
        };
    }

    static float Heuristic(Vector3 a, Vector3 b)
    {
        // Euclidean distance — admissible and consistent for 3D navigation
        return Vector3.Distance(a, b);
    }
}
