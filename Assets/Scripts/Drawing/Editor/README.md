# VR Drawing System - Editor Automation Tools

## Overview

Complete Unity Editor automation for zero-manual setup of the VR Drawing System. All prefabs, materials, layers, and scene objects are created and configured automatically.

---

## 🚀 QUICK START

### Option 1: One-Click Complete Setup (Recommended)

1. **Menu**: `Tools > VR Drawing > Auto Setup`
2. Click **⚡ RUN COMPLETE SETUP ⚡**
3. Wait for completion (check Console for progress)
4. Done! All prefabs, materials, and scene objects created

### Option 2: Step-by-Step Setup

1. **Menu**: `Tools > VR Drawing > Auto Setup`
2. Click buttons in order:
   - 1. Setup Layers
   - 2. Create Materials
   - 3. Create Drawing Board Prefab
   - 4. Create Tool Panel Prefab
   - 5. Create Drawing Board Activator Prefab
   - 6. Create Pen Prefab
   - 7. Setup Scene (DrawingModeManager)

### Option 3: Quick Menu

Use `Tools > VR Drawing > Quick Setup` for fast access:
- **1. Run Complete Auto Setup** - Opens setup window
- **2. Validate Setup** - Validates all components
- **3. Setup Drawing Mode Manager** - Creates manager in scene

---

## 📁 CREATED ASSETS

### Prefabs (Assets/Prefabs/Drawing/)
- **DrawingBoard.prefab** - Main drawing board with DrawingSurface
- **ToolPanel.prefab** - UI panel for tool selection
- **DrawingBoardActivator.prefab** - Trigger object for Drawing Mode
- **Pen.prefab** - Physical pen with XRGrabInteractable

### Materials (Assets/Materials/Drawing/)
- **DrawingCanvas.mat** - White unlit material for drawing surface
- **PenMaterial.mat** - Blue lit material for pen object

### Layers
- **Drawing Surface** (Layer 3) - For drawing collision detection

---

## 🛠️ EDITOR TOOLS

### 1. VRDrawingAutoSetup (Main Setup Window)

**Menu**: `Tools > VR Drawing > Auto Setup`

**Features:**
- XR Toolkit validation
- Layer creation
- Material generation
- Prefab creation with full configuration
- Scene setup with DrawingModeManager
- Progress logging
- Idempotent (safe to run multiple times)

**What It Does:**
1. **Setup Layers**: Creates "Drawing Surface" layer at index 3
2. **Create Materials**: Generates DrawingCanvas.mat and PenMaterial.mat
3. **Create Drawing Board Prefab**:
   - Root GameObject with DrawingBoardActivator
   - Canvas Quad with MeshRenderer
   - DrawingSurface child with BoxCollider (trigger)
   - MeshStrokeRenderer component
   - Auto-assigns Drawing Surface layer
4. **Create Tool Panel Prefab**:
   - World Space Canvas
   - Pen, Eraser, Undo, Clear buttons
   - 8 color picker buttons
   - Thickness slider
   - DrawingToolPanel component
   - All UI references auto-wired
5. **Create Drawing Board Activator Prefab**:
   - Simple GameObject with DrawingBoardActivator
   - Auto-activate on spawn enabled
6. **Create Pen Prefab**:
   - Cylinder mesh with PenTool component
   - ToolTip child with SphereCollider (trigger)
   - XRGrabInteractable
   - Rigidbody configured
   - ToolTipCollisionDetector
   - Material applied
7. **Setup Scene**:
   - Creates DrawingModeManager GameObject
   - Auto-assigns all references:
     - Drawing Board Prefab
     - Tool Panel Prefab
     - Locomotion providers
     - XR Origin
     - UI Ray Interactor
     - Player Camera

---

### 2. VRDrawingValidator (Validation Tool)

**Menu**: `Tools > VR Drawing > Validate Setup`

**Features:**
- Validates all prefabs exist
- Checks materials
- Verifies scene components
- Validates layers
- Checks XR components
- Quick-select missing items
- Detailed validation logging

**Use Cases:**
- After auto setup to verify success
- Before building to ensure completeness
- Troubleshooting missing references
- Documentation of current setup state

---

### 3. DrawingModeManagerEditor (Custom Inspector)

**Automatic** when DrawingModeManager is selected

