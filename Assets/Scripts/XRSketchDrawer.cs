using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class XRSketchDrawer : MonoBehaviour
{
    [Header("Input (Input System)")]
    public InputActionReference drawAction;   // Mouse LMB
    public InputActionReference eraseAction;  // Mouse RMB (hold)

    [Header("Tip")]
    public Transform tipTransform;
    public float tipRadius = 0.003f;

    [Header("Board (single collider)")]
    public LayerMask drawableMask;
    public float castDistance = 0.03f;
    public float surfaceOffset = 0.0015f;

    [Header("Stroke")]
    public LineRenderer strokePrefab;
    public float minDistance = 0.005f;
    public float filterStrength = 20f;

    [Header("Erase (partial)")]
    public float eraseRadius = 0.02f;     // bán kính cục tẩy (m)
    public int minPointsPerSegment = 2;   // đoạn còn lại phải >= 2 điểm mới tạo stroke mới

    [Header("Color")]
    public Color currentColor = Color.black;
    public Color[] palette = new Color[]
    {
        Color.black, Color.red, Color.blue, Color.green, Color.yellow, Color.white
    };

    // ---------- internal ----------
    class StrokeData
    {
        public LineRenderer lr;
        public List<Vector3> pts;
        public Color color;
    }

    StrokeData _current;
    Vector3 _filteredPoint;
    bool _isDrawing;

    readonly List<StrokeData> _strokes = new();

    void OnEnable()
    {
        drawAction?.action.Enable();
        eraseAction?.action.Enable();
    }

    void OnDisable()
    {
        drawAction?.action.Disable();
        eraseAction?.action.Disable();
    }

    void Update()
    {
        if (!tipTransform || !strokePrefab || drawAction == null) return;

        // Quick color hotkeys (không cần InputAction):
        // 1..6 chọn palette
        for (int i = 0; i < palette.Length && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                currentColor = palette[i];
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            currentColor = Color.red;
        }

        float draw = drawAction.action.ReadValue<float>();
        float erase = (eraseAction != null) ? eraseAction.action.ReadValue<float>() : 0f;

        // Erase ưu tiên
        if (erase > 0.1f)
        {
            TryErasePartial();
            if (_isDrawing) EndStroke();
            return;
        }

        // Cast để lấy điểm vẽ trên board
        Vector3 origin = tipTransform.position;
        Vector3 dir = tipTransform.forward; // nếu không hit: thử -tipTransform.up

        if (Physics.SphereCast(origin, tipRadius, dir, out RaycastHit hit,
            castDistance, drawableMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 hitPoint = hit.point + hit.normal * surfaceOffset;

            if (draw > 0.1f)
            {
                Debug.Log($"Before StartStroke on {gameObject.name}: currentColor={currentColor}");

                if (!_isDrawing) StartStroke(hitPoint, currentColor);
                AddPointSmooth(hitPoint);
            }
            else if (_isDrawing)
            {
                EndStroke();
            }
        }
        else
        {
            if (_isDrawing) EndStroke();
        }
    }

    // ---------------- DRAW ----------------
    void StartStroke(Vector3 firstPoint, Color color)
    {
        var lr = Instantiate(strokePrefab);
        ApplyColor(lr, color);

        var data = new StrokeData
        {
            lr = lr,
            pts = new List<Vector3>(128),
            color = color
        };

        data.pts.Add(firstPoint);
        lr.positionCount = 1;
        lr.SetPosition(0, firstPoint);

        _strokes.Add(data);
        _current = data;

        _filteredPoint = firstPoint;
        _isDrawing = true;
    }

    void AddPointSmooth(Vector3 rawPoint)
    {
        if (_current == null || _current.lr == null) return;

        float t = 1f - Mathf.Exp(-filterStrength * Time.deltaTime);
        _filteredPoint = Vector3.Lerp(_filteredPoint, rawPoint, t);

        var pts = _current.pts;
        if (Vector3.Distance(pts[^1], _filteredPoint) < minDistance) return;

        pts.Add(_filteredPoint);

        _current.lr.positionCount = pts.Count;
        _current.lr.SetPosition(pts.Count - 1, _filteredPoint);
    }

    void EndStroke()
    {
        _isDrawing = false;
        _current = null;
    }

    void ApplyColor(LineRenderer lr, Color c)
{
    // 1) set start/end color (nhiều trường hợp ăn hơn gradient)
    lr.startColor = c;
    lr.endColor = c;

    // 2) set gradient
    var g = new Gradient();
    g.SetKeys(
        new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
        new[] { new GradientAlphaKey(c.a, 0f), new GradientAlphaKey(c.a, 1f) }
    );
    lr.colorGradient = g;

    // 3) material instance + tint
    if (lr.material != null)
    {
        lr.material = new Material(lr.material);

        if (lr.material.HasProperty("_BaseColor"))
            lr.material.SetColor("_BaseColor", c);
        if (lr.material.HasProperty("_Color"))
            lr.material.SetColor("_Color", c);
    }

    Debug.Log($"Shader={lr.material?.shader?.name} c={c}");
}

    // ---------------- ERASE (PARTIAL) ----------------
    void TryErasePartial()
    {
        if (_strokes.Count == 0) return;

        Vector3 eraser = tipTransform.position;
        float r = eraseRadius;

        for (int si = _strokes.Count - 1; si >= 0; si--)
        {
            StrokeData s = _strokes[si];
            if (s == null || s.lr == null || s.pts == null || s.pts.Count < 2)
            {
                _strokes.RemoveAt(si);
                continue;
            }

            var pts = s.pts;
            bool[] keep = new bool[pts.Count];
            for (int i = 0; i < keep.Length; i++) keep[i] = true;

            bool anyCut = false;

            // Nếu segment (i -> i+1) nằm trong bán kính, “cắt” quanh đoạn đó
            for (int i = 0; i < pts.Count - 1; i++)
            {
                float d = DistancePointToSegment(eraser, pts[i], pts[i + 1]);
                if (d <= r)
                {
                    keep[i] = false;
                    keep[i + 1] = false;
                    anyCut = true;
                }
            }

            if (!anyCut) continue;

            // Tách thành các đoạn liên tục còn lại
            List<List<Vector3>> segments = new();
            List<Vector3> cur = null;

            for (int i = 0; i < pts.Count; i++)
            {
                if (keep[i])
                {
                    cur ??= new List<Vector3>();
                    cur.Add(pts[i]);
                }
                else
                {
                    if (cur != null)
                    {
                        if (cur.Count >= minPointsPerSegment) segments.Add(cur);
                        cur = null;
                    }
                }
            }
            if (cur != null && cur.Count >= minPointsPerSegment) segments.Add(cur);

            // Xoá stroke cũ
            Destroy(s.lr.gameObject);
            _strokes.RemoveAt(si);

            // Tạo stroke mới cho từng đoạn
            foreach (var seg in segments)
            {
                var lrNew = Instantiate(strokePrefab);
                ApplyColor(lrNew, s.color);

                lrNew.positionCount = seg.Count;
                lrNew.SetPositions(seg.ToArray());

                _strokes.Add(new StrokeData { lr = lrNew, pts = seg, color = s.color });
            }
        }
    }
    static float DistancePointToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float ab2 = Vector3.Dot(ab, ab);
        if (ab2 < 1e-8f) return Vector3.Distance(p, a);

        float t = Vector3.Dot(p - a, ab) / ab2;
        t = Mathf.Clamp01(t);
        Vector3 proj = a + t * ab;
        return Vector3.Distance(p, proj);
    }
}
