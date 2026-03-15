using UnityEngine;
using System.Collections.Generic;

namespace VRDrawing.Geology
{
    /// <summary>
    /// Runtime state for one annotation layer, corresponding to one SymbolCategory.
    /// Controls visibility of all SymbolInstances that belong to this category.
    /// </summary>
    public class AnnotationLayerState
    {
        public readonly SymbolCategory Category;
        public readonly string DisplayName;

        private bool isVisible = true;

        public bool IsVisible => isVisible;

        /// <summary>Raised when visibility changes. Passes the new visibility state.</summary>
        public System.Action<AnnotationLayerState, bool> OnVisibilityChanged;

        public AnnotationLayerState(SymbolCategory category)
        {
            Category = category;
            DisplayName = CategoryDisplayName(category);
        }

        /// <summary>Toggles layer visibility and raises OnVisibilityChanged.</summary>
        public void SetVisible(bool visible)
        {
            if (isVisible == visible) return;
            isVisible = visible;
            OnVisibilityChanged?.Invoke(this, isVisible);
        }

        private static string CategoryDisplayName(SymbolCategory category)
        {
            return category switch
            {
                SymbolCategory.Soil      => "Soils",
                SymbolCategory.MixedSoil => "Mixed Soils",
                SymbolCategory.Rock      => "Rocks",
                _                        => category.ToString()
            };
        }
    }
}
