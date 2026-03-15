using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using VRDrawing.Geology;
using VRDrawing.Geology.UI;
using System.IO;

namespace VRDrawing.Editor
{
    /// <summary>
    /// Generates the full Symbol Palette canvas, Annotation Legend canvas,
    /// and their three child-prefabs (SymbolPaletteButton, LayerToggleRow, LegendRow).
    ///
    /// Menu: Tools > Geology > Generate Annotation Canvas Prefabs
    /// </summary>
    public static class AnnotationCanvasPrefabGenerator
    {
        // ── Output paths ────────────────────────────────────────────────────
        private const string PrefabDir  = "Assets/Prefabs/Drawing";
        private const string FontPath   = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        // Canvas world-space sizes (in meters at 1000 ppu)
        private const float PaletteWidth  = 0.60f;
        private const float PaletteHeight = 0.80f;
        private const float LegendWidth   = 0.50f;
        private const float LegendHeight  = 0.80f;

        // Color palette
        private static readonly Color BgDark        = new Color(0.12f, 0.12f, 0.14f, 0.97f);
        private static readonly Color BgMid         = new Color(0.18f, 0.18f, 0.20f, 1.00f);
        private static readonly Color BgLight       = new Color(0.22f, 0.22f, 0.25f, 1.00f);
        private static readonly Color AccentBlue    = new Color(0.25f, 0.55f, 0.95f, 1.00f);
        private static readonly Color TextPrimary   = new Color(0.92f, 0.92f, 0.95f, 1.00f);
        private static readonly Color TextSecondary = new Color(0.60f, 0.62f, 0.68f, 1.00f);
        private static readonly Color TabActive     = new Color(0.22f, 0.22f, 0.26f, 1.00f);
        private static readonly Color TabInactive   = new Color(0.15f, 0.15f, 0.17f, 1.00f);
        private static readonly Color ToggleOn      = new Color(0.25f, 0.75f, 0.45f, 1.00f);
        private static readonly Color DangerRed     = new Color(0.85f, 0.25f, 0.25f, 1.00f);
        private static readonly Color SelectedBorder= new Color(0.25f, 0.55f, 0.95f, 1.00f);

        // ── Entry point ─────────────────────────────────────────────────────

