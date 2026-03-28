using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class DijkstraPathfinder : IPathfinder
{
    readonly NavigationGraph graph;
    readonly bool useElevators;

    public string AlgorithmName => "Dijkstra";

    public DijkstraPathfinder(NavigationGraph graph, bool useElevators = true)
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
        float[] dist = new float[count];
        int[] prev = new int[count];
        bool[] visited = new bool[count];

        for (int i = 0; i < count; i++)
        {
            dist[i] = float.MaxValue;
            prev[i] = -1;
        }
        dist[startNode.Id] = 0;

        // Simple priority queue using sorted set
        var pq = new SortedSet<(float cost, int id)>(Comparer<(float cost, int id)>.Create(
            (a, b) => a.cost != b.cost ? a.cost.CompareTo(b.cost) : a.id.CompareTo(b.id)));
        pq.Add((0, startNode.Id));

        int nodesExplored = 0;

        while (pq.Count > 0)
        {
            var (currentCost, currentId) = pq.Min;
            pq.Remove(pq.Min);

            if (visited[currentId]) continue;
            visited[currentId] = true;
            nodesExplored++;

            if (currentId == endNode.Id) break;

            var current = graph.Nodes[currentId];
            foreach (var edge in current.Edges)
            {
                if (!useElevators && edge.RequiresElevator) continue;

                int neighborId = edge.To.Id;
                if (visited[neighborId]) continue;

                float newDist = dist[currentId] + edge.Cost;
                if (newDist < dist[neighborId])
                {
                    pq.Remove((dist[neighborId], neighborId));
                    dist[neighborId] = newDist;
                    prev[neighborId] = currentId;
                    pq.Add((newDist, neighborId));
                }
            }
        }

        sw.Stop();

        if (!visited[endNode.Id])
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
}
