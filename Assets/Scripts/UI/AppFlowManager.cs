using UnityEngine;

/// <summary>
/// Central state machine managing the full app flow:
/// MainMenu -> CampusSelect -> DestinationSelect -> StartPointSelect -> Navigation
/// </summary>
public class AppFlowManager : MonoBehaviour
{
    public enum AppState
    {
        MainMenu,
        CampusSelect,
        DestinationSelect,
        StartPointSelect,
        Navigation,
        IndoorSelect
    }

    public enum NavigationMode { Map, AR }

    [Header("Screens")]
    [SerializeField] private MainMenuScreen mainMenuScreen;
    [SerializeField] private CampusSelectScreen campusSelectScreen;
    [SerializeField] private DestinationSelectScreen destinationSelectScreen;
    [SerializeField] private StartPointScreen startPointScreen;
    [SerializeField] private NavigationScreen navigationScreen;
    [SerializeField] private FloorSelectUI indoorSelectScreen;

    [Header("Systems")]
    [SerializeField] private CampusNavigator navigator;
    [SerializeField] private GeoAnchorSystem geoAnchor;
    [SerializeField] private PlayerGeoTracker playerTracker;
    [SerializeField] private CampusCameraController cameraController;
    [SerializeField] private CampusData[] availableCampuses;

    [Header("3D World")]
    [SerializeField] private GameObject campusWorldRoot;

    AppState currentState;
    NavigationMode currentMode;
    CampusData selectedCampus;
    RoutePoint selectedDestination;
    RoutePoint selectedStartPoint;
    bool useLiveGps;
    string enteredBuildingCode;

    public AppState CurrentState => currentState;
    public NavigationMode CurrentMode => currentMode;
    public CampusData SelectedCampus => selectedCampus;
    public RoutePoint SelectedDestination => selectedDestination;
    public bool UseLiveGps => useLiveGps;
    public GeoAnchorSystem GeoAnchor => geoAnchor;
    public CampusNavigator Navigator => navigator;
    public CampusCameraController CameraController => cameraController;
    public PlayerGeoTracker PlayerTracker => playerTracker;
    public string EnteredBuildingCode => enteredBuildingCode;

    void Start()
    {
        GoToState(AppState.MainMenu);
    }

    public void GoToState(AppState state)
    {
        currentState = state;
        HideAllScreens();

        switch (state)
        {
            case AppState.MainMenu:
                if (mainMenuScreen != null)
                {
                    mainMenuScreen.gameObject.SetActive(true);
                    mainMenuScreen.Show(this);
                }
                break;

            case AppState.CampusSelect:
                if (campusSelectScreen != null)
                {
                    campusSelectScreen.gameObject.SetActive(true);
                    campusSelectScreen.Show(this, availableCampuses);
                }
                break;

            case AppState.DestinationSelect:
                if (destinationSelectScreen != null)
                {
                    destinationSelectScreen.gameObject.SetActive(true);
                    destinationSelectScreen.Show(this, selectedCampus);
                }
                break;

            case AppState.StartPointSelect:
                if (startPointScreen != null)
                {
                    startPointScreen.gameObject.SetActive(true);
                    startPointScreen.Show(this, selectedCampus);
                }
                break;

            case AppState.Navigation:
                if (navigationScreen != null)
                {
                    navigationScreen.gameObject.SetActive(true);
                    navigationScreen.Show(this);
                }
                StartNavigation();
                break;

            case AppState.IndoorSelect:
                if (indoorSelectScreen != null)
                {
                    indoorSelectScreen.gameObject.SetActive(true);
                    indoorSelectScreen.Show(this, selectedCampus, enteredBuildingCode);
                }
                break;
        }
    }

    void HideAllScreens()
    {
        if (mainMenuScreen != null) mainMenuScreen.gameObject.SetActive(false);
        if (campusSelectScreen != null) campusSelectScreen.gameObject.SetActive(false);
        if (destinationSelectScreen != null) destinationSelectScreen.gameObject.SetActive(false);
        if (startPointScreen != null) startPointScreen.gameObject.SetActive(false);
        if (navigationScreen != null) navigationScreen.gameObject.SetActive(false);
        if (indoorSelectScreen != null) indoorSelectScreen.gameObject.SetActive(false);
    }