        [MenuItem("Tools/Geology/Generate Annotation Canvas Prefabs")]
        public static void GenerateAll()
        {
            EnsureDir(PrefabDir);

            TMP_FontAsset font = LoadFont();

            // Build sub-prefabs first (referenced by canvases)
            GameObject symbolButtonPrefab = BuildSymbolButtonPrefab(font);
            GameObject layerToggleRowPrefab = BuildLayerToggleRowPrefab(font);
            GameObject legendRowPrefab = BuildLegendRowPrefab(font);

            // Build main canvases
            BuildSymbolPaletteCanvas(font, symbolButtonPrefab);
            BuildAnnotationLegendCanvas(font, layerToggleRowPrefab, legendRowPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[AnnotationCanvasPrefabGenerator] All annotation prefabs generated successfully.");
            EditorUtility.DisplayDialog("Done", "Annotation canvas prefabs generated in Assets/Prefabs/Drawing/", "OK");
        }

        // ── Symbol Palette Canvas ────────────────────────────────────────────

        private static void BuildSymbolPaletteCanvas(TMP_FontAsset font, GameObject symbolButtonPrefab)
        {
            const string assetName = "SymbolPaletteCanvas";
            string path = $"{PrefabDir}/{assetName}.prefab";

            // Root is a plain, no-component GameObject.
            // Canvas sits as a child — this prevents Unity from adding "Canvas (Environment)" wrapper.
            GameObject root = new GameObject(assetName);

            // ── Canvas child ─────────────────────────────────────────────────
            GameObject canvasObj = new GameObject("Canvas");
            canvasObj.transform.SetParent(root.transform, false);

            RectTransform canvasRT = canvasObj.AddComponent<RectTransform>();
            canvasRT.sizeDelta  = new Vector2(PaletteWidth * 1000f, PaletteHeight * 1000f);
            canvasRT.localScale = new Vector3(0.001f, 0.001f, 0.001f);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.WorldSpace;
            canvas.sortingOrder = 50;
            canvas.sortingLayerName = "UI";

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 1000f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Background panel
            GameObject bg = CreatePanel(canvasObj.transform, "Background", BgDark,
                Vector2.zero, new Vector2(PaletteWidth * 1000f, PaletteHeight * 1000f));
            AddRoundedOutline(bg, AccentBlue, 2f);
            Transform bgT = bg.transform;

            // ── Header ───────────────────────────────────────────────────────
            float halfH = PaletteHeight * 1000f * 0.5f;
            GameObject header = CreatePanel(bgT, "Header", BgMid,
                new Vector2(0f, halfH - 40f), new Vector2(PaletteWidth * 1000f, 80f));
            CreateTMPLabel(header.transform, "TitleLabel", "Geological Symbols",
                new Vector2(-30f, 0f), new Vector2(400f, 60f), font, 22f, TextPrimary, TextAnchor.MiddleLeft, FontStyles.Bold);
            Button closeBtn = CreateIconButton(header.transform, "CloseButton",
                new Vector2(PaletteWidth * 1000f * 0.5f - 35f, 0f), new Vector2(50f, 50f), "✕", font, 20f, DangerRed);

            // ── Tab bar ──────────────────────────────────────────────────────
            float tabY = halfH - 40f - 80f - 25f;
            GameObject tabBar = CreatePanel(bgT, "TabBar", BgMid,
                new Vector2(0f, tabY), new Vector2(PaletteWidth * 1000f, 50f));
            float tabW = PaletteWidth * 1000f / 3f - 4f;
            Button tabSoil  = CreateTabButton(tabBar.transform, "TabSoil",
                new Vector2(-PaletteWidth * 1000f / 3f, 0f), new Vector2(tabW, 42f), "Soil",  font, TabActive, TabInactive);
            Button tabMixed = CreateTabButton(tabBar.transform, "TabMixed",
                new Vector2(0f, 0f),                          new Vector2(tabW, 42f), "Mixed", font, TabActive, TabInactive);
            Button tabRock  = CreateTabButton(tabBar.transform, "TabRock",
                new Vector2(PaletteWidth * 1000f / 3f, 0f),  new Vector2(tabW, 42f), "Rock",  font, TabActive, TabInactive);

            // ── Scroll / grid ────────────────────────────────────────────────
            float scrollH = PaletteHeight * 1000f - 80f - 50f - 60f - 20f;
            float scrollY = -80f - 50f - scrollH * 0.5f + 10f;
            GameObject scrollView = CreateScrollView(bgT, "GridScrollView",
                new Vector2(0f, scrollY + 30f), new Vector2(PaletteWidth * 1000f - 20f, scrollH));
            Transform gridContent = scrollView.transform.Find("Viewport/Content");
            if (gridContent != null)
            {
                GridLayoutGroup grid = gridContent.gameObject.AddComponent<GridLayoutGroup>();
                grid.cellSize        = new Vector2(170f, 80f);
                grid.spacing         = new Vector2(8f, 8f);
                grid.padding         = new RectOffset(8, 8, 8, 8);
                grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 3;
                grid.childAlignment  = TextAnchor.UpperLeft;
                ContentSizeFitter csf = gridContent.gameObject.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            // ── Status bar ───────────────────────────────────────────────────
            float statusY = -halfH + 35f;
            GameObject statusBar = CreatePanel(bgT, "StatusBar", BgMid,
                new Vector2(0f, statusY), new Vector2(PaletteWidth * 1000f, 60f));
            statusBar.SetActive(false);
            CreateTMPLabel(statusBar.transform, "StatusLabel", "Placing: <b>—</b>",
                new Vector2(-40f, 0f), new Vector2(PaletteWidth * 1000f - 160f, 50f),
                font, 14f, TextPrimary, TextAnchor.MiddleLeft, FontStyles.Normal);
            Button cancelBtn = CreateTextButton(statusBar.transform, "CancelButton",
                new Vector2(PaletteWidth * 1000f * 0.5f - 70f, 0f), new Vector2(120f, 42f), "Cancel", font, 14f, DangerRed);

            // ── Wire SymbolPaletteUI on Canvas child ─────────────────────────
            SymbolPaletteUI paletteUI = canvasObj.AddComponent<SymbolPaletteUI>();
            SerializedObject so = new SerializedObject(paletteUI);
            so.FindProperty("tabSoil").objectReferenceValue           = tabSoil;
            so.FindProperty("tabMixed").objectReferenceValue          = tabMixed;
            so.FindProperty("tabRock").objectReferenceValue           = tabRock;
            so.FindProperty("tabActiveColor").colorValue              = TabActive;
            so.FindProperty("tabInactiveColor").colorValue            = TabInactive;
            so.FindProperty("gridContent").objectReferenceValue       = gridContent;
            so.FindProperty("symbolButtonPrefab").objectReferenceValue= symbolButtonPrefab;
            so.FindProperty("statusBar").objectReferenceValue         = statusBar;
            so.FindProperty("statusLabel").objectReferenceValue       =
                statusBar.transform.Find("StatusLabel")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("cancelButton").objectReferenceValue      = cancelBtn;
            so.FindProperty("closeButton").objectReferenceValue       = closeBtn;
            so.FindProperty("offsetX").floatValue = -0.72f;
            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, path);
            Object.DestroyImmediate(root);
        }

        // ── Annotation Legend Canvas ─────────────────────────────────────────

        private static void BuildAnnotationLegendCanvas(TMP_FontAsset font,
            GameObject layerToggleRowPrefab, GameObject legendRowPrefab)
        {
            const string assetName = "AnnotationLegendCanvas";
            string path = $"{PrefabDir}/{assetName}.prefab";

            // Root is a plain, no-component GameObject — prevents "Canvas (Environment)" wrapper.
            GameObject root = new GameObject(assetName);

            // ── Canvas child ─────────────────────────────────────────────────
            GameObject canvasObj = new GameObject("Canvas");
            canvasObj.transform.SetParent(root.transform, false);

            RectTransform canvasRT = canvasObj.AddComponent<RectTransform>();
            canvasRT.sizeDelta  = new Vector2(LegendWidth * 1000f, LegendHeight * 1000f);
            canvasRT.localScale = new Vector3(0.001f, 0.001f, 0.001f);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode       = RenderMode.WorldSpace;
            canvas.sortingOrder     = 50;
            canvas.sortingLayerName = "UI";

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 1000f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Background
            GameObject bg = CreatePanel(canvasObj.transform, "Background", BgDark,
                Vector2.zero, new Vector2(LegendWidth * 1000f, LegendHeight * 1000f));
            AddRoundedOutline(bg, AccentBlue, 2f);
            Transform bgT = bg.transform;

            // ── Header ───────────────────────────────────────────────────────
            float halfH = LegendHeight * 1000f * 0.5f;
            GameObject header = CreatePanel(bgT, "Header", BgMid,
                new Vector2(0f, halfH - 40f), new Vector2(LegendWidth * 1000f, 80f));
            CreateTMPLabel(header.transform, "TitleLabel", "Legend",
                new Vector2(-30f, 0f), new Vector2(300f, 60f), font, 22f, TextPrimary, TextAnchor.MiddleLeft, FontStyles.Bold);
            Button closeBtn = CreateIconButton(header.transform, "CloseButton",
                new Vector2(LegendWidth * 1000f * 0.5f - 35f, 0f), new Vector2(50f, 50f), "✕", font, 20f, DangerRed);

            // ── Layer Control section ─────────────────────────────────────────
            float sectionLabelY = halfH - 80f - 20f;
            CreateSectionDivider(bgT, "LayerControlDivider", "Layer Control",
                new Vector2(0f, sectionLabelY), LegendWidth * 1000f, font);

            float layerContainerH = 120f;
            float layerContainerY = sectionLabelY - 20f - layerContainerH * 0.5f;
            GameObject layerContainer = CreatePanel(bgT, "LayerControlContainer", Color.clear,
                new Vector2(0f, layerContainerY), new Vector2(LegendWidth * 1000f - 20f, layerContainerH));
            VerticalLayoutGroup layerVLG = layerContainer.AddComponent<VerticalLayoutGroup>();
            layerVLG.spacing             = 4f;
            layerVLG.childControlHeight  = false;
            layerVLG.childControlWidth   = true;
            layerVLG.childForceExpandWidth = true;
            layerVLG.padding             = new RectOffset(8, 8, 4, 4);
            ContentSizeFitter layerCSF   = layerContainer.AddComponent<ContentSizeFitter>();
            layerCSF.verticalFit         = ContentSizeFitter.FitMode.PreferredSize;

            // ── Placed Symbols section ────────────────────────────────────────
            float placedDividerY = layerContainerY - layerContainerH * 0.5f - 24f;
            CreateSectionDivider(bgT, "PlacedDivider", "Placed Symbols",
                new Vector2(0f, placedDividerY), LegendWidth * 1000f, font);

            float scrollH = LegendHeight * 1000f - 80f - 30f - layerContainerH - 30f - 60f - 20f;
            float scrollY = placedDividerY - 20f - scrollH * 0.5f;
            GameObject scrollView = CreateScrollView(bgT, "LegendScrollView",
                new Vector2(0f, scrollY), new Vector2(LegendWidth * 1000f - 20f, scrollH));
            Transform legendContent = scrollView.transform.Find("Viewport/Content");
            if (legendContent != null)
            {
                VerticalLayoutGroup vlg = legendContent.gameObject.AddComponent<VerticalLayoutGroup>();
                vlg.spacing             = 4f;
                vlg.childControlHeight  = false;
                vlg.childControlWidth   = true;
                vlg.childForceExpandWidth = true;
                vlg.padding             = new RectOffset(4, 4, 4, 4);
                ContentSizeFitter csf   = legendContent.gameObject.AddComponent<ContentSizeFitter>();
                csf.verticalFit         = ContentSizeFitter.FitMode.PreferredSize;
            }

            // ── Footer ───────────────────────────────────────────────────────
            float footerY = -halfH + 35f;
            GameObject footer = CreatePanel(bgT, "Footer", BgMid,
                new Vector2(0f, footerY), new Vector2(LegendWidth * 1000f, 60f));
            Button clearAllBtn = CreateTextButton(footer.transform, "ClearAllButton",
                Vector2.zero, new Vector2(160f, 40f), "Clear All", font, 15f, DangerRed);

            // ── Wire AnnotationLegendUI on Canvas child ───────────────────────
            AnnotationLegendUI legendUI = canvasObj.AddComponent<AnnotationLegendUI>();
            SerializedObject so = new SerializedObject(legendUI);
            so.FindProperty("layerControlContainer").objectReferenceValue  = layerContainer.transform;
            so.FindProperty("layerToggleRowPrefab").objectReferenceValue   = layerToggleRowPrefab;
            so.FindProperty("legendRowContainer").objectReferenceValue     = legendContent;
            so.FindProperty("legendRowPrefab").objectReferenceValue        = legendRowPrefab;
            so.FindProperty("clearAllButton").objectReferenceValue         = clearAllBtn;
            so.FindProperty("closeButton").objectReferenceValue            = closeBtn;
            so.FindProperty("offsetX").floatValue = 0.72f;
            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, path);
            Object.DestroyImmediate(root);
        }

