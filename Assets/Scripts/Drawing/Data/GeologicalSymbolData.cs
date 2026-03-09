using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRDrawing.Data
{
    /// <summary>Category of a geological symbol for layer-based toggling.</summary>
    public enum SymbolCategory
    {
        Soil,
        Rock,
        Note
    }

    /// <summary>Immutable definition of a single geological symbol type.</summary>
    [Serializable]
    public class GeologicalSymbolDefinition
    {
        public string code;
        public string fullName;
        public SymbolCategory category;
        public Color color;

        public GeologicalSymbolDefinition(string code, string fullName, SymbolCategory category, Color color)
        {
            this.code = code;
            this.fullName = fullName;
            this.category = category;
            this.color = color;
        }
    }

    /// <summary>
    /// Runtime data for one placed symbol annotation.
    /// Coordinates are normalised (0–1) relative to the board.
    /// </summary>
    [Serializable]
    public class PlacedSymbolData
    {
        public string id;
        public string type;
        public float x;
        public float y;
        public string note;

        public PlacedSymbolData(string type, float x, float y)
        {
            id = Guid.NewGuid().ToString();
            this.type = type;
            this.x = x;
            this.y = y;
            note = string.Empty;
        }
    }

    /// <summary>Full annotation board saved to JSON.</summary>
    [Serializable]
    public class AnnotationBoardData
    {
        public string image;
        public List<PlacedSymbolData> symbols = new List<PlacedSymbolData>();
    }

    /// <summary>
    /// Central registry of all supported geological symbol definitions.
    /// </summary>
    public static class GeologicalSymbolRegistry
    {
        private static readonly List<GeologicalSymbolDefinition> AllDefinitions =
            new List<GeologicalSymbolDefinition>
        {
            // ── Soil – pure ────────────────────────────────────────────────
            new GeologicalSymbolDefinition("S",  "Sand",             SymbolCategory.Soil, new Color(1.0f, 0.85f, 0.0f)),
            new GeologicalSymbolDefinition("C",  "Clay",             SymbolCategory.Soil, new Color(0.55f, 0.27f, 0.07f)),
            new GeologicalSymbolDefinition("Si", "Silt",             SymbolCategory.Soil, new Color(1.0f, 0.55f, 0.0f)),
            new GeologicalSymbolDefinition("G",  "Gravel",           SymbolCategory.Soil, new Color(0.60f, 0.60f, 0.60f)),

            // ── Soil – mixed ────────────────────────────────────────────────
            new GeologicalSymbolDefinition("SC", "Sandy Clay",       SymbolCategory.Soil, new Color(0.80f, 0.60f, 0.10f)),
            new GeologicalSymbolDefinition("SM", "Silty Sand",       SymbolCategory.Soil, new Color(1.0f, 0.70f, 0.0f)),
            new GeologicalSymbolDefinition("CL", "Low Plasticity Clay",  SymbolCategory.Soil, new Color(0.65f, 0.33f, 0.10f)),
            new GeologicalSymbolDefinition("CH", "High Plasticity Clay", SymbolCategory.Soil, new Color(0.45f, 0.18f, 0.04f)),
            new GeologicalSymbolDefinition("ML", "Low Plasticity Silt",  SymbolCategory.Soil, new Color(1.0f, 0.65f, 0.1f)),
            new GeologicalSymbolDefinition("MH", "High Plasticity Silt", SymbolCategory.Soil, new Color(0.90f, 0.45f, 0.0f)),

            // ── Rock ────────────────────────────────────────────────────────
            new GeologicalSymbolDefinition("Gr", "Granite",        SymbolCategory.Rock, new Color(0.20f, 0.40f, 0.90f)),
            new GeologicalSymbolDefinition("Ba", "Basalt",         SymbolCategory.Rock, new Color(0.15f, 0.30f, 0.75f)),
            new GeologicalSymbolDefinition("Sa", "Sandstone",      SymbolCategory.Rock, new Color(0.25f, 0.50f, 0.95f)),
            new GeologicalSymbolDefinition("Li", "Limestone",      SymbolCategory.Rock, new Color(0.30f, 0.55f, 1.0f)),
            new GeologicalSymbolDefinition("Sh", "Shale",          SymbolCategory.Rock, new Color(0.18f, 0.35f, 0.80f)),
            new GeologicalSymbolDefinition("Co", "Conglomerate",   SymbolCategory.Rock, new Color(0.22f, 0.45f, 0.85f)),
            new GeologicalSymbolDefinition("Do", "Dolomite",       SymbolCategory.Rock, new Color(0.28f, 0.52f, 0.92f)),
        };

        /// <summary>Returns all registered definitions.</summary>
        public static IReadOnlyList<GeologicalSymbolDefinition> GetAll() => AllDefinitions;

        /// <summary>Finds a definition by its short code (case-sensitive).</summary>
        public static GeologicalSymbolDefinition Find(string code)
        {
            foreach (var def in AllDefinitions)
                if (def.code == code) return def;
            return null;
        }

        /// <summary>Returns all definitions belonging to a specific category.</summary>
        public static List<GeologicalSymbolDefinition> GetByCategory(SymbolCategory category)
        {
            var result = new List<GeologicalSymbolDefinition>();
            foreach (var def in AllDefinitions)
                if (def.category == category) result.Add(def);
            return result;
        }
    }
}
