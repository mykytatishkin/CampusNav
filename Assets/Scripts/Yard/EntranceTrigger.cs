using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Placed at building entrances. When the navigating player arrives,
/// teleports them to the building's indoor area via IndoorManager
/// and switches AppFlowManager to IndoorSelect state.
/// </summary>
public class EntranceTrigger : MonoBehaviour
{
    [Header("Building Info")]
    public string buildingCode;
    public string buildingDisplayName;
    public int totalFloors;

    [Header("Visual")]
    [SerializeField] private Color gizmoColor = new(0.2f, 0.9f, 0.2f, 0.5f);

    AppFlowManager flowManager;
    IndoorManager indoorManager;

    void Start()
    {
        flowManager = FindAnyObjectByType<AppFlowManager>();
        indoorManager = FindAnyObjectByType<IndoorManager>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;
        if (flowManager == null || flowManager.CurrentState != AppFlowManager.AppState.Navigation) return;

        // Teleport player into the indoor area
        if (indoorManager != null)
            indoorManager.EnterBuilding(buildingCode);

        // Switch to indoor selection UI
        flowManager.OnBuildingEntered(buildingCode);
    }

    static bool IsPlayer(Collider col)
    {
        return col.CompareTag("Player") || col.GetComponent<NavMeshAgent>() != null;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        var col = GetComponent<BoxCollider>();
        if (col != null)
            Gizmos.DrawWireCube(transform.position + col.center, col.size);
        else
            Gizmos.DrawWireSphere(transform.position, 2f);
    }
}
