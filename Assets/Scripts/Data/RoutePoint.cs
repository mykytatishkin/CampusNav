using UnityEngine;

[CreateAssetMenu(fileName = "RoutePoint", menuName = "CampusNav/Route Point")]
public class RoutePoint : ScriptableObject
{
    public string pointName;
    public string buildingCode;
    public int floor;
    public string roomNumber;
    public Vector3 worldPosition;
    public RoutePointCategory category;
    [TextArea] public string description;
    public bool isAccessible = true;
}
