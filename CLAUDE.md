# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository. Mirrored to `AGENTS.md` (OpenAI Codex) — when updating one, update the other.

## Project Overview

DeedPlanner 3 is a 3D deed/house planning tool for Wurm Online and Wurm Unlimited, built with Unity 6000.3.20f1. It runs as a standalone application (Windows/Linux/Mac) and WebGL version.

## Building and Running

Development workflow:
- Open the project in **Unity 6000.3.20f1** (see `ProjectSettings/ProjectVersion.txt` for the exact version)
- Main scenes: `Assets/Scenes/LoadingScene.unity` (startup) → `Assets/Scenes/MainScene.unity`
- Use Unity Editor Play mode to run
- Automated build logic is in `Assets/Warlander/Deedplanner/Editor/BuildSystem.cs`
- Solution file `DeedPlanner-3.sln` for IDE (VSCode/Rider/Visual Studio)

**Never run builds** (`unity build` or any other build path) unless the user explicitly asks for a build in their prompt — releases are handled by the developer.

## Unity CLI (preferred automation path)

The **Unity CLI** (`unity`, v1.0.0-beta.3+) is the primary way to interact with Unity from the terminal. Use the `unity-cli` skill for guidance. Always prefer CLI over MCP.

**Availability:** the CLI is expected on every dev machine but is a separate install. Check it exists (`unity --version`) before relying on it. If missing, ask the developer to install it and fall back to manual/Editor-side verification — NEVER install it automatically or silently.

Useful commands:

- `unity status` — is an Editor connected? Which project/PID/state?
- `unity list` — commands the connected Editor exposes (Pipeline package)
- `unity command <cmd> [args]` — execute a command on the connected Editor (domain reload, play mode, scene queries, etc.)
- `unity test --mode EditMode` — run tests, NUnit XML to test-results.xml
- `unity open` — open the project with the correct Editor version
- `unity build` — full batch-mode build (see the build restriction above)

Add `--json` for machine-readable output in agentic loops.

A **Unity MCP server** may also be available. The ONLY acceptable option is the official one: the project includes `com.unity.pipeline`, which powers both the CLI commands and the official MCP server (`unity mcp configure`). Do NOT add, configure, or use any other MCP server (e.g. the community "MCP for Unity") — alternatives require installing extra packages into the project itself.

**Opening the project:** if `unity status` shows no connected Editor, use `unity open` to launch the project — after startup, both CLI commands and MCP become usable. For automation workflows, launch with the `-automated` flag:

```
unity open "E:/Unity/DeedPlanner-3" --args "-automated"
```

Without it, play mode ENTRY stalls indefinitely while the Editor window is unfocused (in-play behavior is unaffected).

**CLI call latency / polling pitfall:** `unity open` stays attached for the Editor's lifetime — always run it as a background task and never wait on its completion. Every CLI invocation on this machine also pays a multi-second telemetry fetch timeout (no external network), so tight `until unity status | grep -q ready` polling loops effectively hang: each iteration is slow and the Editor typically becomes ready long before the loop notices. Instead, launch, do other work, then check `unity status` once when needed — treat "hangs on status polling" as "Editor is probably already up, just check it".

**Code evaluation:** the Pipeline package ships a CodeEval command (edit-mode AND runtime — `unity command` / `unity list` can also attach to a running Player via `--runtime`). This lets the CLI execute arbitrary C# in the live Editor or in the running app — usable both for manipulating the Editor (scenes, assets, play mode) and for testing the app's behavior from the outside. Discover exact command names with `unity list`.

**eval_file quirks (verified):** `eval` rejects bare expressions; use `eval_file` with an explicit `return`. `eval_file` wraps the file in an `Execute()` method body: statements only (no `using` directives, no class/method definitions), fully-qualified names (`UnityEngine.Object`, `System.IO.Path` — `Object` collides with `object`). Write eval scripts to system temp, never under `Assets/` (stray .cs files break compilation).

## Agentic Verification

No automated test suite exists (despite the Test Framework package being present), but the project iterates fast — domain reload and play-mode enter/exit each take only a few seconds. After triggering a recompile or play-mode change, allow a ~5 second buffer, then poll `unity status` for the Editor state before issuing the next command.

