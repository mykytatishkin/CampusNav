using UnityEngine;

public class CampusCameraController : MonoBehaviour
{
    public enum CameraMode { Follow, Free }

    [Header("General")]
    [SerializeField] private CameraMode mode = CameraMode.Follow;
    [SerializeField] private Transform target;

    [Header("Follow Mode")]
    [SerializeField] private Vector3 followOffset = new(0, 40, -25);
    [SerializeField] private float followSmoothTime = 0.3f;
    [SerializeField] private float followLookAngle = 60f;

    [Header("Free Mode")]
    [SerializeField] private float panSpeed = 0.5f;
    [SerializeField] private float rotateSpeed = 0.3f;

    [Header("Zoom (both modes)")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minZoom = 10f;
    [SerializeField] private float maxZoom = 120f;

    [Header("Bounds")]
    [SerializeField] private Vector2 boundsCenter = Vector2.zero;
    [SerializeField] private Vector2 boundsSize = new(200, 200);

    Vector3 velocity;
    float currentZoom;
    float currentYaw;
    Vector2 lastPanPos;
    float lastPinchDist;
    bool isPanning;
    bool isRotating;

    public CameraMode Mode
    {
        get => mode;
        set => mode = value;
    }

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    void Start()
    {
        currentZoom = followOffset.magnitude;
        currentYaw = transform.eulerAngles.y;
    }

    void LateUpdate()
    {
        HandleZoom();

        if (mode == CameraMode.Follow)
            UpdateFollow();
        else
            UpdateFree();

        ClampPosition();
    }

    void UpdateFollow()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position + Quaternion.Euler(0, currentYaw, 0) * followOffset.normalized * currentZoom;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, followSmoothTime);
        transform.rotation = Quaternion.Euler(followLookAngle, currentYaw, 0);
    }

    void UpdateFree()
    {
        if (Input.touchSupported && Input.touchCount > 0)
            HandleTouchFree();
        else
            HandleMouseFree();
    }

    void HandleMouseFree()
    {
        // Right-click drag to rotate
        if (Input.GetMouseButtonDown(1))
        {
            isRotating = true;
            lastPanPos = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(1))
            isRotating = false;
        if (isRotating)
        {
            Vector2 delta = (Vector2)Input.mousePosition - lastPanPos;
            currentYaw += delta.x * rotateSpeed;
            lastPanPos = Input.mousePosition;
        }

        // Middle-click or left-click drag to pan
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2))
        {
            isPanning = true;
            lastPanPos = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(2))
            isPanning = false;
        if (isPanning && !isRotating)
        {
            Vector2 delta = (Vector2)Input.mousePosition - lastPanPos;
            Vector3 right = transform.right;
            Vector3 fwd = Vector3.Cross(right, Vector3.up).normalized;
            transform.position -= (right * delta.x + fwd * delta.y) * panSpeed * (currentZoom / 50f) * Time.deltaTime;
            lastPanPos = Input.mousePosition;
        }

        transform.rotation = Quaternion.Euler(followLookAngle, currentYaw, 0);
    }

    void HandleTouchFree()
    {
        int touchCount = Input.touchCount;

        if (touchCount == 1)
        {
            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                Vector3 right = transform.right;
                Vector3 fwd = Vector3.Cross(right, Vector3.up).normalized;
                Vector2 d = touch.deltaPosition;
                transform.position -= (right * d.x + fwd * d.y) * panSpeed * (currentZoom / 50f) * Time.deltaTime;
            }
        }
        else if (touchCount == 2)
        {
            var t0 = Input.GetTouch(0);
            var t1 = Input.GetTouch(1);

            // Rotation from two-finger twist
            Vector2 prevDir = (t0.position - t0.deltaPosition) - (t1.position - t1.deltaPosition);
            Vector2 currDir = t0.position - t1.position;
            float angle = Vector2.SignedAngle(prevDir, currDir);
            currentYaw += angle * rotateSpeed;
        }

        transform.rotation = Quaternion.Euler(followLookAngle, currentYaw, 0);
    }

    void HandleZoom()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentZoom = Mathf.Clamp(currentZoom - scroll * zoomSpeed, minZoom, maxZoom);
        }

        // Pinch-to-zoom
        if (Input.touchCount == 2)
        {
            var t0 = Input.GetTouch(0);
            var t1 = Input.GetTouch(1);
            float dist = Vector2.Distance(t0.position, t1.position);

            if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
            {
                lastPinchDist = dist;
            }
            else
            {
                float delta = dist - lastPinchDist;
                currentZoom = Mathf.Clamp(currentZoom - delta * zoomSpeed * 0.01f, minZoom, maxZoom);
                lastPinchDist = dist;
            }
        }
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
        velocity = Vector3.zero;
    }

    public void ToggleMode()
    {
        SwitchMode(mode == CameraMode.Follow ? CameraMode.Free : CameraMode.Follow);
    }
}
