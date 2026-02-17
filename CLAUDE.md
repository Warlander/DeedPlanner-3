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
`CameraCoordinator` in `Logic/Cameras/` manages three modes: First-Person (FPP), Isometric (ISO), Top-Down. Each camera renders a specific level via independent camera controllers implementing `ICameraController`.

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

## Notable Third-Party Packages

- **Zenject/Extenject** — DI container (in `Assets/Plugins/`)
- **R3** — reactive extensions
- **Unity InputSystem** 1.18.0
- **TextMesh Pro** — UI text
- **UnityFX Outline** 0.8.5 — selection outline effects
- **Steamworks.NET** — Steam distribution/achievements