        // ── Symbol Button Sub-prefab ─────────────────────────────────────────

        private static GameObject BuildSymbolButtonPrefab(TMP_FontAsset font)
        {
            const string assetName = "SymbolPaletteButton";
            string path = $"{PrefabDir}/{assetName}.prefab";

            GameObject root = new GameObject(assetName);
            RectTransform rootRT = root.AddComponent<RectTransform>();
            rootRT.sizeDelta = new Vector2(170f, 80f);

            Image bg = root.AddComponent<Image>();
            bg.color = BgMid;

            Button btn = root.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor      = BgMid;
            cb.highlightedColor = BgLight;
            cb.pressedColor     = AccentBlue;
            cb.selectedColor    = TabActive;
            btn.colors = cb;

            // Color swatch (left strip)
            GameObject swatchObj = new GameObject("ColorSwatch");
            swatchObj.transform.SetParent(root.transform, false);
            RectTransform swatchRT = swatchObj.AddComponent<RectTransform>();
            swatchRT.anchorMin  = new Vector2(0f, 0f);
            swatchRT.anchorMax  = new Vector2(0f, 1f);
            swatchRT.offsetMin  = new Vector2(0f, 0f);
            swatchRT.offsetMax  = new Vector2(14f, 0f);
            Image swatchImg = swatchObj.AddComponent<Image>();
            swatchImg.color = Color.white;

            // Label (symbol id)
            GameObject labelObj = new GameObject("LabelText");
            labelObj.transform.SetParent(root.transform, false);
            RectTransform labelRT = labelObj.AddComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0f, 0.5f);
            labelRT.anchorMax = new Vector2(0f, 0.5f);
            labelRT.pivot     = new Vector2(0f, 0.5f);
            labelRT.anchoredPosition = new Vector2(18f, 10f);
            labelRT.sizeDelta = new Vector2(60f, 28f);
            TextMeshProUGUI labelTMP = labelObj.AddComponent<TextMeshProUGUI>();
            labelTMP.text      = "SC";
            labelTMP.fontSize  = 18f;
            labelTMP.fontStyle = FontStyles.Bold;
            labelTMP.color     = TextPrimary;
            labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
            if (font != null) labelTMP.font = font;

