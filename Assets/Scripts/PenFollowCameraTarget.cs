using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PenKeyboardMoverClamped : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public Transform tip; // TipProbe ở đầu bút (đúng ngoài cùng)

    [Header("Speeds")]
    public float lateralSpeed = 0.6f;     // trái/phải (m/s)
    public float forwardSpeed = 0.6f;     // tới/lui (m/s)
    public float verticalSpeed = 0.4f;    // lên/xuống theo trục dọc bút (m/s)

    [Header("Move Space")]
    public bool forwardInCameraSpace = true; // N/M theo hướng nhìn camera hay theo trục bút

    [Header("Anti-penetration + Slide")]
    public float castRadius = 0.006f;   // ~kích thước tip
    public float skin = 0.001f;         // 1mm
    public float maxStep = 0.08f;       // giới hạn bước mỗi FixedUpdate (m)

    Transform cam;

    void Reset() => rb = GetComponent<Rigidbody>();

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!tip) tip = transform;

        cam = Camera.main ? Camera.main.transform : null;

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    void FixedUpdate()
    {
        Vector3 move = ReadKeyboardMove();     // meters per second
        move *= Time.fixedDeltaTime;           // -> meters per physics step

        // Limit step (avoid big jumps)
        float m = move.magnitude;
        if (m > maxStep) move = move / m * maxStep;

        if (move.sqrMagnitude < 1e-10f) return;

        Vector3 finalMove = SolveSlideByTipCast(move);
        rb.MovePosition(rb.position + finalMove);
    }

    Vector3 ReadKeyboardMove()
    {
        // 1) Left/Right: arrows (lateral)
        float lr = 0f;
        if (Input.GetKey(KeyCode.LeftArrow))  lr -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) lr += 1f;

        // 2) Up/Down arrows: vertical along pen's local up axis
        float ud = 0f;
        if (Input.GetKey(KeyCode.UpArrow))    ud += 1f;
        if (Input.GetKey(KeyCode.DownArrow))  ud -= 1f;

        // 3) Forward/Back: N/M
        float fb = 0f;
        if (Input.GetKey(KeyCode.N)) fb += 1f; // tới
        if (Input.GetKey(KeyCode.M)) fb -= 1f; // lui

        // Build directions
        Vector3 rightDir = GetPlanarRight();         // trái/phải
        Vector3 forwardDir = GetForwardDir();        // tới/lui
        Vector3 verticalDir = transform.up.normalized; // lên/xuống theo bút

        Vector3 v =
            rightDir * (lr * lateralSpeed) +
            forwardDir * (fb * forwardSpeed) +
            verticalDir * (ud * verticalSpeed);

        // normalize diagonal speed a bit (optional)
        float max = Mathf.Max(lateralSpeed, forwardSpeed, verticalSpeed);
        if (v.magnitude > max) v = v.normalized * max;

        return v;
    }

    Vector3 GetPlanarRight()
    {
        // Trái/phải theo mặt phẳng ngang để dễ điều khiển
        Vector3 r;

        if (cam != null)
        {
            r = cam.right;
        }
        else
        {
            r = transform.right;
        }

        r.y = 0f;
        if (r.sqrMagnitude < 1e-6f) r = Vector3.right;
        return r.normalized;
    }

    Vector3 GetForwardDir()
    {
        Vector3 f;

        if (forwardInCameraSpace && cam != null)
            f = cam.forward;
        else
            f = transform.forward;

        f.y = 0f;
        if (f.sqrMagnitude < 1e-6f) f = Vector3.forward;
        return f.normalized;
    }

    // ---- Slide & Clamp ----
    Vector3 SolveSlideByTipCast(Vector3 move)
    {
        Vector3 tipNow = tip.position;

        if (TipCast(tipNow, move, out RaycastHit hit1))
        {
            // slide along surface
            Vector3 slide = Vector3.ProjectOnPlane(move, hit1.normal);
            if (slide.sqrMagnitude < 1e-10f) return Vector3.zero;

            // cast again along slide to avoid sliding through edges
            if (TipCast(tipNow, slide, out RaycastHit hit2))
            {
                float allowed = Mathf.Max(0f, hit2.distance - skin);
                return slide.normalized * allowed;
            }

            return slide;
        }

        return move;
    }

    bool TipCast(Vector3 tipPos, Vector3 move, out RaycastHit hit)
    {
        float dist = move.magnitude;
        if (dist < 1e-6f)
        {
            hit = default;
            return false;
        }

        Vector3 dir = move / dist;

        if (Physics.SphereCast(tipPos, castRadius, dir, out hit,
                               dist + skin, ~0, QueryTriggerInteraction.Ignore))
        {
            // ignore self
            if (hit.collider && hit.collider.transform.IsChildOf(transform))
                return false;

            return true;
        }

        return false;
    }
}
