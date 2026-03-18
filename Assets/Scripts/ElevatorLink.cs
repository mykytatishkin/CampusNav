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
