using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NavigationSettingsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavigationPreferences navPreferences;
    [SerializeField] private CampusCameraController cameraController;

    [Header("UI Elements")]
    [SerializeField] private Toggle elevatorToggle;
    [SerializeField] private Toggle accessibleToggle;
    [SerializeField] private Button cameraModeButton;
    [SerializeField] private TextMeshProUGUI cameraModeLabel;
    [SerializeField] private TextMeshProUGUI floorIndicator;

    [Header("Floor Detection")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float floorHeight = 3.5f;

    void Start()
    {
        if (elevatorToggle != null)
        {
            elevatorToggle.isOn = navPreferences != null && navPreferences.UseElevators;
            elevatorToggle.onValueChanged.AddListener(OnElevatorToggled);
        }

        if (accessibleToggle != null)
        {
            accessibleToggle.isOn = navPreferences != null && navPreferences.AccessibleRoutesOnly;
            accessibleToggle.onValueChanged.AddListener(OnAccessibleToggled);
        }

        if (cameraModeButton != null)
            cameraModeButton.onClick.AddListener(OnCameraModeToggle);

        UpdateCameraModeLabel();
    }

    void Update()
    {
        UpdateFloorIndicator();
    }

    void OnElevatorToggled(bool value)
    {
        if (navPreferences != null)
            navPreferences.UseElevators = value;
    }

    void OnAccessibleToggled(bool value)
    {
        if (navPreferences != null)
            navPreferences.AccessibleRoutesOnly = value;
    }

    void OnCameraModeToggle()
    {
        if (cameraController != null)
            cameraController.ToggleMode();
        UpdateCameraModeLabel();
    }

    void UpdateCameraModeLabel()
    {
        if (cameraModeLabel == null || cameraController == null) return;
        cameraModeLabel.text = cameraController.Mode == CampusCameraController.CameraMode.Follow
            ? "Camera: Follow"
            : "Camera: Free";
    }

    void UpdateFloorIndicator()
    {
        if (floorIndicator == null || playerTransform == null) return;
        int floor = Mathf.FloorToInt(playerTransform.position.y / floorHeight) + 1;
        floorIndicator.text = $"Floor: {floor}";
    }
}
