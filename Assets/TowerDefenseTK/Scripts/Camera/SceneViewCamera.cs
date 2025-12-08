using UnityEngine;

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
    private Vector3 lastMousePosition;

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

    private void HandlePan()
    {
        Vector3 moveDirection = Vector3.zero;

        // Middle mouse drag pan
        if (Input.GetMouseButton(2))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            moveDirection -= transform.right * delta.x * panSpeed * 0.01f;
            moveDirection -= transform.up * delta.y * panSpeed * 0.01f;
        }

        // Keyboard pan (WASD or Arrow keys)
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            moveDirection += GetFlatForward();
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            moveDirection -= GetFlatForward();
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            moveDirection -= transform.right;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            moveDirection += transform.right;

        // Vertical movement (Q and E)
        if (Input.GetKey(KeyCode.Q))
            moveDirection += Vector3.down;
        if (Input.GetKey(KeyCode.E))
            moveDirection += Vector3.up;

        // Apply keyboard/edge movement
        if (moveDirection != Vector3.zero && !Input.GetMouseButton(2))
        {
            Vector3 move = moveDirection.normalized * panSpeed * Time.deltaTime;
            transform.position += move;
            pivotPoint += move;
        }
        // Apply mouse drag movement
        else if (Input.GetMouseButton(2))
        {
            transform.position += moveDirection;
            pivotPoint += moveDirection;
        }

        // Edge pan (optional)
        if (enableEdgePan)
        {
            Vector3 edgeMove = Vector3.zero;

            if (Input.mousePosition.x <= panBorderThickness)
                edgeMove -= transform.right;
            if (Input.mousePosition.x >= Screen.width - panBorderThickness)
                edgeMove += transform.right;
            if (Input.mousePosition.y <= panBorderThickness)
                edgeMove -= GetFlatForward();
            if (Input.mousePosition.y >= Screen.height - panBorderThickness)
                edgeMove += GetFlatForward();

            if (edgeMove != Vector3.zero)
            {
                Vector3 move = edgeMove.normalized * panSpeed * Time.deltaTime;
                transform.position += move;
                pivotPoint += move;
            }
        }

        lastMousePosition = Input.mousePosition;
    }

    private void HandleRotation()
    {
        // Right mouse drag to orbit
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

            // Horizontal orbit (around world up)
            transform.RotateAround(pivotPoint, Vector3.up, mouseX);

            // Vertical orbit (around camera's right axis)
            transform.RotateAround(pivotPoint, transform.right, -mouseY);

            // Clamp vertical rotation to prevent flipping
            Vector3 angles = transform.eulerAngles;
            angles.x = ClampAngle(angles.x, -89f, 89f);
            angles.z = 0f;
            transform.eulerAngles = angles;
        }

        // Alt + Left mouse for orbit (Unity style)
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0))
        {
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

            transform.RotateAround(pivotPoint, Vector3.up, mouseX);
            transform.RotateAround(pivotPoint, transform.right, -mouseY);

            Vector3 angles = transform.eulerAngles;
            angles.x = ClampAngle(angles.x, -89f, 89f);
            angles.z = 0f;
            transform.eulerAngles = angles;
        }
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentDistance -= scroll * zoomSpeed;
            currentDistance = Mathf.Clamp(currentDistance, minZoomDistance, maxZoomDistance);

            transform.position = pivotPoint - transform.forward * currentDistance;
        }

        // Alt + Right mouse drag zoom (Unity style)
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(1))
        {
            float delta = Input.GetAxis("Mouse X") * zoomSpeed * 0.1f;
            currentDistance -= delta;
            currentDistance = Mathf.Clamp(currentDistance, minZoomDistance, maxZoomDistance);

            transform.position = pivotPoint - transform.forward * currentDistance;
        }
    }

    private void HandleFocus()
    {
        // Press F to focus on a point under the mouse
        if (Input.GetKeyDown(KeyCode.F))
        {
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (groundPlane.Raycast(ray, out float distance))
            {
                pivotPoint = ray.GetPoint(distance);
                currentDistance = focusDistance;
                transform.position = pivotPoint - transform.forward * currentDistance;
            }
        }
    }

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

    // Call this to focus on a specific object
    public void FocusOn(Vector3 position)
    {
        pivotPoint = position;
        transform.position = pivotPoint - transform.forward * currentDistance;
    }

    // Call this to focus on a GameObject
    public void FocusOn(GameObject target)
    {
        if (target != null)
        {
            FocusOn(target.transform.position);
        }
    }

    // Visualize pivot point in editor
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pivotPoint, 0.5f);
        Gizmos.DrawLine(transform.position, pivotPoint);
    }
}