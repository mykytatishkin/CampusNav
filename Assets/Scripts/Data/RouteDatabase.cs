using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "RouteDatabase", menuName = "CampusNav/Route Database")]
public class RouteDatabase : ScriptableObject
{
    public List<RoutePoint> allPoints = new();

    public RoutePoint FindByName(string pointName)
    {
        return allPoints.FirstOrDefault(p => p != null && p.pointName == pointName);
    }

    public List<RoutePoint> FindByBuilding(string buildingCode)
    {
        return allPoints.Where(p => p != null && p.buildingCode == buildingCode).ToList();
    }

    public List<RoutePoint> FindByCategory(RoutePointCategory category)
    {
        return allPoints.Where(p => p != null && p.category == category).ToList();
    }

    public List<RoutePoint> FindByFloor(string buildingCode, int floor)
    {
        return allPoints.Where(p => p != null && p.buildingCode == buildingCode && p.floor == floor).ToList();
    }

    public List<RoutePoint> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return allPoints.Where(p => p != null).ToList();
        string q = query.ToLowerInvariant();
        return allPoints.Where(p => p != null && MatchesQuery(p, q)).ToList();
    }

    static bool MatchesQuery(RoutePoint p, string q)
    {
        if (!string.IsNullOrEmpty(p.pointName) && p.pointName.ToLowerInvariant().Contains(q)) return true;
        if (!string.IsNullOrEmpty(p.buildingCode) && p.buildingCode.ToLowerInvariant().Contains(q)) return true;
        if (!string.IsNullOrEmpty(p.roomNumber) && p.roomNumber.ToLowerInvariant().Contains(q)) return true;
        if (!string.IsNullOrEmpty(p.description) && p.description.ToLowerInvariant().Contains(q)) return true;
        return false;
    }

    public List<string> GetAllBuildingCodes()
    {
        return allPoints
            .Where(p => p != null)
            .Select(p => p.buildingCode)
            .Distinct()
            .OrderBy(c => c)
            .ToList();
    }
}
