using UnityEngine;
using System.Collections.Generic;
using VRDrawing.Tools;

namespace VRDrawing
{
    public class DrawingSystemManager : MonoBehaviour
    {
        [Header("Auto-Registration")]
        [SerializeField] private bool autoRegisterTools = true;
        [SerializeField] private bool autoRegisterSurfaces = true;

        private List<DrawingToolBase> registeredTools = new List<DrawingToolBase>();
        private List<DrawingSurface> registeredSurfaces = new List<DrawingSurface>();

        public static DrawingSystemManager Instance { get; private set; }

        public IReadOnlyList<DrawingToolBase> Tools => registeredTools;
        public IReadOnlyList<DrawingSurface> Surfaces => registeredSurfaces;

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
        }

        private void Start()
        {
            if (autoRegisterTools)
            {
                DrawingToolBase[] tools = FindObjectsByType<DrawingToolBase>(FindObjectsSortMode.None);
                foreach (var tool in tools)
                {
                    RegisterTool(tool);
                }
            }

            if (autoRegisterSurfaces)
            {
                DrawingSurface[] surfaces = FindObjectsByType<DrawingSurface>(FindObjectsSortMode.None);
                foreach (var surface in surfaces)
                {
                    RegisterSurface(surface);
                }
            }
        }

        public void RegisterTool(DrawingToolBase tool)
        {
            if (tool == null || registeredTools.Contains(tool)) return;

            registeredTools.Add(tool);

            foreach (var surface in registeredSurfaces)
            {
                surface.RegisterTool(tool);
            }
        }

        public void UnregisterTool(DrawingToolBase tool)
        {
            if (tool == null) return;

            registeredTools.Remove(tool);

            foreach (var surface in registeredSurfaces)
            {
                surface.UnregisterTool(tool);
            }
        }

        public void RegisterSurface(DrawingSurface surface)
        {
            if (surface == null || registeredSurfaces.Contains(surface)) return;

            registeredSurfaces.Add(surface);

            foreach (var tool in registeredTools)
            {
                surface.RegisterTool(tool);
            }
        }

        public void UnregisterSurface(DrawingSurface surface)
        {
            if (surface == null) return;

            registeredSurfaces.Remove(surface);

            foreach (var tool in registeredTools)
            {
                surface.UnregisterTool(tool);
            }
        }

        public void ClearAllSurfaces()
        {
            foreach (var surface in registeredSurfaces)
            {
                surface.Clear();
            }
        }

        public void SaveAllSurfaces(string directoryPath)
        {
            if (!System.IO.Directory.Exists(directoryPath))
            {
                System.IO.Directory.CreateDirectory(directoryPath);
            }

            for (int i = 0; i < registeredSurfaces.Count; i++)
            {
                string filename = $"drawing_surface_{i}_{System.DateTime.Now:yyyyMMdd_HHmmss}.json";
                string fullPath = System.IO.Path.Combine(directoryPath, filename);
                registeredSurfaces[i].SaveToFile(fullPath);
            }
        }
    }
}
