using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CampusData", menuName = "CampusNav/Campus Data")]
public class CampusData : ScriptableObject
{
    public string campusName = "Saulėtekio Rūmai";
    public string campusCode = "SR";
    public string address = "Saulėtekio al. 11, Vilnius";

    [Header("GPS Bounds")]
    public double boundsMinLat = 54.6875;
    public double boundsMaxLat = 54.6910;
    public double boundsMinLon = 25.2860;
    public double boundsMaxLon = 25.2960;

    [Header("GPS Center")]
    public double centerLat = 54.6893;
    public double centerLon = 25.2910;

    [Header("Buildings")]
    public List<BuildingInfo> buildings = new();

    [Header("Route Database")]
    public RouteDatabase routeDatabase;

    public bool IsNearCampus(double lat, double lon, double marginDegrees = 0.002)
    {
        return lat >= boundsMinLat - marginDegrees && lat <= boundsMaxLat + marginDegrees
            && lon >= boundsMinLon - marginDegrees && lon <= boundsMaxLon + marginDegrees;
    }
}

[System.Serializable]
public class BuildingInfo
{
    public string code;
    public string displayName;
    public int floors;
    public Vector3 worldPosition;
    public string description;
}