    // === Flow callbacks from screens ===

    public void OnModeSelected(NavigationMode mode)
    {
        currentMode = mode;
        GoToState(AppState.CampusSelect);
    }

    public void OnCampusSelected(CampusData campus)
    {
        selectedCampus = campus;
        // Wire route database to navigator at runtime
        if (navigator != null && campus.routeDatabase != null)
            navigator.SetRouteDatabase(campus.routeDatabase);
        GoToState(AppState.DestinationSelect);
    }

    public void OnDestinationSelected(RoutePoint destination)
    {
        selectedDestination = destination;
        GoToState(AppState.StartPointSelect);
    }

    public void OnStartPointSelected(RoutePoint startPoint)
    {
        selectedStartPoint = startPoint;
        useLiveGps = false;
        GoToState(AppState.Navigation);
    }

    public void OnLiveGpsSelected()
    {
        selectedStartPoint = null;
        useLiveGps = true;
        GoToState(AppState.Navigation);
    }

    public void OnBackPressed()
    {
        switch (currentState)
        {
            case AppState.CampusSelect:
                GoToState(AppState.MainMenu);
                break;
            case AppState.DestinationSelect:
                GoToState(AppState.CampusSelect);
                break;
            case AppState.StartPointSelect:
                GoToState(AppState.DestinationSelect);
                break;
            case AppState.Navigation:
                StopNavigation();
                GoToState(AppState.DestinationSelect);
                break;
            case AppState.IndoorSelect:
                StopNavigation();
                GoToState(AppState.DestinationSelect);
                break;
        }
    }

    /// <summary>Called by EntranceTrigger when the player reaches a building entrance.</summary>
    public void OnBuildingEntered(string buildingCode)
    {
        if (currentState != AppState.Navigation) return;
        enteredBuildingCode = buildingCode;
        StopNavigation();
        GoToState(AppState.IndoorSelect);
    }

    /// <summary>Called by FloorSelectUI when user picks a room inside a building.</summary>
    public void OnIndoorDestinationSelected(RoutePoint destination)
    {
        selectedDestination = destination;
        GoToState(AppState.Navigation);
    }

    // === Navigation ===

    void StartNavigation()
    {
        if (navigator == null || selectedDestination == null) return;

        if (useLiveGps)
        {
            // Enable GPS tracking
            if (playerTracker != null)
            {
                playerTracker.Mode = PlayerGeoTracker.TrackingMode.GPS;
            }
            // Camera follows player
            if (cameraController != null)
            {
                cameraController.SwitchMode(CampusCameraController.CameraMode.Follow);
                cameraController.SetOrbit(0, 50f, 30f);
            }
        }
        else if (selectedStartPoint != null)
        {
            // Teleport player to start point
            if (playerTracker != null)
            {
                playerTracker.SetManualPosition(selectedStartPoint.worldPosition);
                playerTracker.Mode = PlayerGeoTracker.TrackingMode.Manual;
            }
            if (cameraController != null)
            {
                cameraController.FocusOn(selectedStartPoint.worldPosition);
                cameraController.SwitchMode(CampusCameraController.CameraMode.Follow);
            }
        }

        // Set navigation destination
        navigator.SetDestination(selectedDestination);
    }

    void StopNavigation()
    {
        if (navigator != null)
            navigator.ClearPath();
    }

    public bool IsGpsNearUniversity()
    {
        if (geoAnchor == null || !geoAnchor.IsLocationReady) return false;
        if (availableCampuses == null) return false;

        foreach (var campus in availableCampuses)
        {
            if (campus != null && campus.IsNearCampus(geoAnchor.CurrentLatitude, geoAnchor.CurrentLongitude))
                return true;
        }
        return false;
    }
}
