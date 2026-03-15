using UnityEngine;

namespace VRDrawing.Geology
{
    public enum SymbolCategory
    {
        Soil,
        MixedSoil,
        Rock
    }

    [CreateAssetMenu(menuName = "Geology/Symbol Definition", fileName = "Symbol_")]
    public class GeologicalSymbolDefinition : ScriptableObject
    {
        [Header("Identity")]
        /// <summary>Unique identifier used in code and serialization (e.g. SC, CL, Gr).</summary>
        public string id;

        /// <summary>Short label rendered on the board (e.g. SC).</summary>
        public string label;

        /// <summary>Human-readable name (e.g. Sandy Clay).</summary>
        public string fullName;

        [Header("Classification")]
        public SymbolCategory category;

        [Header("Display")]
        public Color displayColor = Color.white;

        /// <summary>Optional icon shown in the palette UI.</summary>
        public Sprite icon;

        [Header("Metadata")]
        [TextArea(2, 4)]
        public string description;
    }
}
