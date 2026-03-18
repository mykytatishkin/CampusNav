using UnityEngine;
using UnityEngine.AI;

public class PlayerGeoTracker : MonoBehaviour
{
    public enum TrackingMode { GPS, Manual, Disabled }

    [SerializeField] private GeoAnchorSystem geoAnchor;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private TrackingMode trackingMode = TrackingMode.GPS;
    [SerializeField] private float positionSmoothTime = 0.5f;
    [SerializeField] private float maxSnapDistance = 10f;

    Vector3 smoothVelocity;
    Vector3 lastGpsWorldPos;
    bool hasInitialFix;

    public TrackingMode Mode
    {
        get => trackingMode;
        set => trackingMode = value;
    }

    void OnEnable()
    {
        if (geoAnchor != null)
            geoAnchor.OnPositionUpdated += OnGpsPositionUpdated;
    }

    void OnDisable()
    {
        if (geoAnchor != null)
            geoAnchor.OnPositionUpdated -= OnGpsPositionUpdated;
    }

    void OnGpsPositionUpdated(Vector3 worldPos)
    {
        lastGpsWorldPos = worldPos;
        hasInitialFix = true;
    }

    void Update()
    {
        if (trackingMode != TrackingMode.GPS || !hasInitialFix) return;
        if (agent == null) return;

        // Snap to NavMesh nearest point
        if (NavMesh.SamplePosition(lastGpsWorldPos, out NavMeshHit hit, maxSnapDistance, NavMesh.AllAreas))
        {
            Vector3 target = hit.position;
            Vector3 smoothed = Vector3.SmoothDamp(
                transform.position, target, ref smoothVelocity, positionSmoothTime);

            agent.Warp(smoothed);
        }
    }

    public void SetManualPosition(Vector3 worldPos)
    {
        if (agent != null)
            agent.Warp(worldPos);
        trackingMode = TrackingMode.Manual;
    }
}
