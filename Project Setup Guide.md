# TerraLearnXR — Project Setup Guide

## Overview

**TerraLearnXR** is a VR collaborative field-survey application built with Unity 6000.3 (URP). Users join a multiplayer Photon room in a VR lobby, then explore geological sites rendered as Gaussian Splats (`.ply`) on a live Mapbox-powered map, with fly-cam video previews per site.

**Key packages:**

- XR Interaction Toolkit 3.3.1 (OpenXR)
- Photon PUN 2 (multiplayer)
- `wu.yize.gsplat` — Gaussian Splat renderer (custom Git package)
- Mapbox Styles API (tile fetching via HTTP — no Mapbox SDK)

---

## 1. Unity Setup

- Unity version: **6000.3**
- Render pipeline: **URP** (3D renderer)
- Input System: **New Input System** (`com.unity.inputsystem` 1.18.0)

Open the project and let all packages resolve. Two scenes exist under `Assets/Scenes/`:

```
Lobby.unity     # VR lobby — player name entry, room creation/join
Props.unity     # Main experience — world map, Gaussian Splat viewer
```

Build order in **File > Build Settings** must be: `Lobby` (index 0) → `Props` (index 1). `LobbyManager` calls `PhotonNetwork.LoadLevel("Props")` by name.

---

## 2. API Keys

All API credentials live in a single ScriptableObject: `Assets/Resources/Config/ApiConfig.asset`.

To edit it, select the asset in the Project window and fill in the Inspector:

