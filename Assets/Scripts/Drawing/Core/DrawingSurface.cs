using UnityEngine;
using System.Collections.Generic;
using VRDrawing.Data;
using VRDrawing.Tools;
using VRDrawing.Rendering;

namespace VRDrawing
{
    [RequireComponent(typeof(Collider))]
    public class DrawingSurface : MonoBehaviour
    {
        [Header("Surface Settings")]
        [SerializeField] private Vector2 surfaceSize = new Vector2(0.4f, 0.3f);
        [SerializeField] private float minPointDistance = 0.002f;

        [Header("Data")]
        [SerializeField] private DrawingData drawingData;

        [Header("Rendering")]
        [SerializeField] private StrokeRenderer strokeRenderer;

        [Header("History")]
        [SerializeField] private bool enableHistory = true;
        [SerializeField] private int maxHistorySize = 50;

        [Header("Audio")]
        [SerializeField] private AudioClip drawingAudioClip;
        [SerializeField] private AudioClip eraseAudioClip;
        [SerializeField] private AudioClip clearAudioClip;

        private Dictionary<DrawingToolBase, Stroke> activeStrokes = new Dictionary<DrawingToolBase, Stroke>();
        private DrawingHistoryManager historyManager;
        private Collider surfaceCollider;
        private AudioSource audioSource;
        private bool isDirty = false;

        public DrawingData Data => drawingData;
        public int StrokeCount => drawingData?.GetStrokeCount() ?? 0;
        public bool HasActiveStroke => activeStrokes.Count > 0;

        public System.Action<Stroke> OnStrokeAdded;
        public System.Action<Stroke> OnStrokeRemoved;
        public System.Action OnCleared;
        public System.Action OnHistoryChanged;

        private void Awake()
        {
            surfaceCollider = GetComponent<Collider>();
            if (!surfaceCollider.isTrigger)
            {
                surfaceCollider.isTrigger = true;
            }

            if (drawingData == null)
            {
                drawingData = new DrawingData();
            }

            if (strokeRenderer == null)
            {
                strokeRenderer = GetComponent<StrokeRenderer>();
                if (strokeRenderer == null)
                {
                    strokeRenderer = gameObject.AddComponent<MeshStrokeRenderer>();
                }
            }

            if (enableHistory)
            {
                historyManager = new DrawingHistoryManager(maxHistorySize);
            }

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.volume = 0.5f;

            strokeRenderer.Initialize(this);
        }

        private void Start()
        {
            if (drawingData.GetStrokeCount() > 0)
            {
                strokeRenderer.RebuildAllStrokes(drawingData);
            }
        }

        private void LateUpdate()
        {
            if (isDirty)
            {
                strokeRenderer.RebuildAllStrokes(drawingData);
                isDirty = false;
            }
        }

        public void RegisterTool(DrawingToolBase tool)
        {
            if (tool == null) return;

            tool.OnSurfaceTouched += OnToolEntered;
            tool.OnSurfaceDraw += OnToolStay;
            tool.OnSurfaceExited += OnToolExit;
        }

        public void UnregisterTool(DrawingToolBase tool)
        {
            if (tool == null) return;

            tool.OnSurfaceTouched -= OnToolEntered;
            tool.OnSurfaceDraw -= OnToolStay;
            tool.OnSurfaceExited -= OnToolExit;
        }

        private void OnToolEntered(DrawingToolBase tool, DrawingSurface surface, Vector3 worldPos)
        {
            if (surface != this || tool == null) return;

            if (tool.Type == ToolType.Eraser)
            {
                HandleEraserEnter(tool, worldPos);
            }
            else
            {
                HandleDrawToolEnter(tool, worldPos);
            }
        }

        private void OnToolStay(DrawingToolBase tool, DrawingSurface surface, Vector3 worldPos)
        {
            if (surface != this || tool == null) return;

            if (tool.Type == ToolType.Eraser)
            {
                HandleEraserStay(tool, worldPos);
            }
            else
            {
                HandleDrawToolStay(tool, worldPos);
            }
        }

        private void OnToolExit(DrawingToolBase tool, DrawingSurface surface)
        {
            if (surface != this || tool == null) return;

            if (activeStrokes.ContainsKey(tool))
            {
                Stroke completedStroke = activeStrokes[tool];
                activeStrokes.Remove(tool);

                if (completedStroke.IsValid())
                {
                    if (enableHistory && historyManager != null)
                    {
                        historyManager.RecordState(drawingData);
                    }

                    OnStrokeAdded?.Invoke(completedStroke);
                    OnHistoryChanged?.Invoke();
                }
            }
        }

