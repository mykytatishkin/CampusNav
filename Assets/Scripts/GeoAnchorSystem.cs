using System.Collections;
using UnityEngine;

public class GeoAnchorSystem : MonoBehaviour
{
    [SerializeField] private GeoReference geoReference;
    [SerializeField] private float updateInterval = 1f;
    [SerializeField] private float desiredAccuracyMeters = 5f;

    public bool IsLocationReady { get; private set; }
    public double CurrentLatitude { get; private set; }
    public double CurrentLongitude { get; private set; }
    public float CurrentAccuracy { get; private set; }
    public Vector3 CurrentWorldPosition { get; private set; }

    public event System.Action<Vector3> OnPositionUpdated;

    void Start()
    {
        StartCoroutine(InitLocation());
    }

    IEnumerator InitLocation()
    {
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
}
