using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// RTS-style orbit camera with pan, rotation, and zoom.
/// Compatible with both the legacy Input Manager and the new Input System package.
/// </summary>
public class SceneViewCamera : MonoBehaviour
{
    [Header("Pan Settings")]
    public float panSpeed = 20f;
    public float panBorderThickness = 10f;
    public bool enableEdgePan = false;

    [Header("Rotation Settings")]
    public float rotationSpeed = 3f;

    [Header("Zoom Settings")]
    public float zoomSpeed = 10f;
    public float minZoomDistance = 5f;
    public float maxZoomDistance = 100f;

    [Header("Focus Settings")]
    public float focusDistance = 20f;

    private Vector3 pivotPoint;
    private float currentDistance;

#if !ENABLE_INPUT_SYSTEM
    private Vector3 lastMousePosition;
#endif

    // ── Input Abstraction ─────────────────────────────────────────────────────
    // All Input calls are isolated here so the camera logic stays clean.
    // Switching input systems only requires changes in these helpers.
#if ENABLE_INPUT_SYSTEM
    private static Vector2 MousePos        => Mouse.current?.position.ReadValue() ?? Vector2.zero;
    private static Vector2 MouseDeltaRaw   => Mouse.current?.delta.ReadValue()    ?? Vector2.zero;
    private static bool    MiddleMouseHeld => Mouse.current?.middleButton.isPressed      ?? false;
    private static bool    RightMouseHeld  => Mouse.current?.rightButton.isPressed       ?? false;
    private static bool    LeftMouseHeld   => Mouse.current?.leftButton.isPressed        ?? false;
    private static bool    AltHeld         => Keyboard.current?.leftAltKey.isPressed     ?? false;
    private static bool    FKeyDown        => Keyboard.current?.fKey.wasPressedThisFrame ?? false;
    private static bool    WKey            => Keyboard.current?.wKey.isPressed           ?? false;
    private static bool    SKey            => Keyboard.current?.sKey.isPressed           ?? false;
    private static bool    AKey            => Keyboard.current?.aKey.isPressed           ?? false;
    private static bool    DKey            => Keyboard.current?.dKey.isPressed           ?? false;
    private static bool    QKey            => Keyboard.current?.qKey.isPressed           ?? false;
    private static bool    EKey            => Keyboard.current?.eKey.isPressed           ?? false;
    private static bool    UpKey           => Keyboard.current?.upArrowKey.isPressed     ?? false;
    private static bool    DownKey         => Keyboard.current?.downArrowKey.isPressed   ?? false;
    private static bool    LeftKey         => Keyboard.current?.leftArrowKey.isPressed   ?? false;
    private static bool    RightKey        => Keyboard.current?.rightArrowKey.isPressed  ?? false;
    // Scale delta to roughly match legacy GetAxis("Mouse X/Y") sensitivity
    private static float   MouseAxisX      => MouseDeltaRaw.x * 0.1f;
    private static float   MouseAxisY      => MouseDeltaRaw.y * 0.1f;
    // Scale scroll to match legacy GetAxis("Mouse ScrollWheel") (~0.1 per notch)
    private static float   ScrollAxis      => (Mouse.current?.scroll.ReadValue().y ?? 0f) / 1200f;
#else
    private static Vector2 MousePos        => Input.mousePosition;
    private static bool    MiddleMouseHeld => Input.GetMouseButton(2);
    private static bool    RightMouseHeld  => Input.GetMouseButton(1);
    private static bool    LeftMouseHeld   => Input.GetMouseButton(0);
    private static bool    AltHeld         => Input.GetKey(KeyCode.LeftAlt);
    private static bool    FKeyDown        => Input.GetKeyDown(KeyCode.F);
    private static bool    WKey            => Input.GetKey(KeyCode.W);
    private static bool    SKey            => Input.GetKey(KeyCode.S);
    private static bool    AKey            => Input.GetKey(KeyCode.A);
    private static bool    DKey            => Input.GetKey(KeyCode.D);
    private static bool    QKey            => Input.GetKey(KeyCode.Q);
    private static bool    EKey            => Input.GetKey(KeyCode.E);
    private static bool    UpKey           => Input.GetKey(KeyCode.UpArrow);
    private static bool    DownKey         => Input.GetKey(KeyCode.DownArrow);
    private static bool    LeftKey         => Input.GetKey(KeyCode.LeftArrow);
    private static bool    RightKey        => Input.GetKey(KeyCode.RightArrow);
    private static float   MouseAxisX      => Input.GetAxis("Mouse X");
    private static float   MouseAxisY      => Input.GetAxis("Mouse Y");
    private static float   ScrollAxis      => Input.GetAxis("Mouse ScrollWheel");
#endif

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Start()
    {
        currentDistance = Vector3.Distance(transform.position, pivotPoint);
        if (currentDistance < 1f)
        {
            currentDistance = focusDistance;
            pivotPoint = transform.position + transform.forward * currentDistance;
        }
    }

    private void Update()
    {
        HandlePan();
        HandleRotation();
        HandleZoom();
        HandleFocus();
    }

    // ── Camera Controls ───────────────────────────────────────────────────────

