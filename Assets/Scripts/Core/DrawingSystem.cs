using UnityEngine;
using System.Collections.Generic;

[System.Obsolete("Use VRDrawing.DrawingSurface instead")]
public class DrawingSystem : MonoBehaviour
{
    [Header("Drawing Settings")]
    [SerializeField] private float maxDrawingDistance = 0.05f;
    [SerializeField] private float minPointDistance = 0.001f;
    [SerializeField] private float lineWidth = 0.008f;
    
    [Header("Line Settings")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private int maxPointsPerLine = 1000;
    
    [Header("Layer")]
    [SerializeField] private LayerMask drawingSurfaceLayer;
    
    private List<PenController> registeredPens = new List<PenController>();
    private Dictionary<PenController, DrawingLine> activeLines = new Dictionary<PenController, DrawingLine>();
    private List<DrawingLine> allLines = new List<DrawingLine>();
    private Transform linesParent;
    
    public static DrawingSystem Instance { get; private set; }
    
    private class DrawingLine
    {
        public LineRenderer lineRenderer;
        public List<Vector3> points;
        public Color color;
    }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        GameObject linesObj = new GameObject("DrawnLines");
        linesObj.transform.SetParent(transform);
        linesParent = linesObj.transform;
    }
    
    private void Update()
    {
        UpdateDrawing();
    }
    
    public void RegisterPen(PenController pen)
    {
        if (!registeredPens.Contains(pen))
        {
            registeredPens.Add(pen);
        }
    }
    
    public void UnregisterPen(PenController pen)
    {
        registeredPens.Remove(pen);
        
        if (activeLines.ContainsKey(pen))
        {
            activeLines.Remove(pen);
        }
    }
    
    private void UpdateDrawing()
    {
        foreach (PenController pen in registeredPens)
        {
            if (pen == null || !pen.IsHeld()) continue;
            
            Vector3 tipPosition = pen.GetTipPosition();
            Vector3 tipDirection = pen.GetTipDirection();
            
            RaycastHit hit;
            bool isDrawing = Physics.Raycast(tipPosition, tipDirection, out hit, maxDrawingDistance, drawingSurfaceLayer);
            
            if (isDrawing)
            {
                if (!activeLines.ContainsKey(pen))
                {
                    StartNewLine(pen, hit.point);
                }
                else
                {
                    ContinueLine(pen, hit.point);
                }
            }
            else
            {
                if (activeLines.ContainsKey(pen))
                {
                    EndLine(pen);
                }
            }
        }
    }
    
    private void StartNewLine(PenController pen, Vector3 startPoint)
    {
        GameObject lineObj = new GameObject($"Line_{allLines.Count}");
        lineObj.transform.SetParent(linesParent);
        lineObj.transform.position = Vector3.zero;
        
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.positionCount = 1;
        lr.SetPosition(0, startPoint);
        lr.useWorldSpace = true;
        lr.numCapVertices = 5;
        lr.numCornerVertices = 5;
        
        Color penColor = pen.GetPenColor();
        lr.startColor = penColor;
        lr.endColor = penColor;
        
        DrawingLine drawingLine = new DrawingLine
        {
            lineRenderer = lr,
            points = new List<Vector3> { startPoint },
            color = penColor
        };
        
        activeLines[pen] = drawingLine;
        allLines.Add(drawingLine);
    }
    
    private void ContinueLine(PenController pen, Vector3 newPoint)
    {
        DrawingLine line = activeLines[pen];
        
        if (line.points.Count >= maxPointsPerLine) return;
        
        Vector3 lastPoint = line.points[line.points.Count - 1];
        float distance = Vector3.Distance(lastPoint, newPoint);
        
        if (distance < minPointDistance) return;
        
        line.points.Add(newPoint);
        line.lineRenderer.positionCount = line.points.Count;
        line.lineRenderer.SetPosition(line.points.Count - 1, newPoint);
    }
    
    private void EndLine(PenController pen)
    {
        activeLines.Remove(pen);
    }
    
    private void StopAllDrawing()
    {
        activeLines.Clear();
    }
    
    public void ClearAllLines()
    {
        foreach (DrawingLine line in allLines)
        {
            if (line.lineRenderer != null)
            {
                Destroy(line.lineRenderer.gameObject);
            }
        }
        
        allLines.Clear();
        activeLines.Clear();
    }
    
    public bool CanDraw()
    {
        return registeredPens.Count > 0;
    }
}
