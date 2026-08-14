---
name: main-scene
description: Map and editing guide for Assets/Scenes/MainScene.unity (the main editor scene of DeedPlanner 3). Use whenever reading, querying, or modifying the main scene — finding a UI element, tab, panel, camera, or manager in the hierarchy, checking how a view is wired into VContainer DI, or planning scene edits. Contains the full structural map so you don't have to scan the 25k-line YAML.
---

# MainScene Map & Editing Guide

> **MAINTENANCE: This skill mirrors `Assets/Scenes/MainScene.unity`. Whenever you change the main scene (add/remove/rename/reparent any GameObject or change view wiring), update this file in the same change. A stale map is worse than no map.**

Last verified against the scene: 2026-08-14.

## Quick Facts

- File: `Assets/Scenes/MainScene.unity` (~430 KB YAML, most content prefab-instanced).
- Startup flow: `LoadingScene.unity` (bootstrap) → `MainScene.unity` (everything else).
- Deliberately a single scene — it is small; all UI lives here. Do NOT split into additive scenes without discussing with the developer first (DI wiring assumes one scene, see below).
- Large subtrees are prefabs under `Assets/Prefabs/MainScene/`: all 13 tab contents (`Tabs/`), `Tab Toggles Panel`, `Height Chooser`, `Top Bar`, `Bridge Bar`, `Splits`. Edit those via the prefab, not the scene instance. Known instance overrides in the scene: `Top Bar/SunSlider` → `AngleSlider.source` = Directional Light (scene ref, cannot live in the prefab asset).

## Root-Level Structure

```
SceneScope            MainSceneLifetimeScope (VContainer scene scope — DI root)
Directional Light     Light, UniversalAdditionalLightData
Cameras               CameraCoordinator
  Camera 1            Camera, AudioListener, MultiCamera  (active)
  Camera 2..4         Camera, MultiCamera                 (inactive until multi-window layout)
GUI
  Main Canvas         Canvas, CanvasScaler, GraphicRaycaster, CanvasGroup, CanvasGuiScaler
    EventSystem       EventSystem, InputSystemUIInputModule
    Main Panel        HorizontalLayoutGroup — the whole app layout
      Map Panel       EditorAreaLayouterView — left/center area (map view + bars)
      Side Panel      ToggleOnKeyPress — right column (height chooser + editing tabs)
  UI Camera           Camera (UI overlay)
```

## Map Panel (center area)

Path prefix: `/GUI/Main Canvas/Main Panel/Map Panel/`

```
Top Bar                     [PREFAB: Top Bar.prefab] ToggleOnKeyPress
  SunSlider Container       Label, Sunrise, SunSlider (AngleSlider — source override → Directional Light), Sunset
  Dropdown Bar
    Debug Menu              TopDownMenu, DestroyIfNotDebug
    Visibility Menu         TopDownMenu, MapRenderSettingsUI — ground/object/tree/bush/ship/bridge toggles
Horizontal Camera Holder 1  Screen 1, Screen 2 — ResizableRenderTexture + RawImage + MouseEventCatcher per screen
Horizontal Camera Holder 2  Screen 3, Screen 4 (inactive by default)
Splits                      [PREFAB: Splits.prefab] window-split border overlays (referenced by EditorAreaLayouterView.splits)
Bridge Bar                  [PREFAB: Bridge Bar.prefab] BridgeSegmentContainer (registered as IBridgeSegmentBarView), ToggleOnKeyPress
Compass 1..4 Manager        CompassManager — one per screen; 2-4 inactive by default
```

## Side Panel (right column)

Path prefix: `/GUI/Main Canvas/Main Panel/Side Panel/`

```
Height Chooser              [PREFAB: Height Chooser.prefab]
  Heights Panel             4 camera-mode toggles (WU/ISO/3D/2D, CameraModeToggle)
                            + Floor 16..1 and Floor -1..-6 toggles (LevelToggle)
                            — every toggle is a Simple Toggle Button prefab instance
                              (Assets/Prefabs/Gui/) with per-instance level/text overrides
  Windows Panel             CameraLayoutIndicatorsCoordinator; Layouts = WindowOpenerButtonView
Editing Panel
  Tab Toggles Panel         [PREFAB: Tab Toggles Panel.prefab] TabTransitionView + TabSelectionView + VisibleTabsToggler
                            13 tab toggles: Ground, Caves, Height, Floors, Walls, Roofs,
                            Objects, Labels, Borders, Bridges, Mirror, Tools, Menu
  Content Panel             13 matching tab content roots (below), each a prefab from Tabs/
```

### Tab Content Roots (`Content Panel/`)

