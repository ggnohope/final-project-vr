using UnityEngine;

namespace VRDrawing.Data
{
    [System.Serializable]
    public struct StrokePoint
    {
        public Vector2 uv;
        public float pressure;
        public float timestamp;

        public StrokePoint(Vector2 uv, float pressure = 1f)
        {
            this.uv = uv;
            this.pressure = pressure;
            this.timestamp = Time.time;
        }

        public StrokePoint(float u, float v, float pressure = 1f)
        {
            this.uv = new Vector2(u, v);
            this.pressure = pressure;
            this.timestamp = Time.time;
        }
    }
}
