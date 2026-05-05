using UnityEngine;
using Unity.AI.Navigation;

[RequireComponent(typeof(NavMeshLink))]
public class ElevatorLink : MonoBehaviour
{
    [SerializeField] private string buildingCode;
    [SerializeField] private int fromFloor;
    [SerializeField] private int toFloor;
    [SerializeField] private bool requiresElevator = true;
    [SerializeField] private bool isAccessible = true;

    NavMeshLink navLink;

    public string BuildingCode => buildingCode;
    public int FromFloor => fromFloor;
    public int ToFloor => toFloor;
    public bool RequiresElevator => requiresElevator;
    public bool IsAccessible => isAccessible;

    public void Initialize(string bld, int f1, int f2, bool elevator = true)
    {
        buildingCode = bld;
        fromFloor = f1;
        toFloor = f2;
        requiresElevator = elevator;
    }

    void Awake()
    {
        navLink = GetComponent<NavMeshLink>();
    }

    public void SetEnabled(bool enabled)
    {
        if (navLink != null)
            navLink.enabled = enabled;
    }
}