    private void HandlePan()
    {
        Vector3 moveDirection = Vector3.zero;

        // Middle mouse drag pan
        if (MiddleMouseHeld)
        {
#if ENABLE_INPUT_SYSTEM
            Vector2 delta = MouseDeltaRaw;
#else
            Vector3 delta = (Vector3)MousePos - lastMousePosition;
#endif
            moveDirection -= transform.right * delta.x * panSpeed * 0.01f;
            moveDirection -= transform.up    * delta.y * panSpeed * 0.01f;
        }

        // Keyboard pan (WASD / Arrow keys)
        if (WKey || UpKey)    moveDirection += GetFlatForward();
        if (SKey || DownKey)  moveDirection -= GetFlatForward();
        if (AKey || LeftKey)  moveDirection -= transform.right;
        if (DKey || RightKey) moveDirection += transform.right;

        // Vertical movement (Q / E)
        if (QKey) moveDirection += Vector3.down;
        if (EKey) moveDirection += Vector3.up;

        if (moveDirection != Vector3.zero && !MiddleMouseHeld)
        {
            Vector3 move = moveDirection.normalized * panSpeed * Time.deltaTime;
            transform.position += move;
            pivotPoint         += move;
        }
        else if (MiddleMouseHeld)
        {
            transform.position += moveDirection;
            pivotPoint         += moveDirection;
        }

        // Edge pan (optional)
        if (enableEdgePan)
        {
            Vector3 edgeMove = Vector3.zero;
            Vector2 mPos = MousePos;

            if (mPos.x <= panBorderThickness)                 edgeMove -= transform.right;
            if (mPos.x >= Screen.width  - panBorderThickness) edgeMove += transform.right;
            if (mPos.y <= panBorderThickness)                 edgeMove -= GetFlatForward();
            if (mPos.y >= Screen.height - panBorderThickness) edgeMove += GetFlatForward();

            if (edgeMove != Vector3.zero)
            {
                Vector3 move = edgeMove.normalized * panSpeed * Time.deltaTime;
                transform.position += move;
                pivotPoint         += move;
            }
        }

#if !ENABLE_INPUT_SYSTEM
        lastMousePosition = Input.mousePosition;
#endif
    }

    private void HandleRotation()
    {
        // Right mouse drag to orbit
        if (RightMouseHeld)
        {
            float mouseX = MouseAxisX * rotationSpeed;
            float mouseY = MouseAxisY * rotationSpeed;

            transform.RotateAround(pivotPoint, Vector3.up,        mouseX);
            transform.RotateAround(pivotPoint, transform.right,  -mouseY);

            Vector3 angles = transform.eulerAngles;
            angles.x = ClampAngle(angles.x, -89f, 89f);
            angles.z = 0f;
            transform.eulerAngles = angles;
        }

        // Alt + Left mouse for orbit (Unity-editor style)
        if (AltHeld && LeftMouseHeld)
        {
            float mouseX = MouseAxisX * rotationSpeed;
            float mouseY = MouseAxisY * rotationSpeed;

            transform.RotateAround(pivotPoint, Vector3.up,        mouseX);
            transform.RotateAround(pivotPoint, transform.right,  -mouseY);

            Vector3 angles = transform.eulerAngles;
            angles.x = ClampAngle(angles.x, -89f, 89f);
            angles.z = 0f;
            transform.eulerAngles = angles;
        }
    }

    private void HandleZoom()
    {
        float scroll = ScrollAxis;
        if (Mathf.Abs(scroll) > 0.001f)
        {
            currentDistance -= scroll * zoomSpeed;
            currentDistance  = Mathf.Clamp(currentDistance, minZoomDistance, maxZoomDistance);
            transform.position = pivotPoint - transform.forward * currentDistance;
        }

        // Alt + Right mouse drag zoom (Unity-editor style)
        if (AltHeld && RightMouseHeld)
        {
            float delta = MouseAxisX * zoomSpeed * 0.1f;
            currentDistance -= delta;
            currentDistance  = Mathf.Clamp(currentDistance, minZoomDistance, maxZoomDistance);
            transform.position = pivotPoint - transform.forward * currentDistance;
        }
    }

    private void HandleFocus()
    {
        if (!FKeyDown) return;

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = Camera.main.ScreenPointToRay((Vector3)MousePos);

        if (groundPlane.Raycast(ray, out float distance))
        {
            pivotPoint = ray.GetPoint(distance);
            currentDistance = focusDistance;
            transform.position = pivotPoint - transform.forward * currentDistance;
        }
    }

    // ── Math Helpers ──────────────────────────────────────────────────────────

    private Vector3 GetFlatForward()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        return forward.normalized;
    }

    private float ClampAngle(float angle, float min, float max)
    {
        if (angle > 180f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Instantly move the camera pivot to a world position.</summary>
    public void FocusOn(Vector3 position)
    {
        pivotPoint = position;
        transform.position = pivotPoint - transform.forward * currentDistance;
    }

    /// <summary>Instantly move the camera pivot to a GameObject's position.</summary>
    public void FocusOn(GameObject target)
    {
        if (target != null) FocusOn(target.transform.position);
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pivotPoint, 0.5f);
        Gizmos.DrawLine(transform.position, pivotPoint);
    }
}
