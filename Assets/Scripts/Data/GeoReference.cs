using UnityEngine;

[CreateAssetMenu(fileName = "GeoReference", menuName = "CampusNav/Geo Reference")]
public class GeoReference : ScriptableObject
{
    [Header("GPS Anchor Point")]
    public double anchorLatitude = 54.6872;
    public double anchorLongitude = 25.2797;

    [Header("Corresponding Unity Position")]
    public Vector3 anchorWorldPosition = Vector3.zero;

    [Header("Orientation")]
    [Tooltip("Degrees clockwise from Unity +Z to geographic North")]
    public float northRotationOffset;

    [Header("Scale")]
    [Tooltip("Meters per Unity unit (usually 1.0)")]
    public float metersPerUnit = 1f;

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
}
