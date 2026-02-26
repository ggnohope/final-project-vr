using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace VRDrawing.Data
{
    [System.Serializable]
    public class DrawingData
    {
        public List<Stroke> strokes = new List<Stroke>();
        public string version = "1.0";
        public long createdTimestamp;
        public long modifiedTimestamp;

        public DrawingData()
        {
            createdTimestamp = System.DateTime.UtcNow.Ticks;
            modifiedTimestamp = createdTimestamp;
        }

        public void AddStroke(Stroke stroke)
        {
            if (stroke != null && stroke.IsValid())
            {
                strokes.Add(stroke);
                UpdateModifiedTimestamp();
            }
        }

        public void RemoveStroke(Stroke stroke)
        {
            if (strokes.Remove(stroke))
            {
                UpdateModifiedTimestamp();
            }
        }

        public void RemoveStrokeAt(int index)
        {
            if (index >= 0 && index < strokes.Count)
            {
                strokes.RemoveAt(index);
                UpdateModifiedTimestamp();
            }
        }

        public void Clear()
        {
            strokes.Clear();
            UpdateModifiedTimestamp();
        }

        public int GetStrokeCount()
        {
            return strokes.Count;
        }

        public int GetTotalPointCount()
        {
            int total = 0;
            foreach (var stroke in strokes)
            {
                total += stroke.points.Count;
            }
            return total;
        }

        public DrawingData Clone()
        {
            DrawingData clone = new DrawingData();
            clone.version = version;
            clone.createdTimestamp = createdTimestamp;
            clone.modifiedTimestamp = modifiedTimestamp;
            
            foreach (var stroke in strokes)
            {
                clone.strokes.Add(stroke.Clone());
            }
            
            return clone;
        }

        private void UpdateModifiedTimestamp()
        {
            modifiedTimestamp = System.DateTime.UtcNow.Ticks;
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }

        public static DrawingData FromJson(string json)
        {
            try
            {
                return JsonUtility.FromJson<DrawingData>(json);
            }
            catch (System.Exception e)
            {
                _ = e;
                return new DrawingData();
            }
        }

        public bool SaveToFile(string filePath)
        {
            try
            {
                string json = ToJson();
                File.WriteAllText(filePath, json);
                return true;
            }
            catch (System.Exception e)
            {
                _ = e;
                return false;
            }
        }

        public static DrawingData LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return new DrawingData();
                }

                string json = File.ReadAllText(filePath);
                return FromJson(json);
            }
            catch (System.Exception e)
            {
                _ = e;
                return new DrawingData();
            }
        }
    }
}