**Features:**
- **🔧 Auto-Assign All References** button
- **Validate Setup** - Checks all references
- **Clear All** - Removes all references
- Organized foldout sections
- Auto-find buttons for each category
- Runtime status display
- Input setup helpers

**Auto-Find Features:**
- Auto-Find Locomotion Components
- Find UI Ray Interactor
- Assign Main Camera
- Auto-Setup Y Button Input (helper)

**Sections:**
1. Drawing Board Settings
2. Tool Panel Settings
3. Input Settings (Y Button)
4. Locomotion Settings
5. UI Ray Settings
6. Other References
7. Runtime Status (Play Mode only)

---

### 4. ItemSpawnerAutoWire (Custom Inspector)

**Automatic** when ItemSpawner is selected

**Features:**
- **Auto-Wire Drawing Prefabs** button
- **Validate References** button
- Auto-assigns:
  - Drawing Board Activator Prefab
  - Pen Prefab
  - Spawn Point (Main Camera if not set)
  - Item Bar Controller

**Usage:**
1. Select ItemSpawner GameObject in scene
2. Click "Auto-Wire Drawing Prefabs"
3. Click "Validate References" to verify

---

### 5. DrawingPrefabPostProcessor (Automatic)

**Runs automatically** when prefabs are imported/modified

**Features:**
- Validates DrawingBoard prefab structure
- Ensures Drawing Surface layer assigned
- Verifies trigger colliders
- Auto-adds MeshStrokeRenderer if missing
- Validates Pen prefab references
- Wires ToolTipCollisionDetector
- Validates Tool Panel UI references

**Benefits:**
- Prevents configuration errors
- Maintains consistency
- Auto-fixes common issues
- Silent unless errors occur

---

### 6. VRDrawingQuickMenu (Quick Access)

**Menu**: `Tools > VR Drawing`

**Quick Setup:**
- 1. Run Complete Auto Setup
- 2. Validate Setup
- 3. Setup Drawing Mode Manager

**Scene Objects:**
- Select Drawing Mode Manager
- Select Drawing System Manager
- Select Item Spawner

**Prefabs:**
- Open Drawing Board Prefab
- Open Tool Panel Prefab
- Open Pen Prefab
- Open Activator Prefab

**Documentation:**
- Open Setup Guide
- Show Console Log

**Utilities:**
- Clear All Console Logs
- Force Recompile Scripts

---

## 🔧 TECHNICAL DETAILS

### Layer Configuration
```csharp
Layer Name: "Drawing Surface"
Layer Index: 3
Usage: Trigger collision for DrawingSurface
```

### Prefab Structure

**DrawingBoard.prefab:**
```
DrawingBoard (root)
├── DrawingBoardActivator component
├── Canvas (Quad mesh)
│   └── MeshRenderer (DrawingCanvas material)
└── DrawingSurface (child)
    ├── DrawingSurface component
    ├── MeshStrokeRenderer component
    ├── BoxCollider (trigger, size: 0.95x0.95x0.02)
    └── Layer: Drawing Surface
```

**ToolPanel.prefab:**
```
ToolPanel (Canvas - World Space)
├── DrawingToolPanel component
├── GraphicRaycaster
├── CanvasScaler
├── Background (Panel)
├── PenButton
├── EraserButton
├── UndoButton
├── ClearButton
├── ColorPanel (GridLayoutGroup)
│   ├── Color0 through Color7 buttons
└── ThicknessSlider
    ├── Background
    ├── Fill Area
    └── Handle
```

**Pen.prefab:**
```
Pen (root)
├── PenTool component
├── XRGrabInteractable component
├── Rigidbody (no gravity)
├── CapsuleCollider
├── MeshRenderer (PenMaterial)
└── ToolTip (child)
    ├── ToolTipCollisionDetector component
    └── SphereCollider (trigger, radius: 0.5)
```

### Reference Auto-Assignment Logic

**DrawingModeManager:**
1. Prefabs loaded from Assets/Prefabs/Drawing/
2. FindFirstObjectByType for locomotion providers
3. GameObject.Find for "UI Ray Interactor"
4. Camera.main for player camera
5. All assigned via SerializedObject

**Validation:**
- Null checks before assignment
- Logging for each found/missing component
- EditorUtility.SetDirty for scene changes
- AssetDatabase.Refresh after asset creation

---

## 🎯 USAGE EXAMPLES

