# VR Geological Field Survey — Thesis Project

A collaborative multi-user VR application built with **Unity 6** for geological fieldwork training and annotation. Users explore real-world terrain reconstructed from 3D Gaussian Splatting scans, draw freehand annotations on a virtual drawing board, place geological symbols, and collaborate in real-time via Photon PUN2.

---

## Overview

This project addresses the challenge of remote geological education and collaborative field survey. Instead of physically visiting a site, multiple users can enter a shared VR room, navigate to a photorealistic 3D Gaussian Splat reconstruction of a real geological site, and collaboratively annotate observations on a virtual drawing board — then export those annotations as PNG images.

---

## Key Features

### Multi-user Lobby & Networking

- Photon PUN2-powered multiplayer with room creation, browsing, and quick-join
- VR keyboard for text input directly in headset
- Up to 8 players per room
- Networked scene synchronization: all players load the same geological site simultaneously (`NetworkedMapSync`)
- Networked avatar IK: head and both hand positions synced per-frame via `NetworkPlayerSync` and Animation Rigging

### 3D Gaussian Splatting Scene Viewer

- Loads `.ply` Gaussian Splat assets at runtime via `GsplatSceneLoader`
- Streaming asset loading with `Resources.LoadAsync` and LRU-based memory management
- Automatic SH band capping for Meta Quest's 128 MB GPU buffer limit
- Per-region camera spawn configuration (position + yaw)

### World Map Navigation

- Interactive Mapbox tile map rendered inside VR via `MapTileRenderer`
- LRU tile cache with pinned-tile eviction guard (`MapTileFetcher`, max 4 concurrent HTTP requests)
- Clickable region hotspots (`MapHotspot`, `MapHotspotNavigator`) that teleport all players to the selected geological site
- Tooltip and 3D model preview per region (`MapRegionTooltip`, `MapModel3DLoader`)

### VR Drawing System

- Freehand ink drawing on a physical drawing surface using `PenTool` and `DrawingSurface`
- Eraser tool (`EraserTool`) and UI ray-based drawing mode (`UIRayDrawingTool`)
- Multiple drawing modes managed by `DrawingModeManager`
- Photo attachment workflow: capture field photos via in-game camera item, attach to drawing board

### Geological Symbol Annotation

- Symbol database (`GeologicalSymbolDatabase` ScriptableObject) with categorized geological symbols
- Place symbols at UV coordinates on the drawing board via `GeologicalAnnotationManager`
- Per-symbol and per-category layer visibility toggling
- Rendered as overlay on top of ink strokes via `SymbolOverlayRenderer`

### Board Export

- `BoardExporter` renders the drawing board (background photo + ink strokes + symbol overlays) to a 2048x2048 PNG
- Saved to `/Assets/CapturedPhotos/` with timestamp filename
- Optional copy to a desktop photos folder
- Export triggers photo gallery refresh automatically

### VR Item System

- Item bar UI for switching between tools (pen, eraser, camera, map, etc.)
- `ItemSpawner` spawns and returns VR interactable items
- Input System integration for controller button bindings

---

## Technology Stack

| Category              | Package / Version                        |
|-----------------------|------------------------------------------|
| Engine                | Unity 6000.3                             |
| Render Pipeline       | Universal Render Pipeline (URP) 17.3.0   |
| XR Framework          | XR Interaction Toolkit 3.3.1 + OpenXR 1.16.1 |
| Networking            | Photon PUN2 (PhotonUnityNetworking)      |
| 3D Gaussian Splatting | wu.yize.gsplat (GitHub)                  |
| Map Tiles             | Mapbox REST API                          |
| Input                 | Unity Input System 1.18.0               |
| Animation IK          | Animation Rigging 1.4.1                  |
| AI Navigation         | Unity AI Navigation 2.0.10              |

---

## Project Structure

```
Assets/
├── Scenes/
│   ├── Lobby.unity          # Entry point — player name + room selection
│   └── Props.unity          # Main experience — map, GSplat viewer, drawing board
├── Scripts/
│   ├── Core/                # Map, networking, scene loading, IK sync, item system
│   ├── Drawing/             # Drawing surface, tools, geological annotations, export
│   ├── Lobby/               # LobbyManager, VR keyboard
│   ├── Items/               # Camera item, photo attachment
│   └── Config/              # API config ScriptableObjects
├── Resources/
│   ├── GsplatScenes/        # Runtime-loaded .ply Gaussian Splat assets
│   ├── Geology/             # GeologicalSymbolDatabase + symbol sprites
│   ├── Config/              # ApiConfig (Mapbox token, etc.)
│   └── NetworkPlayer.prefab # Photon network avatar prefab
├── Prefabs/
│   ├── VR rig/              # XR Origin setup
│   ├── Weapons/ Equipments/ # Interactable item prefabs
│   ├── Maps/                # World map UI prefabs
│   └── Drawing/             # Drawing board prefab
└── Photon/                  # Photon PUN2 SDK
```

---

## Setup Requirements

1. **Photon App ID** — Create a PUN2 app at https://www.photon-engine.com and paste the App ID into `PhotonServerSettings` (`Assets/Photon/PhotonUnityNetworking/Resources/PhotonServerSettings.asset`).
2. **Mapbox Token** — Add your public token to `Assets/Resources/Config/ApiConfig.asset`.
3. **Gaussian Splat assets** — Place converted `.ply` assets under `Assets/Resources/GsplatScenes/` and configure paths in `SceneMapData`.
4. **XR Plugin** — Enable OpenXR in `Project Settings > XR Plug-in Management` for the target platform (PC / Android for Meta Quest).
5. **Target platform for Quest** — Switch platform to Android; the `GsplatSceneLoader` will automatically cap SH bands to respect the 128 MB GPU buffer limit.

---

## Scene Flow

```
Lobby.unity
└── Player enters name (VR keyboard)
└── Connects to Photon master server
└── Creates or joins a room (up to 8 players)
└── Master client loads Props.unity (AutomaticallySyncScene)
        └── World Map opens (Mapbox tiles)
        └── Player clicks a geological site hotspot
        └── NetworkedMapSync broadcasts region ID to all players
        └── GsplatSceneLoader loads .ply asset + teleports all players
        └── Drawing board available for annotation
            ├── Pen / Eraser tools
            ├── Photo capture + board attachment
            ├── Geological symbol placement
            └── Export board as PNG
```

---

## Architecture Notes

- **Singleton managers** — `GeologicalAnnotationManager`, `DrawingSystemManager`, `BoardExporter`, `VRItemSystemManager`, `ItemBarController`: one instance per scene, self-destruct on duplicate.
- **Event-driven decoupling** — systems communicate via C# `Action` events rather than direct references (e.g., `OnSymbolPlaced`, `OnExportCompleted`, `OnSceneLoadCompleted`, `OnPhotosUpdated`).
- **Network authority** — `NetworkedMapSync` routes all region-load requests through the Photon master client to guarantee consistent state across all players; non-master clients receive the region ID as an RPC.
- **Memory management** — `MapTileFetcher` uses an LRU linked-list cache with pinned-tile protection; `GsplatSceneLoader` explicitly calls `Resources.UnloadUnusedAssets()` and `GC.Collect()` between scene loads to prevent OOM on Meta Quest.
- **XR compatibility** — Camera teleportation sets position/yaw on the `XROrigin` transform rather than the camera directly, preserving headset tracking offset and preventing floor clipping.
