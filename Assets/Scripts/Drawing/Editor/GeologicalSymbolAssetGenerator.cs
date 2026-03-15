using UnityEngine;
using UnityEditor;
using VRDrawing.Geology;
using System.IO;

namespace VRDrawing.Editor
{
    /// <summary>
    /// Generates all GeologicalSymbolDefinition assets and links them into a GeologicalSymbolDatabase.
    /// Run via Tools > Geology > Generate Symbol Assets.
    /// </summary>
    public static class GeologicalSymbolAssetGenerator
    {
        private const string SymbolOutputPath = "Assets/Resources/Geology/Symbols";
        private const string DatabaseOutputPath = "Assets/Resources/Geology";
        private const string DatabaseAssetName = "GeologicalSymbolDatabase";

        private struct SymbolData
        {
            public string Id;
            public string Label;
            public string FullName;
            public SymbolCategory Category;
            public Color Color;
            public string Description;
        }

        private static readonly SymbolData[] Symbols =
        {
            // ── Soils ──────────────────────────────────────────────────────────
            new SymbolData { Id = "S",  Label = "S",  FullName = "Sand",
                Category = SymbolCategory.Soil,
                Color = new Color(0.96f, 0.87f, 0.53f),
                Description = "Cohesionless granular material with particle size 0.063–2 mm." },

            new SymbolData { Id = "C",  Label = "C",  FullName = "Clay",
                Category = SymbolCategory.Soil,
                Color = new Color(0.60f, 0.45f, 0.30f),
                Description = "Fine-grained soil with particle size < 0.002 mm. High plasticity." },

            new SymbolData { Id = "Si", Label = "Si", FullName = "Silt",
                Category = SymbolCategory.Soil,
                Color = new Color(0.78f, 0.70f, 0.55f),
                Description = "Fine-grained soil with particle size 0.002–0.063 mm. Low plasticity." },

            new SymbolData { Id = "G",  Label = "G",  FullName = "Gravel",
                Category = SymbolCategory.Soil,
                Color = new Color(0.75f, 0.75f, 0.75f),
                Description = "Coarse-grained soil with particle size 2–60 mm." },

            // ── Mixed Soils ────────────────────────────────────────────────────
            new SymbolData { Id = "SC", Label = "SC", FullName = "Sandy Clay",
                Category = SymbolCategory.MixedSoil,
                Color = new Color(0.80f, 0.65f, 0.40f),
                Description = "Mixture of sand and clay. Plasticity governed by clay fraction." },

            new SymbolData { Id = "SM", Label = "SM", FullName = "Silty Sand",
                Category = SymbolCategory.MixedSoil,
                Color = new Color(0.87f, 0.79f, 0.53f),
                Description = "Sand with appreciable silt content. Low plasticity." },

            new SymbolData { Id = "CL", Label = "CL", FullName = "Low Plasticity Clay",
                Category = SymbolCategory.MixedSoil,
                Color = new Color(0.55f, 0.40f, 0.28f),
                Description = "Clay with liquid limit < 50. USCS group CL." },

            new SymbolData { Id = "CH", Label = "CH", FullName = "High Plasticity Clay",
                Category = SymbolCategory.MixedSoil,
                Color = new Color(0.40f, 0.25f, 0.15f),
                Description = "Clay with liquid limit ≥ 50. USCS group CH." },

            new SymbolData { Id = "ML", Label = "ML", FullName = "Low Plasticity Silt",
                Category = SymbolCategory.MixedSoil,
                Color = new Color(0.83f, 0.76f, 0.62f),
                Description = "Silt or sandy silt with liquid limit < 50. USCS group ML." },

            new SymbolData { Id = "MH", Label = "MH", FullName = "High Plasticity Silt",
                Category = SymbolCategory.MixedSoil,
                Color = new Color(0.70f, 0.60f, 0.45f),
                Description = "Elastic silt with liquid limit ≥ 50. USCS group MH." },

            // ── Rocks ──────────────────────────────────────────────────────────
            new SymbolData { Id = "Gr", Label = "Gr", FullName = "Granite",
                Category = SymbolCategory.Rock,
                Color = new Color(0.82f, 0.78f, 0.80f),
                Description = "Intrusive igneous rock. High compressive strength." },

            new SymbolData { Id = "Ba", Label = "Ba", FullName = "Basalt",
                Category = SymbolCategory.Rock,
                Color = new Color(0.30f, 0.30f, 0.32f),
                Description = "Extrusive igneous rock. Fine-grained, dark coloured." },

            new SymbolData { Id = "Sa", Label = "Sa", FullName = "Sandstone",
                Category = SymbolCategory.Rock,
                Color = new Color(0.90f, 0.75f, 0.50f),
                Description = "Sedimentary rock composed of sand-sized grains cemented together." },

            new SymbolData { Id = "Li", Label = "Li", FullName = "Limestone",
                Category = SymbolCategory.Rock,
                Color = new Color(0.92f, 0.92f, 0.85f),
                Description = "Sedimentary carbonate rock, primarily calcite." },

            new SymbolData { Id = "Sh", Label = "Sh", FullName = "Shale",
                Category = SymbolCategory.Rock,
                Color = new Color(0.50f, 0.48f, 0.45f),
                Description = "Fine-grained sedimentary rock. Laminated, fissile." },

            new SymbolData { Id = "Co", Label = "Co", FullName = "Conglomerate",
                Category = SymbolCategory.Rock,
                Color = new Color(0.70f, 0.60f, 0.50f),
                Description = "Sedimentary rock with rounded clasts > 2 mm in a fine matrix." },

            new SymbolData { Id = "Do", Label = "Do", FullName = "Dolomite",
                Category = SymbolCategory.Rock,
                Color = new Color(0.88f, 0.85f, 0.78f),
                Description = "Carbonate rock composed of calcium magnesium carbonate." },
        };