Suggested verification ladder, cheapest first:
1. Compile check (connected Editor via `unity command`, or `unity test --mode EditMode` batch) — catches syntax/type errors.
2. EditMode smoke run if tests are ever added (`unity test --mode EditMode`).
3. Play mode enter/exit on the connected Editor to catch startup/initialization exceptions (VContainer wiring, scene load). While in play mode, use the Pipeline CodeEval command to execute C# against the running app — assert state, drive interactions, and read back results, giving real behavioral testing without a test assembly.
4. Manual/QA pass by the developer for visual or gameplay behavior — agents cannot judge rendering correctness.

When adding testable plain-C# logic (presenters, data model, commands), prefer code that *could* be covered by EditMode tests later — keep it free of UnityEngine.Object dependencies where practical.

## Architecture

### Dependency Injection & MVP Pattern
The project uses **VContainer** as its IoC container. Scope classes in `Assets/Warlander/Deedplanner/Scopes/` (VContainer `LifetimeScope` subclasses) wire bindings.

The UI follows a strict **MVP (Model-View-Presenter)** pattern:
- **View** (MonoBehaviour): thin view only — handles its own internal visual state at most; all logic belongs in the presenter; no container access allowed
- **Presenter** (plain C# class): mediates between view and model; container use is discouraged — prefer pure DI (constructor injection) except in exceptional situations
- Each presenter handles exactly one view. When multiple instances of a view type can exist simultaneously, each instance gets its own presenter. When such presenters need to share model state, the approach is decided case by case.

Injection style:
- **Plain C# classes (presenters, etc.)**: constructor injection (preferred)
- **MonoBehaviours (views)**: no container injection — the presenter receives the view, not the other way around; views must not reference their presenter

### Core Data Model
- **Map** (`Data/Map*.cs`) — central data structure, recently split into:
  - `MapTileGrid` — 2D grid of tiles
  - `MapLevelRenderer` — per-level rendering
  - `MapBridgesController` — bridge logic
  - `MapRoofCalculator` — roof computation
  - `MapHeightTracker` — heightmap tracking
- **Tile** (`Data/Tile.cs`) — individual grid cell containing ground, walls, floors, roof, decorations, cave data
- **Database** (`Data/Database.cs`) — static dictionaries for all game asset metadata (ground/floor/wall/roof/decoration types)
- **Materials** (`Data/Materials.cs`) — material costs are unit counts, not weights; weight-based goods use template-weight units (Mortar unit = 2 kg, Tar unit = 1 kg), matching the game's build-list convention

### Tab-Based Updater Pattern
Each editing mode maps to a UI tab and a corresponding `*Updater` class in `Assets/Warlander/Deedplanner/Updaters/`. Updaters are plain C# classes implementing `IUpdater` (`TargetTab`, `Initialize`, `Enable`, `Disable`, `Tick`), constructor-injected, and registered in the VContainer scope. `UpdaterCoordinator` (`Logic/UpdaterCoordinator.cs`) receives them as `IReadOnlyList<IUpdater>`, initializes all, and on `TabContext.TabChanged` disables the active updater, enables the one whose `TargetTab` matches, and ticks only the active one. Views follow MVP: each updater holds an `I*UpdaterView` interface (implementations in `Gui/Updaters/`).

### Camera System
`CameraCoordinator` in `Logic/Cameras/` manages four modes: Perspective (FPP), Wurmian, Isometric (ISO), and Top. Each camera renders a specific level via independent camera controllers implementing `ICameraController`.

### Screen-Space Outline System
Custom screen-space selection outline split across `Graphics/Outline/` and `Logic/Outlines/`:
- `ScreenSpaceOutlineFeature` (`Graphics/Outline/`) — `ScriptableRendererFeature`; renders outlined objects to a mask RT, dilates, composites border over scene
- `OutlineCoordinator` (`Logic/Outlines/`) — pure plain C# class tracking `Dictionary<DynamicModelBehaviour, OutlineEntry>`; no statics
- `OutlineFeatureBridge` (`Logic/Outlines/`) — `IInitializable`+`IDisposable`, bound NonLazy; discovers and wires the feature on startup via reflection
- `OutlineEntry` (`Graphics/Outline/`) — readonly struct grouping renderers and outline type
- Auto-setup: `Editor/OutlineFeatureSetup.cs` uses `[InitializeOnLoad]` + `EditorApplication.update`

### Command Pattern (Undo/Redo)
All map edits are implemented as `IReversibleCommand` objects managed by `CommandManager`. Never modify map state directly — always go through commands.

### Async / Reactive
- Modern `async/await` throughout (converted from coroutines — async methods use the `Async` suffix per C# convention)
- **R3** (Cysharp reactive extensions) used for streaming updates and observable texture loading
- `TextureLoader`, `WurmModelLoader`, `MaterialLoader` load Wurm assets asynchronously

### Save/Load
Map serialization uses a custom `IXmlSerializable` interface. `MapHandler` orchestrates load/save (backed by `MapLoader` and `MapFactory`); `StartupMapLoader` (plain C# class) handles initial load on startup.

### Settings & Features
- `DPSettings`, `InputSettings`, `MapRenderSettings` — global settings classes
- `DPFeatureStateRepository` — feature flags for experimental features

## Key Namespaces

```
Warlander.Deedplanner.Data         # Tile, Map, Database, entity types
Warlander.Deedplanner.Logic        # MapHandler, CameraCoordinator, TileSelection
Warlander.Deedplanner.Gui          # Windows, widgets, tab layout
Warlander.Deedplanner.Updaters     # Per-tab editing updaters
Warlander.Deedplanner.Graphics     # Model/texture/material loading and caching
Warlander.Deedplanner.Settings     # Application settings
Warlander.Deedplanner.Features     # Feature flag system
```

## Coding Conventions

- **Private fields**: `_camelCase` prefix
- **Methods/Properties/Classes**: PascalCase
- **Async methods**: must end with `Async` suffix
- Avoid `FindObjectOfType` or manual component wiring — use VContainer injection instead
- Prefer `[SerializeField]` for inspector-assigned component references over `GetComponent` calls
- Input handling uses the modern Unity Input System; input definitions are in `Assets/Prefabs/Input/DPInput.inputactions`
- **Class naming**: avoid generic, undescriptive suffixes — `Manager`, `Handler`, `Controller`, `Helper`, `Util`, `Service`, `Provider`, `Processor`, and similar vague nouns. These say *where* something lives but not *what it does*. Prefer names that describe the specific responsibility (e.g. `WaterFacade`, `WaterObjectContainer`, `MapHeightTracker`). Existing legacy names (`CommandManager`) are grandfathered in; new classes must follow this rule. The `View` suffix is the one intentional exception — MonoBehaviour view classes in the MVP pattern must end with `View` (e.g. `GroundPainterView`, `TileSelectionView`) to distinguish them from their presenter counterparts.
- **Property formatting**: auto-properties and single-expression `get`-only properties stay on one line. Anything more complex splits `get`/`set` onto their own lines. If `get` or `set` contains more than one statement, use expanded block syntax (`{ ... }`) rather than expression-body (`=>`) shorthand.
- **No tuples**: do not use tuples — neither implicit (`(int x, string y)`) nor explicit (`Tuple<int, string>`). Define a named `struct` or value class instead. Named types are self-documenting, refactorable, and avoid accidental structural coupling.
- **UI arrangement**: before building UI, check whether a prefab already exists for similar UI and reuse it. When the things being wired live inside a parent prefab, wire them up inside that prefab rather than from the scene or whatever context you are currently in.

## After Code Changes

Verify compilation after every code edit before considering the task done:

1. If an Editor is connected (`unity status`): trigger a refresh/compile via `unity command` (discover exact command names with `unity list`) and read the console/compile status back the same way.
2. If no Editor is connected: `unity test --mode EditMode` forces a full compile and surfaces errors, or check `Editor.log` after a domain reload.
3. If the Unity CLI itself is missing: ask the developer to install it (never install it yourself) and ask them to confirm compilation in the Editor.
4. Do not consider a task finished until compilation is clean (errors AND new warnings).

## Honesty About Feasibility

If a proposed approach is architecturally poor, has no clean implementation path, or would require unreasonable workarounds — say so clearly and explain why. Do not attempt to implement it anyway. Proposing a better alternative or declining with reasoning is preferable to producing bad code.

## Notable Third-Party Packages

- **VContainer** — DI container
- **R3** — reactive extensions
- **Unity InputSystem** 1.19.0
- **TextMesh Pro** — UI text
- **Steamworks.NET** — Steam distribution/achievements
