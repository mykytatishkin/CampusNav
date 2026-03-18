using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class CampusCameraController : MonoBehaviour
{
    public enum CameraMode { Follow, Free }

    [Header("General")]
    [SerializeField] private CameraMode mode = CameraMode.Follow;
    [SerializeField] private Transform target;

    [Header("Orbit Settings")]
    [SerializeField] private float orbitYaw;
    [SerializeField] private float orbitPitch = 45f;
    [SerializeField] private float minPitch = 10f;
    [SerializeField] private float maxPitch = 85f;
    [SerializeField] private float orbitSensitivity = 0.3f;
    [SerializeField] private float touchOrbitSensitivity = 0.15f;

    [Header("Zoom")]
    [SerializeField] private float zoomDistance = 40f;
    [SerializeField] private float minZoom = 8f;
    [SerializeField] private float maxZoom = 120f;
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float pinchZoomSpeed = 0.05f;

    [Header("Follow Smoothing")]
    [SerializeField] private float followSmoothTime = 0.2f;
    [SerializeField] private Vector3 targetOffset = new(0, 1f, 0);

    [Header("Free Mode Pan")]
    [SerializeField] private float panSpeed = 0.5f;

    [Header("Bounds")]
    [SerializeField] private Vector2 boundsCenter = Vector2.zero;
    [SerializeField] private Vector2 boundsSize = new(300, 300);

    Vector3 followVelocity;
    Vector3 orbitCenter;

    // Input state
    Vector2 lastMousePos;
    float lastPinchDist;
    bool isDragging;
    bool isOrbiting;

    public CameraMode Mode { get => mode; set => mode = value; }
    public Transform Target { get => target; set => target = value; }
    public float Yaw => orbitYaw;
    public float Pitch => orbitPitch;
    public float Zoom => zoomDistance;

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Start()
    {
        orbitYaw = transform.eulerAngles.y;
        if (target != null)
            orbitCenter = target.position + targetOffset;
        else
            orbitCenter = transform.position + transform.forward * zoomDistance;

        ApplyOrbit(orbitCenter);
    }

    void LateUpdate()
    {
        if (Touch.activeTouches.Count > 0)
            HandleTouchInput();
        else
            HandleMouseInput();

        HandleScrollZoom();

        Vector3 center;
        if (mode == CameraMode.Follow && target != null)
        {
            Vector3 desiredCenter = target.position + targetOffset;
            orbitCenter = Vector3.SmoothDamp(orbitCenter, desiredCenter, ref followVelocity, followSmoothTime);
            center = orbitCenter;
        }
        else
        {
            center = orbitCenter;
        }

        ApplyOrbit(center);
        ClampPosition();
    }

    void HandleMouseInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 mousePos = mouse.position.ReadValue();

        // Right-click drag = orbit rotation
        if (mouse.rightButton.wasPressedThisFrame)
        {
            isOrbiting = true;
            lastMousePos = mousePos;
        }
        if (mouse.rightButton.wasReleasedThisFrame)
            isOrbiting = false;

        if (isOrbiting)
        {
            Vector2 delta = mousePos - lastMousePos;
            orbitYaw += delta.x * orbitSensitivity;
            orbitPitch -= delta.y * orbitSensitivity;
            orbitPitch = Mathf.Clamp(orbitPitch, minPitch, maxPitch);
            lastMousePos = mousePos;
        }

        // Left-click or middle-click drag = pan (free mode only)
        if (mode == CameraMode.Free)
        {
            if (mouse.leftButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame)
            {
                isDragging = true;
                lastMousePos = mousePos;
            }
            if (mouse.leftButton.wasReleasedThisFrame || mouse.middleButton.wasReleasedThisFrame)
                isDragging = false;

            if (isDragging && !isOrbiting)
            {
                Vector2 delta = mousePos - lastMousePos;
                Vector3 right = transform.right;
                Vector3 fwd = Vector3.Cross(right, Vector3.up).normalized;
                float scale = panSpeed * (zoomDistance / 50f);
                orbitCenter -= (right * delta.x + fwd * delta.y) * scale * Time.deltaTime;
                lastMousePos = mousePos;
            }
        }
    }

    void HandleTouchInput()
    {
        var touches = Touch.activeTouches;
        int touchCount = touches.Count;

        if (touchCount == 1)
        {
            var touch = touches[0];
            Vector2 delta = touch.delta;

            if (mode == CameraMode.Follow)
            {
                // Single finger = orbit in follow mode
                orbitYaw += delta.x * touchOrbitSensitivity;
                orbitPitch -= delta.y * touchOrbitSensitivity;
                orbitPitch = Mathf.Clamp(orbitPitch, minPitch, maxPitch);
            }
            else
            {
                // Single finger = pan in free mode
                Vector3 right = transform.right;
                Vector3 fwd = Vector3.Cross(right, Vector3.up).normalized;
                float scale = panSpeed * (zoomDistance / 50f) * Time.deltaTime;
                orbitCenter -= (right * delta.x + fwd * delta.y) * scale;
            }
        }
        else if (touchCount >= 2)
        {
            var t0 = touches[0];
            var t1 = touches[1];

            // Two-finger twist = yaw rotation
            Vector2 prevDir = (t0.screenPosition - t0.delta) - (t1.screenPosition - t1.delta);
            Vector2 currDir = t0.screenPosition - t1.screenPosition;
            float twistAngle = Vector2.SignedAngle(prevDir, currDir);
            orbitYaw += twistAngle * touchOrbitSensitivity;

            // Pinch = zoom
            float dist = Vector2.Distance(t0.screenPosition, t1.screenPosition);
            float prevDist = Vector2.Distance(
                t0.screenPosition - t0.delta,
                t1.screenPosition - t1.delta);

            if (prevDist > 0.01f)
            {
                float pinchDelta = dist - prevDist;
                zoomDistance = Mathf.Clamp(zoomDistance - pinchDelta * pinchZoomSpeed, minZoom, maxZoom);
            }

            // Vertical two-finger drag = pitch
            float avgDeltaY = (t0.delta.y + t1.delta.y) * 0.5f;
            if (Mathf.Abs(avgDeltaY) > 0.5f)
            {
                orbitPitch -= avgDeltaY * touchOrbitSensitivity * 0.5f;
                orbitPitch = Mathf.Clamp(orbitPitch, minPitch, maxPitch);
            }
        }
    }

    void HandleScrollZoom()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
            zoomDistance = Mathf.Clamp(zoomDistance - scroll * zoomSpeed * 0.01f, minZoom, maxZoom);
    }

    void ApplyOrbit(Vector3 center)
    {
        Quaternion rotation = Quaternion.Euler(orbitPitch, orbitYaw, 0);
        Vector3 offset = rotation * new Vector3(0, 0, -zoomDistance);
        transform.position = center + offset;
        transform.rotation = rotation;
    }

    void ClampPosition()
    {
        Vector3 pos = transform.position;
        float halfW = boundsSize.x / 2f;
        float halfH = boundsSize.y / 2f;
        pos.x = Mathf.Clamp(pos.x, boundsCenter.x - halfW, boundsCenter.x + halfW);
        pos.z = Mathf.Clamp(pos.z, boundsCenter.y - halfH, boundsCenter.y + halfH);
        transform.position = pos;
    }

    public void SwitchMode(CameraMode newMode)
    {
        mode = newMode;
        followVelocity = Vector3.zero;

        if (newMode == CameraMode.Follow && target != null)
            orbitCenter = target.position + targetOffset;
    }

    public void ToggleMode()
    {
        SwitchMode(mode == CameraMode.Follow ? CameraMode.Free : CameraMode.Follow);
    }

    public void SetOrbit(float yaw, float pitch, float zoom)
    {
        orbitYaw = yaw;
        orbitPitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        zoomDistance = Mathf.Clamp(zoom, minZoom, maxZoom);
    }

    public void FocusOn(Vector3 position)
    {
        orbitCenter = position + targetOffset;
        followVelocity = Vector3.zero;
    }
}
