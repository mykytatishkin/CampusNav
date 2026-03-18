using UnityEngine;

[CreateAssetMenu(fileName = "GeoReference", menuName = "CampusNav/Geo Reference")]
public class GeoReference : ScriptableObject
{
    [Header("GPS Anchor Point — VGTU Saulėtekio Rūmai main entrance SRK-I")]
    [Tooltip("Latitude of the anchor point (main entrance SRK-I)")]
    public double anchorLatitude = 54.6898;

    [Tooltip("Longitude of the anchor point (main entrance SRK-I)")]
    public double anchorLongitude = 25.2888;

    [Header("Corresponding Unity Position")]
    [Tooltip("The Unity world position that corresponds to the GPS anchor")]
    public Vector3 anchorWorldPosition = new(-50, 0, 69);

    [Header("Orientation")]
    [Tooltip("Degrees clockwise from Unity +Z to geographic North. Saulėtekio buildings are rotated ~25° from north.")]
    public float northRotationOffset = 25f;

    [Header("Scale")]
    [Tooltip("Meters per Unity unit (usually 1.0)")]
    public float metersPerUnit = 1f;

    [Header("Campus Boundary (GPS rectangle)")]
    public double campusBoundsMinLat = 54.6875;
    public double campusBoundsMaxLat = 54.6910;
    public double campusBoundsMinLon = 25.2860;
    public double campusBoundsMaxLon = 25.2960;

    public Vector3 GpsToWorld(double latitude, double longitude)
    {
        double dLat = latitude - anchorLatitude;
        double dLon = longitude - anchorLongitude;

        double metersPerDegreeLat = 111320.0;
        double metersPerDegreeLon = 111320.0 * System.Math.Cos(anchorLatitude * System.Math.PI / 180.0);

        float offsetNorth = (float)(dLat * metersPerDegreeLat) / metersPerUnit;
        float offsetEast = (float)(dLon * metersPerDegreeLon) / metersPerUnit;

        float rad = -northRotationOffset * Mathf.Deg2Rad;
        float x = offsetEast * Mathf.Cos(rad) - offsetNorth * Mathf.Sin(rad);
        float z = offsetEast * Mathf.Sin(rad) + offsetNorth * Mathf.Cos(rad);

        return anchorWorldPosition + new Vector3(x, 0, z);
    }

    public (double lat, double lon) WorldToGps(Vector3 worldPosition)
    {
        Vector3 offset = worldPosition - anchorWorldPosition;

        float rad = northRotationOffset * Mathf.Deg2Rad;
        float east = offset.x * Mathf.Cos(rad) - offset.z * Mathf.Sin(rad);
        float north = offset.x * Mathf.Sin(rad) + offset.z * Mathf.Cos(rad);

        east *= metersPerUnit;
        north *= metersPerUnit;

        double metersPerDegreeLat = 111320.0;
        double metersPerDegreeLon = 111320.0 * System.Math.Cos(anchorLatitude * System.Math.PI / 180.0);

        double lat = anchorLatitude + north / metersPerDegreeLat;
        double lon = anchorLongitude + east / metersPerDegreeLon;

        return (lat, lon);
    }

    public bool IsInsideCampus(double lat, double lon)
    {
        return lat >= campusBoundsMinLat && lat <= campusBoundsMaxLat
            && lon >= campusBoundsMinLon && lon <= campusBoundsMaxLon;
    }
}