        [MenuItem("Tools/Geology/Generate Symbol Assets")]
        public static void GenerateAll()
        {
            EnsureDirectoryExists(SymbolOutputPath);
            EnsureDirectoryExists(DatabaseOutputPath);

            var database = LoadOrCreateDatabase();
            database.GetType()
                .GetField("symbols", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(database, new System.Collections.Generic.List<GeologicalSymbolDefinition>());

            var symbolList = new System.Collections.Generic.List<GeologicalSymbolDefinition>();

            foreach (var data in Symbols)
            {
                GeologicalSymbolDefinition def = CreateOrUpdateSymbol(data);
                symbolList.Add(def);
            }

            // Reflect the private list back in
            database.GetType()
                .GetField("symbols", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(database, symbolList);

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[GeologicalSymbolAssetGenerator] Generated {symbolList.Count} symbol assets and updated database.");
        }

        private static GeologicalSymbolDefinition CreateOrUpdateSymbol(SymbolData data)
        {
            string assetPath = $"{SymbolOutputPath}/Symbol_{data.Id}.asset";
            GeologicalSymbolDefinition existing =
                AssetDatabase.LoadAssetAtPath<GeologicalSymbolDefinition>(assetPath);

            if (existing == null)
            {
                existing = ScriptableObject.CreateInstance<GeologicalSymbolDefinition>();
                AssetDatabase.CreateAsset(existing, assetPath);
            }

            existing.id = data.Id;
            existing.label = data.Label;
            existing.fullName = data.FullName;
            existing.category = data.Category;
            existing.displayColor = data.Color;
            existing.description = data.Description;

            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static GeologicalSymbolDatabase LoadOrCreateDatabase()
        {
            string dbPath = $"{DatabaseOutputPath}/{DatabaseAssetName}.asset";
            GeologicalSymbolDatabase db =
                AssetDatabase.LoadAssetAtPath<GeologicalSymbolDatabase>(dbPath);

            if (db == null)
            {
                db = ScriptableObject.CreateInstance<GeologicalSymbolDatabase>();
                AssetDatabase.CreateAsset(db, dbPath);
            }

            return db;
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}
