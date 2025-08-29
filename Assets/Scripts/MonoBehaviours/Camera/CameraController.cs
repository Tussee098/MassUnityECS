using UnityEngine;

[DisallowMultipleComponent]
public class CameraController2D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;                // base pan speed (units/sec)
    public bool middleMouseDrag = true;         // hold MMB to drag
    public float dragSensitivity = 1.0f;        // MMB drag strength multiplier
    public bool accelerateWithShift = true;     // hold Shift to move faster
    public float shiftMultiplier = 2.5f;        // speed when Shift held

    [Header("Zoom (Orthographic)")]
    public float zoomStep = 5f;                 // how much each scroll changes zoom
    public float zoomSmoothTime = 0.08f;        // smoothing for zoom lerp
    public float minZoom = 2f;                 // min orthographic size
    public float maxZoom = 240f;                // max orthographic size
    public float zoomPanScale = 0.2f;           // pan speed scales with current zoom

    [Header("Ranges (optional)")]
    public bool clampToRanges = false;          // clamp camera XY within a rectangle
    public Vector2 minXY = new Vector2(-200f, -200f);
    public Vector2 maxXY = new Vector2(200f, 200f);

    Camera cam;
    float targetZoom;
    float zoomVel;
    float lockedZ;                              // keep Z constant in 2D

    // For precise drag in world space
    Vector3 lastMouseScreenPos;
    bool dragging;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (!cam) cam = Camera.main;

        cam.orthographic = true; // 2D: ensure orthographic
        targetZoom = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        lockedZ = transform.position.z;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // --- Zoom (mouse wheel) ---
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.001f)
            targetZoom = Mathf.Clamp(targetZoom - scroll * zoomStep, minZoom, maxZoom);

        cam.orthographicSize = Mathf.SmoothDamp(
            cam.orthographicSize, targetZoom, ref zoomVel, zoomSmoothTime);

        // --- Movement: keyboard pan on XY plane ---
        float h = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right
        float v = Input.GetAxisRaw("Vertical");   // W/S or Up/Down

        float speed = moveSpeed;
        // Pan a bit faster when zoomed out so it feels consistent
        speed *= 1f + (GetZoomNormalized() * zoomPanScale);
        if (accelerateWithShift && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
            speed *= shiftMultiplier;

        Vector2 moveXY = new Vector2(h, v) * speed;

        // --- Middle-mouse drag (world-space accurate) ---
        if (middleMouseDrag)
        {
            if (Input.GetMouseButtonDown(2))
            {
                dragging = true;
                lastMouseScreenPos = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(2))
            {
                dragging = false;
            }

            if (dragging)
            {
                Vector3 curr = Input.mousePosition;
                // Convert screen delta to world delta at camera plane
                Vector3 worldA = cam.ScreenToWorldPoint(new Vector3(lastMouseScreenPos.x, lastMouseScreenPos.y, 0f));
                Vector3 worldB = cam.ScreenToWorldPoint(new Vector3(curr.x, curr.y, 0f));
                Vector2 dragWorldDelta = (worldA - worldB); // move camera opposite to mouse drag
                moveXY += dragWorldDelta * (dragSensitivity * 60f); // 60 ~ frame feel match
                lastMouseScreenPos = curr;
            }
        }

        // --- Apply movement on XY, lock Z ---
        Vector3 pos = transform.position;
        pos += new Vector3(moveXY.x, moveXY.y, 0f) * dt;
        pos.z = lockedZ;
        transform.position = pos;

        // --- Optional Ranges clamp on XY ---
        if (clampToRanges)
        {
            var p = transform.position;
            p.x = Mathf.Clamp(p.x, minXY.x, maxXY.x);
            p.y = Mathf.Clamp(p.y, minXY.y, maxXY.y);
            p.z = lockedZ;
            transform.position = p;
        }
    }

    float GetZoomNormalized()
    {
        // returns 0 at minZoom, 1 at maxZoom
        return Mathf.InverseLerp(minZoom, maxZoom, targetZoom);
    }
}