| Tab root | View component | Notes |
|---|---|---|
| Ground Tab | GroundUpdaterView | Toolbelt (pencil/fill/diagonal), click-mode info, Grounds Tree (UnityTree + search) |
| Caves Tab | CaveUpdaterView | Caves Tree only |
| Height Tab | HeightUpdaterView | 4 mode toggles; Handles Settings / Painting Settings panels |
| Floors Tab | FloorUpdaterView | Orientation box (N/W/E/S toggles), Floors Tree |
| Walls Tab | WallUpdaterView | Automatic Reverse / Reverse checkboxes, Walls Tree |
| Roofs Tab | RoofUpdaterView | Roofs List (UnityList) |
| Objects Tab | DecorationUpdaterView | Snap/rotation checkboxes, drag sensitivity, Objects Tree |
| Labels Tab | — (plain UIContentTab) | no view |
| Borders Tab | — (plain UIContentTab) | Scroll View list only |
| Bridges Tab | BridgeTabSwapper + BridgeCreationView + BridgeEditingView | 4 swap states: BridgeNothingSelected, BridgeOneTileSelectedSelected, BridgeTwoTilesSelected, BridgeSelected; ButtonsSection (Cancel/Place/Delete) |
| Mirror Tab | — (plain UIContentTab) | no view |
| Tools Tab | ToolsUpdaterView | Calculate Materials / Map Warnings panels |
| Menu Tab | MenuUpdaterView | Resize/Clear/Save/Load map, settings, about, donation, fullscreen, quit, version text |

Serialized active states of tab contents are meaningless residue — at startup `TabTransitionPresenter.Initialize` calls `ShowTab(TabContext.CurrentTab, animated: false)`, which activates exactly one tab and deactivates the rest. The startup tab comes from `TabContext._currentTab`, defaulting to `Tab.Ground` (enum value 0 in `Logic/Tab.cs`). `UIContentTab` carries the tab identity + `CanvasGroup` (FadeGroup) used by `TabTransitionView` for fade animations.

To EDIT a tab: open its prefab in Prefab Mode (`Assets/Prefabs/MainScene/Tabs/<Tab>.prefab`) — each tab root is active inside the prefab asset (scene instances carry `m_IsActive: 0` overrides), so it renders and edits there without touching the scene.

## DI Wiring (read this before moving things)

`MainSceneLifetimeScope` (`Assets/Warlander/Deedplanner/Scopes/MainSceneLifetimeScope.cs`) is the scene's composition root:

- Views are bound with `builder.RegisterComponentInHierarchy<TView>().As<IView>()` — **this searches only the scope's own scene**, which is the main technical reason the app stays one scene.
- A build callback runs `container.InjectGameObject` over **every MonoBehaviour in the scene** (legacy pattern, marked TODO). Any MonoBehaviour with `[Inject]` fields anywhere in the hierarchy gets them filled.
- Serialized refs on the scope itself: `_cameraCoordinator` (Cameras root), `_bridgeTabSwapper` (Bridges Tab), `_debugProperties`.
- All 13 `IUpdater` implementations are plain C# classes registered in the container (not scene objects); `UpdaterCoordinator` ticks the active one.
- Presenters are `RegisterEntryPoint<...Presenter>()` — pure C#, receive views via constructor injection.

Consequences when editing:

- Renaming/moving a view GameObject is safe for DI (lookup is by component type), but check `[SerializeField]` references to it from other components first.
- Deleting a view component breaks container build at startup — remove the matching registration in `MainSceneLifetimeScope` too.
- Adding a new tab = new toggle under `Tab Toggles Panel` (with `TabReference`) + new content root under `Content Panel` + view registration + updater registration. Mirror an existing tab end-to-end.

## Querying & Editing the Scene

Never hand-edit the `.unity` YAML while an Editor is running. Drive the live Editor via the Unity CLI (see the `unity-cli` skill):

```bash
unity status --json                                   # Editor connected? state "ready"?
unity command get_scene_hierarchy --json              # full tree with components + instanceIds
unity command find_gameobjects --query "SunSlider"    # locate by name
unity command get_component_properties ...            # inspect one component
unity command set_active / set_transform / ...        # mutate live scene
unity command save_scene                              # persist to disk
```

If no Editor is connected, open one: `unity open "E:/Unity/DeedPlanner-3" --args "-automated"`. Only hand-edit YAML as a last resort with no Editor available — fileIDs/GUIDs are easy to corrupt.

Prefab instances hold most of the scene's content — the `Tabs/` contents, `Tab Toggles Panel`, `Height Chooser`, `Top Bar`, `Bridge Bar`, `Splits`, plus repeated widgets like `BridgeSegmentItem` and `Simple Toggle Button`. To change those, edit the prefab asset, not the scene instance (use `apply_prefab_overrides` / `revert_prefab_overrides` CLI commands for instance-level changes).
