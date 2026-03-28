using System.Collections.Generic;
using UnityEngine;

public interface IPathfinder
{
    string AlgorithmName { get; }
    PathResult FindPath(Vector3 start, Vector3 end);
}

public struct PathResult
{
    public bool Found;
    public List<Vector3> Waypoints;
    public float TotalDistance;
    public int NodesExplored;
    public double ElapsedMs;
    public string AlgorithmName;

    public static PathResult Failed(string algorithm) => new()
    {
        Found = false,
        Waypoints = new List<Vector3>(),
        TotalDistance = 0,
        NodesExplored = 0,
        ElapsedMs = 0,
        AlgorithmName = algorithm
    };
}
