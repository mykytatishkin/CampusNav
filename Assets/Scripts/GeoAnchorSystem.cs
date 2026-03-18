using System.Collections;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class GeoAnchorSystem : MonoBehaviour
{
    [SerializeField] private GeoReference geoReference;
    [SerializeField] private float updateInterval = 1f;
    [SerializeField] private float desiredAccuracyMeters = 5f;

    [Header("Campus Boundary (GPS)")]
    [SerializeField] private double campusMinLat = 54.6875;
    [SerializeField] private double campusMaxLat = 54.6910;
    [SerializeField] private double campusMinLon = 25.2860;
    [SerializeField] private double campusMaxLon = 25.2960;

    public bool IsLocationReady { get; private set; }
    public bool IsOnCampus { get; private set; }
    public double CurrentLatitude { get; private set; }
    public double CurrentLongitude { get; private set; }
    public float CurrentAccuracy { get; private set; }
    public Vector3 CurrentWorldPosition { get; private set; }

    public event System.Action<Vector3> OnPositionUpdated;
    public event System.Action<bool> OnCampusStateChanged;

    bool previousCampusState;

    void Start()
    {
        StartCoroutine(InitLocation());
    }

    IEnumerator InitLocation()
    {
        // Request permissions on Android
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
            // Wait for user to respond to permission dialog
            float waitTime = 0f;
            while (!Permission.HasUserAuthorizedPermission(Permission.FineLocation) && waitTime < 30f)
            {
                yield return new WaitForSeconds(0.5f);
                waitTime += 0.5f;
            }
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                Debug.LogWarning("[GeoAnchor] Location permission denied by user.");
                yield break;
            }
        }
#endif

        // On iOS, Unity automatically requests permission when Input.location.Start() is called.
        // The Info.plist must contain NSLocationWhenInUseUsageDescription.

        if (!Input.location.isEnabledByUser)
        {
            Debug.LogWarning("[GeoAnchor] Location services disabled by user.");
            yield break;
        }

        Input.location.Start(desiredAccuracyMeters, desiredAccuracyMeters);

        int timeout = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && timeout > 0)
        {
            yield return new WaitForSeconds(1);
            timeout--;
        }

        if (Input.location.status != LocationServiceStatus.Running)
        {
            Debug.LogWarning($"[GeoAnchor] Location init failed: {Input.location.status}");
            yield break;
        }

        IsLocationReady = true;
        Debug.Log("[GeoAnchor] Location services started.");
        StartCoroutine(UpdateLoop());
    }

    IEnumerator UpdateLoop()
    {
        var wait = new WaitForSeconds(updateInterval);
        while (IsLocationReady)
        {
            ReadLocation();
            yield return wait;
        }
    }

    void ReadLocation()
    {
        var data = Input.location.lastData;
        CurrentLatitude = data.latitude;
        CurrentLongitude = data.longitude;
        CurrentAccuracy = data.horizontalAccuracy;

        // Check campus boundary
        bool onCampus = CurrentLatitude >= campusMinLat && CurrentLatitude <= campusMaxLat
                     && CurrentLongitude >= campusMinLon && CurrentLongitude <= campusMaxLon;

        if (onCampus != previousCampusState)
        {
            IsOnCampus = onCampus;
            previousCampusState = onCampus;
            OnCampusStateChanged?.Invoke(onCampus);
            Debug.Log($"[GeoAnchor] Campus state changed: {(onCampus ? "ON campus" : "OFF campus")}");
        }
        IsOnCampus = onCampus;

        if (geoReference != null)
        {
            CurrentWorldPosition = geoReference.GpsToWorld(CurrentLatitude, CurrentLongitude);
            OnPositionUpdated?.Invoke(CurrentWorldPosition);
        }
    }

    void OnDestroy()
    {
        if (Input.location.status == LocationServiceStatus.Running)
            Input.location.Stop();
    }

    public void SetCampusBounds(double minLat, double maxLat, double minLon, double maxLon)
    {
        campusMinLat = minLat;
        campusMaxLat = maxLat;
        campusMinLon = minLon;
        campusMaxLon = maxLon;
    }
}