            // Full name text
            GameObject nameObj = new GameObject("FullNameText");
            nameObj.transform.SetParent(root.transform, false);
            RectTransform nameRT = nameObj.AddComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0f, 0f);
            nameRT.anchorMax = new Vector2(1f, 0f);
            nameRT.pivot     = new Vector2(0f, 0f);
            nameRT.anchoredPosition = new Vector2(18f, 4f);
            nameRT.sizeDelta = new Vector2(-22f, 22f);
            TextMeshProUGUI nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
            nameTMP.text      = "Sandy Clay";
            nameTMP.fontSize  = 11f;
            nameTMP.color     = TextSecondary;
            nameTMP.alignment = TextAlignmentOptions.MidlineLeft;
            nameTMP.overflowMode = TextOverflowModes.Ellipsis;
            if (font != null) nameTMP.font = font;

            // Selected indicator (bright border)
            GameObject selObj = new GameObject("SelectedIndicator");
            selObj.transform.SetParent(root.transform, false);
            RectTransform selRT = selObj.AddComponent<RectTransform>();
            selRT.anchorMin = Vector2.zero;
            selRT.anchorMax = Vector2.one;
            selRT.offsetMin = Vector2.zero;
            selRT.offsetMax = Vector2.zero;
            Image selImg = selObj.AddComponent<Image>();
            selImg.color = SelectedBorder;
            selImg.type  = Image.Type.Sliced;
            selObj.SetActive(false);

