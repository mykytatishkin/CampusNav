using System.Collections.Generic;
using UnityEngine;

public class NavigationGraph
{
    public class Node
    {
        public int Id;
        public Vector3 Position;
        public string Label;
        public int Floor;
        public string BuildingCode;
        public bool IsElevator;
        public List<Edge> Edges = new();
    }

    public class Edge
    {
        public Node From;
        public Node To;
        public float Cost;
        public bool RequiresElevator;
    }

    public List<Node> Nodes { get; } = new();
    readonly Dictionary<Vector3Int, Node> spatialIndex = new();

    static Vector3Int Quantize(Vector3 pos) =>
        new(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), Mathf.RoundToInt(pos.z));

    public Node AddNode(Vector3 position, string label = "", int floor = 0,
        string buildingCode = "", bool isElevator = false)
    {
        var key = Quantize(position);
        if (spatialIndex.TryGetValue(key, out var existing))
            return existing;

        var node = new Node
        {
            Id = Nodes.Count,
            Position = position,
            Label = label,
            Floor = floor,
            BuildingCode = buildingCode,
            IsElevator = isElevator
        };
        Nodes.Add(node);
        spatialIndex[key] = node;
        return node;
    }

    public Edge AddEdge(Node from, Node to, bool requiresElevator = false)
    {
        float cost = Vector3.Distance(from.Position, to.Position);
        return AddEdge(from, to, cost, requiresElevator);
    }

    public Edge AddEdge(Node from, Node to, float cost, bool requiresElevator = false)
    {
        var edge = new Edge { From = from, To = to, Cost = cost, RequiresElevator = requiresElevator };
        from.Edges.Add(edge);

        // Bidirectional
        var reverseEdge = new Edge { From = to, To = from, Cost = cost, RequiresElevator = requiresElevator };
        to.Edges.Add(reverseEdge);

        return edge;
    }

    public Node FindNearest(Vector3 position)
    {
        Node best = null;
        float bestDist = float.MaxValue;
        foreach (var node in Nodes)
        {
            float d = Vector3.SqrMagnitude(node.Position - position);
            if (d < bestDist)
            {
                bestDist = d;
                best = node;
            }
        }
        return best;
    }

    public void BuildFromRoutePoints(List<RoutePoint> points, float maxConnectionDistance = 15f,
        bool connectElevatorFloors = true)
    {
        Nodes.Clear();
        spatialIndex.Clear();

        // Create nodes
        foreach (var rp in points)
        {
            if (rp == null) continue;
            AddNode(rp.worldPosition, rp.pointName, rp.floor, rp.buildingCode,
                rp.category == RoutePointCategory.Elevator);
        }

        // Connect nodes
        for (int i = 0; i < Nodes.Count; i++)
        {
            for (int j = i + 1; j < Nodes.Count; j++)
            {
                var a = Nodes[i];
                var b = Nodes[j];

                bool aOutdoor = string.IsNullOrEmpty(a.BuildingCode);
                bool bOutdoor = string.IsNullOrEmpty(b.BuildingCode);
                bool sameBuilding = !aOutdoor && !bOutdoor && a.BuildingCode == b.BuildingCode;
                bool sameFloor = a.Floor == b.Floor;
                float dist = Vector3.Distance(a.Position, b.Position);

                // Both outdoor / corridor nodes — connect generously
                if (aOutdoor && bOutdoor && dist < maxConnectionDistance * 3f)
                {
                    AddEdge(a, b);
                    continue;
                }

                // One outdoor, one building — connect if nearby (entrance links)
                if ((aOutdoor || bOutdoor) && dist < maxConnectionDistance * 2f)
                {
                    AddEdge(a, b, dist * 1.1f);
                    continue;
                }

                // Same building, same floor
                if (sameBuilding && sameFloor && dist < maxConnectionDistance)
                {
                    AddEdge(a, b);
                    continue;
                }

                // Same building, different floor via elevator/stairs
                if (sameBuilding && !sameFloor && connectElevatorFloors
                    && (a.IsElevator || b.IsElevator) && dist < maxConnectionDistance * 2)
                {
                    AddEdge(a, b, dist * 1.5f, true);
                    continue;
                }

                // Different buildings, same floor — corridor connections
                if (!sameBuilding && !aOutdoor && !bOutdoor && sameFloor
                    && dist < maxConnectionDistance * 1.5f)
                {
                    AddEdge(a, b, dist * 1.2f);
                }
            }
        }
    }
}
