using UnityEngine;
using VRDrawing.Data;

namespace VRDrawing.Features
{
    public class DrawingNetworkSync : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private DrawingSurface targetSurface;

        [Header("Network Settings")]
        [SerializeField] private bool autoSync = true;
        [SerializeField] private float syncInterval = 0.1f;

        private float lastSyncTime;
        private int lastStrokeCount = 0;

        public System.Action<Stroke> OnStrokeSynced;

        private void OnEnable()
        {
            if (targetSurface != null)
            {
                targetSurface.OnStrokeAdded += OnLocalStrokeAdded;
            }
        }

        private void OnDisable()
        {
            if (targetSurface != null)
            {
                targetSurface.OnStrokeAdded -= OnLocalStrokeAdded;
            }
        }

        private void Update()
        {
            if (!autoSync || targetSurface == null) return;

            if (Time.time - lastSyncTime >= syncInterval)
            {
                CheckForNewStrokes();
                lastSyncTime = Time.time;
            }
        }

        private void OnLocalStrokeAdded(Stroke stroke)
        {
            if (!autoSync) return;
            BroadcastStroke(stroke);
        }

        private void CheckForNewStrokes()
        {
            if (targetSurface == null || targetSurface.Data == null) return;

            int currentCount = targetSurface.Data.GetStrokeCount();
            if (currentCount > lastStrokeCount)
            {
                for (int i = lastStrokeCount; i < currentCount; i++)
                {
                    BroadcastStroke(targetSurface.Data.strokes[i]);
                }
                lastStrokeCount = currentCount;
            }
        }

        private void BroadcastStroke(Stroke stroke)
        {
            OnStrokeSynced?.Invoke(stroke);
        }

        public void ReceiveStroke(Stroke stroke)
        {
            if (targetSurface != null && stroke != null)
            {
                targetSurface.Data.AddStroke(stroke.Clone());
                lastStrokeCount = targetSurface.Data.GetStrokeCount();
            }
        }

        public void ReceiveDrawingData(DrawingData data)
        {
            if (targetSurface != null && data != null)
            {
                foreach (var stroke in data.strokes)
                {
                    targetSurface.Data.AddStroke(stroke.Clone());
                }
                lastStrokeCount = targetSurface.Data.GetStrokeCount();
            }
        }

        public string SerializeStroke(Stroke stroke)
        {
            return JsonUtility.ToJson(stroke);
        }

        public Stroke DeserializeStroke(string json)
        {
            return JsonUtility.FromJson<Stroke>(json);
        }
    }
}