| Field                   | Purpose                                   | Where to get it                                       |
| ----------------------- | ----------------------------------------- | ----------------------------------------------------- |
| **Mapbox Access Token** | Fetching map tiles from Mapbox Styles API | [mapbox.com](https://account.mapbox.com) — Tokens tab |

`ApiConfigProvider` (a `DontDestroyOnLoad` singleton) auto-loads this asset from `Resources/Config/ApiConfig` at runtime. It must be present as a component in the **Lobby scene** on a persistent GameObject. `MapboxConfig` reads `ApiConfigProvider.Instance.Config.MapboxAccessToken` — it does **not** store the token itself.

---

## 3. Mapbox Map Configuration

The map tile renderer is configured via `Assets/WorldMapConfigs/MapboxConfig.asset`.

| Property              | Default              | Notes                                                      |
| --------------------- | -------------------- | ---------------------------------------------------------- |
| `styleId`             | `mapbox/streets-v12` | Any Mapbox style slug, e.g. `mapbox/satellite-streets-v12` |
| `tileSize`            | `256`                | `256` or `512` (512 = sharper on high-DPI)                 |
| `defaultCenter`       | `(16.0, 106.5)`      | Lat/Lng for Vietnam center                                 |
| `defaultZoom`         | `5`                  | Initial zoom on map open                                   |
| `minZoom` / `maxZoom` | `4` / `14`           | Pan/zoom limits                                            |
| `minBounds`           | `(7.0, 100.5)`       | SW corner for pan clamping                                 |
| `maxBounds`           | `(24.5, 110.5)`      | NE corner for pan clamping                                 |
| `maxCachedTiles`      | `64`                 | In-memory tile cache size                                  |

Tile URL template (built internally): `https://api.mapbox.com/styles/v1/{styleId}/tiles/{tileSize}/{zoom}/{x}/{y}?access_token={token}`

---

## 4. Region / Hotspot Data

All geological site definitions live in `Assets/WorldMapConfigs/WorldMapData.asset` (a `SceneMapData` ScriptableObject).

Each **`MapRegion`** entry has the following fields:

| Field                  | Type              | Description                                                                                                                                         |
| ---------------------- | ----------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| `regionId`             | string            | Unique key used across the entire system (must match video filename stem and `.ply` asset name)                                                     |
| `displayName`          | string            | Human-readable label shown in UI                                                                                                                    |
| `latLng`               | Vector2           | Site centroid — `x` = latitude (°N), `y` = longitude (°E)                                                                                           |
| `plyAssetPath`         | string            | Path relative to a `Resources` folder **without extension**, e.g. `GsplatScenes/ho-tri-an`                                                          |
| `videoResourcePath`    | string            | Path relative to `StreamingAssets` **without extension**, e.g. `Videos/ho-tri-an`                                                                   |
| `models`               | `RegionModel3D[]` | Optional GLB/bytes overlay models — each entry: `modelName` (filename in `Resources/3DModels` without extension) + `position` (world-space Vector3) |
| `cameraConfig`         | struct            | `position` (XROrigin floor placement), `rotation` (yaw applied to XROrigin), `fieldOfView`                                                          |
| `regionHighlightColor` | Color             | Highlight tint for the hotspot icon                                                                                                                 |

Hotspot GameObjects in the scene are `MapHotspot` components. They read `regionId` and `hotspotIndex` from their Inspector — these must match the index/id in `WorldMapData.asset`.

---

## 5. Gaussian Splat (`.ply`) Data

### Runtime loading

`GsplatSceneLoader` uses `Resources.LoadAsync<GsplatAsset>()`, so all `.ply` files **must be inside a `Resources/` folder** and imported as `GsplatAsset` by the `wu.yize.gsplat` package importer.

### Current `.ply` files

```
Assets/Resources/GsplatScenes/
  bau-nuoc-soi.ply
  hang-doi-2.ply
  ho-tri-an.ply
  horse.ply
  mo-soklu-2.ply
  mo-soklu.ply
  robot.ply
  thac.ply
```

### Adding a new splat

1. Drop the `.ply` file into `Assets/Resources/GsplatScenes/`.
2. Unity imports it as a `GsplatAsset` automatically via the gsplat package importer.
3. In `WorldMapData.asset`, set `plyAssetPath` to `GsplatScenes/<filename-without-extension>`.

### Meta Quest SH band cap

On Android (Meta Quest), `GsplatSceneLoader.CapSHBandsForPlatform()` automatically reduces `SHBands` so the GPU buffer stays under the **128 MB Adreno limit**. This runs at runtime only — the asset on disk is not modified.

### Gsplat renderer settings

Located at `Assets/Gsplat/Settings/Resources/GsplatSettings.asset`. Adjust render quality / sorting parameters here.

---

## 6. Video (Fly-Cam Preview) Data

### Location

```
Assets/StreamingAssets/Videos/
  bau-nuoc-soi.mp4
  ho-tri-an.mp4
  mo-soklu.mp4
  nui-3-chong.mp4
  song-la-nga.mp4
  thac-mai.mp4
```

`StreamingAssets` is copied verbatim to the device — Unity's `VideoPlayer` reads them via `Application.streamingAssetsPath`.

### Naming convention

The `videoResourcePath` field in each `MapRegion` must match `Videos/<stem>` **without `.mp4`**. Example: a file `StreamingAssets/Videos/ho-tri-an.mp4` → `videoResourcePath = "Videos/ho-tri-an"`.

On Android the path is used directly; on all other platforms it is prefixed with `file://`.

### Adding a new video

1. Place the `.mp4` file in `Assets/StreamingAssets/Videos/`.
2. Set `videoResourcePath` in the corresponding `MapRegion` to `Videos/<filename-without-extension>`.

---

## 7. 3D Overlay Models

GLB models converted to `.bytes` live in:

```
Assets/Resources/3DModels/
  HD-1.bytes   HD-2.bytes
  NC-1.bytes   NC-2.bytes
  SL-1.bytes   SL-2.bytes   SL-4.bytes
  TM-1.bytes   TM-2.bytes
```

In a `MapRegion`, `models[]` lists which of these load when that region is active. `modelName` is the filename without extension (e.g. `"NC-1"`). `MapModel3DLoader` reads them via `Resources.Load`.

---

## 8. Multiplayer (Photon PUN 2)

### App ID

Edit `Assets/Photon/PhotonUnityNetworking/Resources/PhotonServerSettings.asset` via **Window > Photon Unity Networking > PUN Wizard** or directly in the Inspector:

| Setting         | Current value                          | Notes                                                                                              |
| --------------- | -------------------------------------- | -------------------------------------------------------------------------------------------------- |
| `AppIdRealtime` | `19ab34d9-0d96-468b-985f-564691c6f0ae` | Replace with your own app ID from [dashboard.photonengine.com](https://dashboard.photonengine.com) |
| `AppIdFusion`   | _(empty)_                              | Not used                                                                                           |
| `AppIdChat`     | _(empty)_                              | Not used                                                                                           |
| `AppIdVoice`    | _(empty)_                              | Not used                                                                                           |
| `FixedRegion`   | `asia`                                 | Photon cloud region — change to `eu` / `us` etc. as needed                                         |
| `Protocol`      | UDP (0)                                | Default                                                                                            |

### Room flow

```
Lobby.unity  (LobbyManager)
  └─ Player enters name → ConnectUsingSettings()
  └─ Create room / Join room (max 8 players)
  └─ MasterClient calls PhotonNetwork.LoadLevel("Props")
        ↓
Props.unity  (PlayerSpawner)
  └─ PhotonNetwork.Instantiate("VRNetworkPlayer", ...)
```

### Networked prefabs

Both prefabs must live in a `Resources/` folder (Photon requirement):

```
Assets/Resources/
  NetworkPlayer.prefab        # Non-VR fallback avatar
  VRNetworkPlayer.prefab      # VR IK avatar (used by default)
```

`PlayerSpawner.playerPrefabName` defaults to `"VRNetworkPlayer"`.

### Map sync

`NetworkedMapSync` broadcasts the selected region to all players via a Photon Room Custom Property (`"selectedRegion"`). Late joiners automatically pick up the current region in `OnJoinedRoom`. No RPCs are used for this.

### IK sync

`NetworkPlayerSync` drives Head / Left Hand / Right Hand IK targets from the local `XROrigin` each `LateUpdate`. Remote clients receive these transforms via `PhotonTransformView` on each IK target child object.

---

## 9. XR / OpenXR Setup

XR settings are in `Assets/XR/` and `Assets/XRI/Settings/`. The project uses **OpenXR** as the XR plugin.

- `Assets/XR/Loaders/` — active XR loaders
- `Assets/XR/Settings/` — OpenXR feature groups per build target
- `Assets/XRI/Settings/Resources/` — XRI input action presets

Hand controller names expected by `NetworkPlayerSync`:

- `"LeftHandController"` — child of `CameraFloorOffsetObject`
- `"RightHandController"` — child of `CameraFloorOffsetObject`

These names must match the actual controller GameObjects under the `XROrigin` rig. The VR Rig prefab is at `Assets/Prefabs/VR Rig.prefab`.

---

## 10. Scene Hierarchy Reference (Props.unity)

Key GameObjects to know:

```
ApiConfigProvider           # DontDestroyOnLoad singleton — holds ApiConfig
GaussianSplattingManager    # Has: GsplatSceneLoader, GsplatRenderer, NetworkedMapSync
WorldMapController          # Orchestrates tile map + region loading
  WorldMapCanvas            # Canvas with MapTileRenderer + MapHotspot children
FlyCamVideoPanelGO          # FlyCamVideoPanel — video preview overlay
PlayerSpawner               # Spawns VRNetworkPlayer on room join
RoomHUD                     # In-room HUD (player count, room name)
```

---

## 11. Asset Structure Quick Reference

```
Assets/
  Resources/
    Config/ApiConfig.asset          # ALL API keys
    GsplatScenes/*.ply              # Gaussian Splat assets (must be in Resources)
    3DModels/*.bytes                # GLB overlay models
    VRNetworkPlayer.prefab          # Networked VR avatar
    NetworkPlayer.prefab            # Networked fallback avatar
  StreamingAssets/
    Videos/*.mp4                    # Fly-cam preview videos
  WorldMapConfigs/
    MapboxConfig.asset              # Map tile settings
    WorldMapData.asset              # Region / hotspot definitions (SceneMapData)
  Gsplat/Settings/Resources/
    GsplatSettings.asset            # Gsplat renderer quality settings
  Photon/PhotonUnityNetworking/
    Resources/PhotonServerSettings.asset   # Photon App ID + region
  Scenes/
    Lobby.unity
    Props.unity
  Scripts/
    Config/                         # ApiConfig, ApiConfigProvider
    Core/                           # All runtime systems
    Lobby/                          # LobbyManager, VRKeyboard
```
