using UnityEngine;
using System.Collections.Generic;

namespace VRDrawing.Data
{
    [System.Serializable]
    public class Stroke
    {
        public List<StrokePoint> points = new List<StrokePoint>();
        public Color color = Color.black;
        public float width = 0.01f;
        public long timestampTicks;
        public string toolId = "pen";

        public Stroke()
        {
            timestampTicks = System.DateTime.UtcNow.Ticks;
        }

        public Stroke(Color color, float width, string toolId = "pen")
        {
            this.color = color;
            this.width = width;
            this.toolId = toolId;
            timestampTicks = System.DateTime.UtcNow.Ticks;
        }

        public void AddPoint(Vector2 uv, float pressure = 1f)
        {
            points.Add(new StrokePoint(uv, pressure));
        }

        public void AddPoint(StrokePoint point)
        {
            points.Add(point);
        }

        public bool IsValid()
        {
            return points != null && points.Count >= 2;
        }

        public float GetLength()
        {
            if (points.Count < 2) return 0f;

            float length = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                length += Vector2.Distance(points[i - 1].uv, points[i].uv);
            }
            return length;
        }

        public Stroke Clone()
        {
            Stroke clone = new Stroke(color, width, toolId);
            clone.timestampTicks = timestampTicks;
            clone.points = new List<StrokePoint>(points);
            return clone;
        }
    }
}
