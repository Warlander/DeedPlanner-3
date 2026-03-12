# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DeedPlanner 3 is a 3D deed/house planning tool for Wurm Online and Wurm Unlimited, built with Unity 6000.3.8f1. It runs as a standalone Windows application and WebGL version.

## Building and Running

This is a Unity project — there are no CLI build/test commands. Development workflow:
- Open the project in **Unity 6000.3.8f1**
- Main scenes: `Assets/Scenes/LoadingScene.unity` (startup) → `Assets/Scenes/MainScene.unity`
- Use Unity Editor Play mode to run
- Automated build logic is in `Assets/Warlander/Deedplanner/Editor/BuildSystem.cs`
- Solution file `DeedPlanner-3.sln` for IDE (VSCode/Rider/Visual Studio)

**Do not run builds directly** — building must be done by the developer through the Unity Editor.

## Unity MCP Server

A **Unity MCP server** may be available when the Unity Editor is open. It provides tools to interact with the running Unity Editor directly from Claude Code (e.g. querying scene state, executing editor commands, inspecting GameObjects). Use these MCP tools when available instead of relying solely on static file analysis — they give live, accurate state of the project.

There is no automated test suite despite the Test Framework package being present.

## Architecture

### Dependency Injection
The project uses **Zenject** (Extenject) as its IoC container throughout. Installer classes in `Assets/Warlander/Deedplanner/Installers/` wire bindings.

Injection style depends on class type:
- **Plain C# classes**: constructor injection (preferred)
- **MonoBehaviours**: a dedicated method annotated with `[Inject]` (field injection with `[Inject]` is not recommended)

### Core Data Model
- **Map** (`Data/Map*.cs`) — central data structure, recently split into:
  - `MapTileGrid` — 2D grid of tiles
  - `MapLevelRenderer` — per-level rendering
  - `MapBridgesController` — bridge logic
  - `MapRoofCalculator` — roof computation
  - `MapHeightTracker` — heightmap tracking
- **Tile** (`Data/Tile.cs`) — individual grid cell containing ground, walls, floors, roof, decorations, cave data
- **Database** (`Data/Database.cs`) — static dictionaries for all game asset metadata (ground/floor/wall/roof/decoration types)

### Tab-Based Updater Pattern
Each editing mode maps to a UI tab and a corresponding `*Updater` MonoBehaviour in `Assets/Warlander/Deedplanner/Updaters/`. All updaters extend `AbstractUpdater` and activate/deactivate based on `LayoutManager.TabChanged` events.

### Camera System
`CameraCoordinator` in `Logic/Cameras/` manages four modes: Perspective (FPP), Wurmian, Isometric (ISO), and Top-Down. Each camera renders a specific level via independent camera controllers implementing `ICameraController`.

### Screen-Space Outline System
Custom screen-space selection outline split across `Graphics/Outline/` and `Logic/`:
- `ScreenSpaceOutlineFeature` — `ScriptableRendererFeature`; renders outlined objects to a mask RT, dilates, composites border over scene
- `OutlineCoordinator` — pure plain C# class tracking `Dictionary<DynamicModelBehaviour, OutlineEntry>`; no statics
- `OutlineFeatureBridge` — `IInitializable`+`IDisposable`, bound NonLazy; discovers and wires the feature on startup via reflection
- `OutlineEntry` — readonly struct grouping renderers and outline type
- Auto-setup: `Editor/OutlineFeatureSetup.cs` uses `[InitializeOnLoad]` + `EditorApplication.update`

### Command Pattern (Undo/Redo)
All map edits are implemented as `IReversibleCommand` objects managed by `CommandManager`. Never modify map state directly — always go through commands.

### Async / Reactive
- Modern `async/await` throughout (converted from coroutines — async methods use the `Async` suffix per C# convention)
- **R3** (Cysharp reactive extensions) used for streaming updates and observable texture loading
- `TextureLoader`, `WurmModelLoader`, `MaterialLoader` load Wurm assets asynchronously

### Save/Load
Map serialization uses a custom `IXmlSerializable` interface. `GameManager` orchestrates load/save; `StartupMapLoader` (plain C# class) handles initial load on startup.

### Settings & Features
- `DPSettings`, `InputSettings`, `MapRenderSettings` — global settings classes
- `FeatureStateRepository` — feature flags for experimental features

## Key Namespaces

```
Warlander.Deedplanner.Data         # Tile, Map, Database, entity types
Warlander.Deedplanner.Logic        # GameManager, CameraCoordinator, TileSelection
Warlander.Deedplanner.Gui          # LayoutManager, windows, widgets
Warlander.Deedplanner.Updaters     # Per-tab editing updaters
Warlander.Deedplanner.Graphics     # Model/texture/material loading and caching
Warlander.Deedplanner.Settings     # Application settings
Warlander.Deedplanner.Features     # Feature flag system
```

## Coding Conventions

- **Private fields**: `_camelCase` prefix
- **Methods/Properties/Classes**: PascalCase
- **Async methods**: must end with `Async` suffix
- Avoid `FindObjectOfType` or manual component wiring — use Zenject injection instead
- Prefer `[SerializeField]` for inspector-assigned component references over `GetComponent` calls
- Input handling uses the modern Unity Input System; input definitions are in `Assets/Prefabs/Input/DPInput.inputactions`
- **Class naming**: avoid generic, undescriptive suffixes — `Manager`, `Handler`, `Controller`, `Helper`, `Util`, `Service`, `Provider`, `Processor`, and similar vague nouns. These say *where* something lives but not *what it does*. Prefer names that describe the specific responsibility (e.g. `WaterFacade`, `WaterObjectContainer`, `MapHeightTracker`). Existing legacy names (`CommandManager`, `GameManager`) are grandfathered in; new classes must follow this rule.

## After Code Changes

After all code edits are complete, use the MCP `refresh_unity` + `read_console` tools to check for compilation errors and warnings. Do not consider a task finished until compilation is clean.

## Honesty About Feasibility

If a proposed approach is architecturally poor, has no clean implementation path, or would require unreasonable workarounds — say so clearly and explain why. Do not attempt to implement it anyway. Proposing a better alternative or declining with reasoning is preferable to producing bad code.

## Notable Third-Party Packages

- **Zenject/Extenject** — DI container (in `Assets/Plugins/`)
- **R3** — reactive extensions
- **Unity InputSystem** 1.18.0
- **TextMesh Pro** — UI text
- **Steamworks.NET** — Steam distribution/achievements
