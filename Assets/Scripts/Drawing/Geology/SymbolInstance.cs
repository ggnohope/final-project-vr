using UnityEngine;
using System;

namespace VRDrawing.Geology
{
    /// <summary>
    /// Represents a single geological symbol annotation placed on a drawing board.
    /// Stores the definition reference plus position/size metadata for later rendering and serialization.
    /// </summary>
    [Serializable]
    public class SymbolInstance
    {
        /// <summary>Unique id of the placed instance, generated at creation time.</summary>
        public string instanceId;

        /// <summary>Id of the GeologicalSymbolDefinition this instance references.</summary>
        public string symbolId;

        /// <summary>Normalized UV position on the drawing surface [0,1].</summary>
        public Vector2 surfaceUV;

        /// <summary>Uniform scale factor applied to the rendered symbol.</summary>
        public float scale;

        /// <summary>Rotation in degrees around the surface normal.</summary>
        public float rotationDegrees;

        /// <summary>UTC ticks when this annotation was placed.</summary>
        public long placedTimestamp;

        public SymbolInstance(string symbolId, Vector2 surfaceUV, float scale = 1f, float rotationDegrees = 0f)
        {
            instanceId = Guid.NewGuid().ToString();
            this.symbolId = symbolId;
            this.surfaceUV = surfaceUV;
            this.scale = scale;
            this.rotationDegrees = rotationDegrees;
            placedTimestamp = DateTime.UtcNow.Ticks;
        }

        /// <summary>Returns true if the instance holds a valid, non-empty symbol reference.</summary>
        public bool IsValid() => !string.IsNullOrEmpty(instanceId) && !string.IsNullOrEmpty(symbolId);
    }
}