        private void HandleDrawToolEnter(DrawingToolBase tool, Vector3 worldPos)
        {
            Vector2 uv = WorldToSurfaceUV(worldPos);
            
            Stroke newStroke = new Stroke(tool.Color, tool.Width, tool.ToolId);
            newStroke.AddPoint(uv);

            activeStrokes[tool] = newStroke;
            drawingData.AddStroke(newStroke);

            PlayAudio(drawingAudioClip);
        }

        private void HandleDrawToolStay(DrawingToolBase tool, Vector3 worldPos)
        {
            if (!activeStrokes.ContainsKey(tool)) return;

            Stroke currentStroke = activeStrokes[tool];
            Vector2 uv = WorldToSurfaceUV(worldPos);

            if (currentStroke.points.Count > 0)
            {
                Vector2 lastUV = currentStroke.points[currentStroke.points.Count - 1].uv;
                float distance = Vector2.Distance(lastUV, uv);

                if (distance < minPointDistance) return;
            }

            currentStroke.AddPoint(uv);
            strokeRenderer.UpdateStroke(currentStroke);
        }

        private void HandleEraserEnter(DrawingToolBase tool, Vector3 worldPos)
        {
            EraseStrokesAtPoint(worldPos, tool.Width);
        }

        private void HandleEraserStay(DrawingToolBase tool, Vector3 worldPos)
        {
            EraseStrokesAtPoint(worldPos, tool.Width);
        }

        private void EraseStrokesAtPoint(Vector3 worldPos, float eraserRadius)
        {
            Vector2 uv = WorldToSurfaceUV(worldPos);
            bool anyErased = false;

            for (int i = drawingData.strokes.Count - 1; i >= 0; i--)
            {
                Stroke stroke = drawingData.strokes[i];
                
                foreach (var point in stroke.points)
                {
                    float distance = Vector2.Distance(point.uv, uv);
                    if (distance < eraserRadius * 2f)
                    {
                        if (enableHistory && historyManager != null && !anyErased)
                        {
                            historyManager.RecordState(drawingData);
                            anyErased = true;
                        }

                        drawingData.RemoveStrokeAt(i);
                        OnStrokeRemoved?.Invoke(stroke);
                        isDirty = true;
                        
                        PlayAudio(eraseAudioClip);
                        break;
                    }
                }
            }

            if (anyErased)
            {
                OnHistoryChanged?.Invoke();
            }
        }

        public Vector2 WorldToSurfaceUV(Vector3 worldPos)
        {
            Vector3 localPos = transform.InverseTransformPoint(worldPos);
            
            float u = (localPos.x / surfaceSize.x) + 0.5f;
            float v = (localPos.y / surfaceSize.y) + 0.5f;

            return new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v));
        }

        public Vector3 SurfaceUVToWorld(Vector2 uv)
        {
            float x = (uv.x - 0.5f) * surfaceSize.x;
            float y = (uv.y - 0.5f) * surfaceSize.y;
            
            Vector3 localPos = new Vector3(x, y, 0f);
            return transform.TransformPoint(localPos);
        }

        public void Clear()
        {
            if (enableHistory && historyManager != null && drawingData.GetStrokeCount() > 0)
            {
                historyManager.RecordState(drawingData);
            }

            drawingData.Clear();
            activeStrokes.Clear();
            strokeRenderer.ClearAllStrokes();

            OnCleared?.Invoke();
            OnHistoryChanged?.Invoke();
            PlayAudio(clearAudioClip);
        }

        public void ClearAll()
        {
            Clear();
        }

        public void Undo()
        {
            if (historyManager == null || !historyManager.CanUndo) return;

            drawingData = historyManager.Undo(drawingData);
            isDirty = true;
            OnHistoryChanged?.Invoke();
        }

        public void Redo()
        {
            if (historyManager == null || !historyManager.CanRedo) return;

            drawingData = historyManager.Redo(drawingData);
            isDirty = true;
            OnHistoryChanged?.Invoke();
        }

        public bool CanUndo()
        {
            return historyManager != null && historyManager.CanUndo;
        }

        public bool CanRedo()
        {
            return historyManager != null && historyManager.CanRedo;
        }

        public bool SaveToFile(string filePath)
        {
            return drawingData.SaveToFile(filePath);
        }

        public bool LoadFromFile(string filePath)
        {
            DrawingData loaded = DrawingData.LoadFromFile(filePath);
            if (loaded != null)
            {
                if (enableHistory && historyManager != null)
                {
                    historyManager.RecordState(drawingData);
                }

                drawingData = loaded;
                isDirty = true;
                OnHistoryChanged?.Invoke();
                return true;
            }
            return false;
        }

        private void PlayAudio(AudioClip clip)
        {
            if (clip != null && audioSource != null && !audioSource.isPlaying)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(surfaceSize.x, surfaceSize.y, 0.01f));
        }
    }
}