            SymbolPaletteButtonUI buttonUI = root.AddComponent<SymbolPaletteButtonUI>();
            SerializedObject so = new SerializedObject(buttonUI);
            so.FindProperty("colorSwatch").objectReferenceValue      = swatchImg;
            so.FindProperty("labelText").objectReferenceValue        = labelTMP;
            so.FindProperty("fullNameText").objectReferenceValue     = nameTMP;
            so.FindProperty("selectedIndicator").objectReferenceValue = selObj;
            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, path);
            GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Object.DestroyImmediate(root);
            return saved;
        }

        // ── Layer Toggle Row Sub-prefab ──────────────────────────────────────

        private static GameObject BuildLayerToggleRowPrefab(TMP_FontAsset font)
        {
            const string assetName = "LayerToggleRow";
            string path = $"{PrefabDir}/{assetName}.prefab";

            GameObject root = new GameObject(assetName);
            RectTransform rootRT = root.AddComponent<RectTransform>();
            rootRT.sizeDelta = new Vector2(460f, 36f);
            Image bg = root.AddComponent<Image>();
            bg.color = BgLight;

            // Use a simple horizontal layout but lock toggle to the right edge via anchor.
            HorizontalLayoutGroup hlg = root.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing            = 8f;
            hlg.padding            = new RectOffset(10, 10, 4, 4);
            hlg.childAlignment     = TextAnchor.MiddleLeft;
            hlg.childControlHeight = true;
            hlg.childControlWidth  = false;
            hlg.childForceExpandWidth = false;

            // ── Category label (fills remaining space) ───────────────────────
            GameObject labelObj = new GameObject("CategoryLabel");
            labelObj.transform.SetParent(root.transform, false);
            labelObj.AddComponent<RectTransform>().sizeDelta = new Vector2(280f, 28f);
            LayoutElement labelLE = labelObj.AddComponent<LayoutElement>();
            labelLE.preferredWidth = 280f;
            labelLE.flexibleWidth  = 1f;
            TextMeshProUGUI labelTMP = labelObj.AddComponent<TextMeshProUGUI>();
            labelTMP.text      = "Layer Name";
            labelTMP.fontSize  = 14f;
            labelTMP.color     = TextPrimary;
            labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
            if (font != null) labelTMP.font = font;

            // ── Eye icon ─────────────────────────────────────────────────────
            GameObject eyeObj = new GameObject("EyeIcon");
            eyeObj.transform.SetParent(root.transform, false);
            eyeObj.AddComponent<RectTransform>().sizeDelta = new Vector2(24f, 24f);
            LayoutElement eyeLE = eyeObj.AddComponent<LayoutElement>();
            eyeLE.preferredWidth  = 24f;
            eyeLE.preferredHeight = 24f;
            Image eyeImg = eyeObj.AddComponent<Image>();
            eyeImg.color = TextSecondary;

            // ── Visibility toggle — pinned to right edge ──────────────────────
            // Remove from HLG flow by setting ignoreLayout, then anchor manually.
            GameObject toggleObj = new GameObject("VisibilityToggle");
            toggleObj.transform.SetParent(root.transform, false);

            // Anchor to right-center of the row
            RectTransform toggleRT = toggleObj.AddComponent<RectTransform>();
            toggleRT.anchorMin        = new Vector2(1f, 0.5f);
            toggleRT.anchorMax        = new Vector2(1f, 0.5f);
            toggleRT.pivot            = new Vector2(1f, 0.5f);
            toggleRT.anchoredPosition = new Vector2(-10f, 0f);   // 10 px inset from right
            toggleRT.sizeDelta        = new Vector2(42f, 24f);

            LayoutElement toggleLE = toggleObj.AddComponent<LayoutElement>();
            toggleLE.ignoreLayout = true;   // take it out of HLG flow

            // Background image — represents ON/OFF colour
            Image toggleBg = toggleObj.AddComponent<Image>();
            toggleBg.color = ToggleOn;

            // Handle child
            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(toggleObj.transform, false);
            RectTransform handleRT = handleObj.AddComponent<RectTransform>();
            handleRT.sizeDelta        = new Vector2(20f, 20f);
            handleRT.anchorMin        = new Vector2(0f, 0.5f);
            handleRT.anchorMax        = new Vector2(0f, 0.5f);
            handleRT.anchoredPosition = new Vector2(12f, 0f);
            Image handleImg = handleObj.AddComponent<Image>();
            handleImg.color = Color.white;

            // Toggle — targetGraphic = background so ColorBlock changes the bg colour.
            Toggle toggle = toggleObj.AddComponent<Toggle>();
            toggle.targetGraphic = toggleBg;            // BG changes colour on on/off
            toggle.graphic       = handleImg;           // checkmark graphic (slides visually)
            toggle.isOn          = true;

            // ColorBlock: ON → ToggleOn tint, OFF → grey tint
            ColorBlock cb = toggle.colors;
            cb.normalColor      = ToggleOn;
            cb.pressedColor     = new Color(ToggleOn.r * 0.8f, ToggleOn.g * 0.8f, ToggleOn.b * 0.8f, 1f);
            cb.highlightedColor = new Color(ToggleOn.r * 1.1f, ToggleOn.g * 1.1f, ToggleOn.b * 1.1f, 1f);
            cb.disabledColor    = new Color(0.5f, 0.5f, 0.5f, 1f);
            cb.colorMultiplier  = 1f;
            cb.fadeDuration     = 0.1f;
            toggle.colors = cb;

            // Wire LayerToggleRowUI
            LayerToggleRowUI rowUI = root.AddComponent<LayerToggleRowUI>();
            SerializedObject so = new SerializedObject(rowUI);
            so.FindProperty("categoryLabel").objectReferenceValue    = labelTMP;
            so.FindProperty("visibilityToggle").objectReferenceValue = toggle;
            so.FindProperty("eyeIcon").objectReferenceValue          = eyeImg;
            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, path);
            GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Object.DestroyImmediate(root);
            return saved;
        }

        // ── Legend Row Sub-prefab ────────────────────────────────────────────

        private static GameObject BuildLegendRowPrefab(TMP_FontAsset font)
        {
            const string assetName = "LegendRow";
            string path = $"{PrefabDir}/{assetName}.prefab";

            GameObject root = new GameObject(assetName);
            RectTransform rootRT = root.AddComponent<RectTransform>();
            rootRT.sizeDelta = new Vector2(460f, 36f);
            Image bg = root.AddComponent<Image>();
            bg.color = BgLight;

            HorizontalLayoutGroup hlg = root.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.padding = new RectOffset(8, 8, 4, 4);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlHeight = true;
            hlg.childControlWidth  = false;
            hlg.childForceExpandWidth = false;

            // Color swatch
            GameObject swatchObj = new GameObject("ColorSwatch");
            swatchObj.transform.SetParent(root.transform, false);
            RectTransform swatchRT = swatchObj.AddComponent<RectTransform>();
            swatchRT.sizeDelta = new Vector2(20f, 28f);
            LayoutElement swatchLE = swatchObj.AddComponent<LayoutElement>();
            swatchLE.preferredWidth  = 20f;
            swatchLE.preferredHeight = 28f;
            Image swatchImg = swatchObj.AddComponent<Image>();
            swatchImg.color = Color.gray;

            // Symbol id label
            GameObject labelObj = new GameObject("LabelText");
            labelObj.transform.SetParent(root.transform, false);
            RectTransform labelRT = labelObj.AddComponent<RectTransform>();
            labelRT.sizeDelta = new Vector2(50f, 28f);
            LayoutElement labelLE = labelObj.AddComponent<LayoutElement>();
            labelLE.preferredWidth = 50f;
            TextMeshProUGUI labelTMP = labelObj.AddComponent<TextMeshProUGUI>();
            labelTMP.text      = "SC";
            labelTMP.fontSize  = 13f;
            labelTMP.fontStyle = FontStyles.Bold;
            labelTMP.color     = TextPrimary;
            labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
            if (font != null) labelTMP.font = font;

            // Full name
            GameObject nameObj = new GameObject("FullNameText");
            nameObj.transform.SetParent(root.transform, false);
            RectTransform nameRT = nameObj.AddComponent<RectTransform>();
            nameRT.sizeDelta = new Vector2(280f, 28f);
            LayoutElement nameLE = nameObj.AddComponent<LayoutElement>();
            nameLE.preferredWidth = 280f;
            nameLE.flexibleWidth  = 1f;
            TextMeshProUGUI nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
            nameTMP.text         = "Sandy Clay";
            nameTMP.fontSize     = 13f;
            nameTMP.color        = TextSecondary;
            nameTMP.alignment    = TextAlignmentOptions.MidlineLeft;
            nameTMP.overflowMode = TextOverflowModes.Ellipsis;
            if (font != null) nameTMP.font = font;

            // Count badge
            GameObject countObj = new GameObject("CountBadge");
            countObj.transform.SetParent(root.transform, false);
            RectTransform countRT = countObj.AddComponent<RectTransform>();
            countRT.sizeDelta = new Vector2(36f, 24f);
            LayoutElement countLE = countObj.AddComponent<LayoutElement>();
            countLE.preferredWidth  = 36f;
            countLE.preferredHeight = 24f;

            Image countBg = countObj.AddComponent<Image>();
            countBg.color = AccentBlue;

            // Badge label
            GameObject badgeLabelObj = new GameObject("BadgeLabel");
            badgeLabelObj.transform.SetParent(countObj.transform, false);
            RectTransform badgeLabelRT = badgeLabelObj.AddComponent<RectTransform>();
            badgeLabelRT.anchorMin        = Vector2.zero;
            badgeLabelRT.anchorMax        = Vector2.one;
            badgeLabelRT.offsetMin        = Vector2.zero;
            badgeLabelRT.offsetMax        = Vector2.zero;
            TextMeshProUGUI countTMP = badgeLabelObj.AddComponent<TextMeshProUGUI>();
            countTMP.text      = "3";
            countTMP.fontSize  = 12f;
            countTMP.fontStyle = FontStyles.Bold;
            countTMP.color     = Color.white;
            countTMP.alignment = TextAlignmentOptions.Center;
            if (font != null) countTMP.font = font;

            // Wire LegendRowUI
            LegendRowUI rowUI = root.AddComponent<LegendRowUI>();
            SerializedObject so = new SerializedObject(rowUI);
            so.FindProperty("colorSwatch").objectReferenceValue  = swatchImg;
            so.FindProperty("labelText").objectReferenceValue    = labelTMP;
            so.FindProperty("fullNameText").objectReferenceValue = nameTMP;
            so.FindProperty("countBadge").objectReferenceValue   = countTMP;
            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, path);
            GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Object.DestroyImmediate(root);
            return saved;
        }

        // ── UI Helper builders ───────────────────────────────────────────────

        private static GameObject CreatePanel(Transform parent, string name, Color color,
            Vector2 anchoredPos, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = size;
            Image img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private static void AddRoundedOutline(GameObject panel, Color color, float thickness)
        {
            // Outline via a child image that is slightly larger and behind.
            GameObject outline = new GameObject("Outline");
            outline.transform.SetParent(panel.transform, false);
            outline.transform.SetAsFirstSibling();
            RectTransform rt = outline.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-thickness, -thickness);
            rt.offsetMax = new Vector2(thickness, thickness);
            Image img = outline.AddComponent<Image>();
            img.color = color;
        }

        private static TextMeshProUGUI CreateTMPLabel(Transform parent, string name, string text,
            Vector2 anchoredPos, Vector2 size, TMP_FontAsset font,
            float fontSize, Color color, TextAnchor anchor, FontStyles style)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = size;

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.color     = color;
            tmp.fontStyle = style;
            tmp.alignment = ConvertAnchor(anchor);
            if (font != null) tmp.font = font;
            return tmp;
        }

        private static Button CreateTextButton(Transform parent, string name,
            Vector2 anchoredPos, Vector2 size, string label, TMP_FontAsset font,
            float fontSize, Color textColor)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = size;

            Image img = go.AddComponent<Image>();
            img.color = BgMid;

            Button btn = go.AddComponent<Button>();

            GameObject labelObj = new GameObject("Text");
            labelObj.transform.SetParent(go.transform, false);
            RectTransform labelRT = labelObj.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = fontSize;
            tmp.color     = textColor;
            tmp.alignment = TextAlignmentOptions.Center;
            if (font != null) tmp.font = font;

            return btn;
        }

        private static Button CreateIconButton(Transform parent, string name,
            Vector2 anchoredPos, Vector2 size, string icon, TMP_FontAsset font,
            float fontSize, Color color)
        {
            return CreateTextButton(parent, name, anchoredPos, size, icon, font, fontSize, color);
        }

        private static Button CreateTabButton(Transform parent, string name,
            Vector2 anchoredPos, Vector2 size, string label, TMP_FontAsset font,
            Color activeColor, Color inactiveColor)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = size;

            Image img = go.AddComponent<Image>();
            img.color = inactiveColor;

            Button btn = go.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor      = inactiveColor;
            cb.highlightedColor = activeColor;
            cb.pressedColor     = AccentBlue;
            btn.colors = cb;

            GameObject labelObj = new GameObject("Text");
            labelObj.transform.SetParent(go.transform, false);
            RectTransform labelRT = labelObj.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(4f, 0f);
            labelRT.offsetMax = new Vector2(-4f, 0f);
            TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = 14f;
            tmp.color     = TextPrimary;
            tmp.alignment = TextAlignmentOptions.Center;
            if (font != null) tmp.font = font;

            return btn;
        }

        private static GameObject CreateScrollView(Transform parent, string name,
            Vector2 anchoredPos, Vector2 size)
        {
            GameObject scrollObj = new GameObject(name);
            scrollObj.transform.SetParent(parent, false);
            RectTransform scrollRT = scrollObj.AddComponent<RectTransform>();
            scrollRT.anchorMin        = new Vector2(0.5f, 0.5f);
            scrollRT.anchorMax        = new Vector2(0.5f, 0.5f);
            scrollRT.pivot            = new Vector2(0.5f, 0.5f);
            scrollRT.anchoredPosition = anchoredPos;
            scrollRT.sizeDelta        = size;
            Image scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.color = new Color(0.1f, 0.1f, 0.12f, 0.5f);

            ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical   = true;

            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            RectTransform vpRT = viewport.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = Vector2.zero;
            vpRT.offsetMax = Vector2.zero;
            // RectMask2D clips by RectTransform bounds — no sprite required.
            // Mask (stencil) needs a valid sprite; without one it clips everything.
            viewport.AddComponent<RectMask2D>();

            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRT = content.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot     = new Vector2(0.5f, 1f);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = Vector2.zero;

            scrollRect.viewport = vpRT;
            scrollRect.content  = contentRT;

            return scrollObj;
        }

        private static void CreateSectionDivider(Transform parent, string name, string label,
            Vector2 anchoredPos, float width, TMP_FontAsset font)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = new Vector2(width, 24f);

            Image line = go.AddComponent<Image>();
            line.color = BgMid;

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(go.transform, false);
            RectTransform labelRT = labelObj.AddComponent<RectTransform>();
            labelRT.anchorMin        = new Vector2(0f, 0f);
            labelRT.anchorMax        = new Vector2(0f, 1f);
            labelRT.pivot            = new Vector2(0f, 0.5f);
            labelRT.anchoredPosition = new Vector2(10f, 0f);
            labelRT.sizeDelta        = new Vector2(200f, 0f);
            TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
            tmp.text      = label.ToUpper();
            tmp.fontSize  = 10f;
            tmp.color     = TextSecondary;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            if (font != null) tmp.font = font;
        }

        // ── Utilities ────────────────────────────────────────────────────────

        private static TMP_FontAsset LoadFont()
        {
            TMP_FontAsset asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (asset == null)
                Debug.LogWarning($"[AnnotationCanvasPrefabGenerator] Font not found at {FontPath}. TMP labels will use default font.");
            return asset;
        }

        private static void SavePrefab(GameObject go, string path)
        {
            bool success;
            PrefabUtility.SaveAsPrefabAsset(go, path, out success);
            if (success)
                Debug.Log($"[AnnotationCanvasPrefabGenerator] Saved prefab: {path}");
            else
                Debug.LogError($"[AnnotationCanvasPrefabGenerator] Failed to save prefab: {path}");
        }

        private static void EnsureDir(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }
        }

        private static TextAlignmentOptions ConvertAnchor(TextAnchor anchor)
        {
            return anchor switch
            {
                TextAnchor.UpperLeft    => TextAlignmentOptions.TopLeft,
                TextAnchor.UpperCenter  => TextAlignmentOptions.Top,
                TextAnchor.UpperRight   => TextAlignmentOptions.TopRight,
                TextAnchor.MiddleLeft   => TextAlignmentOptions.MidlineLeft,
                TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
                TextAnchor.MiddleRight  => TextAlignmentOptions.MidlineRight,
                TextAnchor.LowerLeft    => TextAlignmentOptions.BottomLeft,
                TextAnchor.LowerCenter  => TextAlignmentOptions.Bottom,
                TextAnchor.LowerRight   => TextAlignmentOptions.BottomRight,
                _                       => TextAlignmentOptions.Center,
            };
        }
    }
}
