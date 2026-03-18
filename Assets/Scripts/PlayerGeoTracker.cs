using UnityEngine;
using UnityEngine.AI;

public class PlayerGeoTracker : MonoBehaviour
{
    public enum TrackingMode { GPS, Manual, Disabled }

    [SerializeField] private GeoAnchorSystem geoAnchor;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private CampusCameraController cameraController;
    [SerializeField] private TrackingMode trackingMode = TrackingMode.GPS;
    [SerializeField] private float positionSmoothTime = 0.5f;
    [SerializeField] private float maxSnapDistance = 10f;

    [Header("Campus Auto-Focus")]
    [SerializeField] private bool autoFocusOnCampusEntry = true;

    Vector3 smoothVelocity;
    Vector3 lastGpsWorldPos;
    bool hasInitialFix;
    bool hasFocusedOnce;

    public TrackingMode Mode
    {
        get => trackingMode;
        set => trackingMode = value;
    }

    public bool HasGpsFix => hasInitialFix;

    void OnEnable()
    {
        if (geoAnchor != null)
        {
            geoAnchor.OnPositionUpdated += OnGpsPositionUpdated;
            geoAnchor.OnCampusStateChanged += OnCampusStateChanged;
        }
    }

    void OnDisable()
    {
        if (geoAnchor != null)
        {
            geoAnchor.OnPositionUpdated -= OnGpsPositionUpdated;
            geoAnchor.OnCampusStateChanged -= OnCampusStateChanged;
        }
    }

    void OnGpsPositionUpdated(Vector3 worldPos)
    {
        lastGpsWorldPos = worldPos;
        hasInitialFix = true;
    }

    void OnCampusStateChanged(bool onCampus)
    {
        if (onCampus && autoFocusOnCampusEntry && !hasFocusedOnce)
        {
            hasFocusedOnce = true;

            // Switch camera to follow mode and focus on player
            if (cameraController != null)
            {
                cameraController.SwitchMode(CampusCameraController.CameraMode.Follow);
                cameraController.SetOrbit(0, 45f, 30f);
            }

            Debug.Log("[PlayerGeoTracker] Entered campus — camera focused on player.");
        }
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