### Example 1: Fresh Project Setup
```
1. Import VR Drawing System scripts
2. Tools > VR Drawing > Auto Setup
3. Click "RUN COMPLETE SETUP"
4. Tools > VR Drawing > Validate Setup
5. Done! Ready to use
```

### Example 2: Add to Existing Scene
```
1. Tools > VR Drawing > Quick Setup > 3. Setup Drawing Mode Manager
2. Select ItemSpawner in scene
3. Click "Auto-Wire Drawing Prefabs"
4. Done!
```

### Example 3: Fix Missing References
```
1. Select DrawingModeManager in scene
2. Click "🔧 Auto-Assign All References"
3. Click "Validate Setup"
4. Check Console for any warnings
```

### Example 4: Regenerate Prefabs
```
1. Delete old prefabs in Assets/Prefabs/Drawing/
2. Tools > VR Drawing > Auto Setup
3. Run steps 3-6 individually
4. Tools > VR Drawing > Validate Setup
```

---

## ⚠️ TROUBLESHOOTING

### "XR Interaction Toolkit not found"
- Install com.unity.xr.interaction.toolkit version 3.0+
- Window > Package Manager > Add by name
- Restart Unity Editor

### "Drawing Surface layer already exists at wrong index"
- Manually rename or remove conflicting layer
- Re-run layer setup
- Or accept different index (update code constants)

### "Prefab creation failed"
- Check directory permissions
- Ensure Assets/Prefabs/Drawing exists
- Check Console for specific errors
- Try individual steps instead of complete setup

### "References not assigned after auto-assign"
- Ensure required components exist in scene:
  - XR Origin
  - TeleportationProvider
  - ContinuousMoveProvider
  - ContinuousTurnProvider
  - UI Ray Interactor
- Check Console for "not found" messages
- Manually assign missing references

### "Tool Panel UI not working"
- Open Tool Panel prefab
- Check DrawingToolPanel component
- Verify all button references assigned
- Use DrawingPrefabPostProcessor to auto-fix
- Or delete and regenerate prefab

### "Pen not drawing"
- Check Pen prefab ToolTip collider is trigger
- Verify Drawing Surface layer assigned
- Ensure DrawingSystemManager in scene
- Check PenTool component references

---

## 🔍 VALIDATION CHECKLIST

Run `Tools > VR Drawing > Validate Setup` and verify:

**Prefabs:**
- ✓ Drawing Board
- ✓ Tool Panel
- ✓ Drawing Board Activator
- ✓ Pen

**Materials:**
- ✓ Drawing Canvas
- ✓ Pen Material

**Scene Setup:**
- ✓ DrawingModeManager
- ✓ TeleportationProvider
- ✓ ContinuousMoveProvider
- ✓ ContinuousTurnProvider
- ✓ XR Origin

**Layers:**
- ✓ Drawing Surface (Layer 3)

**Drawing System:**
- ✓ DrawingSystemManager (optional but recommended)

---

## 📝 NOTES

### Idempotency
- All tools are safe to run multiple times
- Existing assets are not overwritten
- References are re-assigned if already exist
- Layers are not recreated if exist

### Performance
- Asset creation takes 1-3 seconds
- Scene setup is instant
- No runtime performance impact

### Compatibility
- Unity 6000.2+ (Unity 6)
- XR Interaction Toolkit 3.0+
- URP 17.0+
- Input System 1.0+

### Limitations
- Y Button input must be configured manually in Input Actions
- Cannot auto-create Input Actions asset
- Cannot modify locked/read-only prefabs
- Requires scene to be saved after setup

---

## 🤝 BEST PRACTICES

1. **Run validation after setup** to catch issues early
2. **Save scene** after auto-setup completes
3. **Backup project** before first run (optional)
4. **Check Console logs** for detailed progress
5. **Use Quick Menu** for common tasks
6. **Validate before building** to ensure completeness

---

## 📚 ADDITIONAL RESOURCES

- **Setup Guide**: Tools > VR Drawing > Documentation > Open Setup Guide
- **Console Logs**: Tools > VR Drawing > Documentation > Show Console Log
- **Unity XR Docs**: https://docs.unity3d.com/Manual/XR.html
- **XR Interaction Toolkit**: https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@latest

---

**Version**: 1.0  
**Created**: VR Drawing System Auto Setup  
**Author**: Automated Editor Tools  
**License**: Use freely within your project
