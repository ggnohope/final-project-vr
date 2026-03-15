using UnityEngine;
using System.Collections.Generic;

namespace VRDrawing.Geology
{
    [CreateAssetMenu(menuName = "Geology/Symbol Database", fileName = "GeologicalSymbolDatabase")]
    public class GeologicalSymbolDatabase : ScriptableObject
    {
        [SerializeField] private List<GeologicalSymbolDefinition> symbols = new List<GeologicalSymbolDefinition>();

        /// <summary>All registered symbol definitions.</summary>
        public IReadOnlyList<GeologicalSymbolDefinition> Symbols => symbols;

        /// <summary>Returns all symbols belonging to the given category.</summary>
        public IReadOnlyList<GeologicalSymbolDefinition> GetByCategory(SymbolCategory category)
        {
            var result = new List<GeologicalSymbolDefinition>();
            foreach (var symbol in symbols)
            {
                if (symbol != null && symbol.category == category)
                    result.Add(symbol);
            }
            return result;
        }

        /// <summary>Finds a symbol by its unique id. Returns null if not found.</summary>
        public GeologicalSymbolDefinition FindById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            foreach (var symbol in symbols)
            {
                if (symbol != null && symbol.id == id)
                    return symbol;
            }
            return null;
        }

#if UNITY_EDITOR
        /// <summary>Editor-only: validates that all entries are unique by id.</summary>
        public void ValidateUniqueness()
        {
            var seen = new HashSet<string>();
            foreach (var symbol in symbols)
            {
                if (symbol == null) continue;
                if (!seen.Add(symbol.id))
                    Debug.LogWarning($"[GeologicalSymbolDatabase] Duplicate symbol id '{symbol.id}' detected.", this);
            }
        }
#endif
    }
}
