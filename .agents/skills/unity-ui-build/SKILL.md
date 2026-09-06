---
name: unity-ui-build
description: DeedPlanner 3 overlay for implementing approved UI mockups as real uGUI. Use with ui and ui-ugui when turning a design into panels, buttons, or palettes; owns project-specific prefab reuse, MVP wiring, and verification.
---

# Unity UI Build — DeedPlanner Overlay

Load `ui` and `ui-ugui` first for generic Unity UI guidance. This skill adds or overrides only DeedPlanner-specific rules. Pair it with `unity-ui-screenshot` for before/after capture.

`AGENTS.md` owns the project-wide rules: MVP, `.meta` GUID preservation, Unity CLI usage, and the verification ladder.

## DeedPlanner overrides to `ui-ugui`

- Match existing C# imports; fully qualify UI types only where ambiguity requires it and always in CLI eval code.
- Keep C# with its owning module, not beside prefabs. Updater views live in `Assets/Warlander/Deedplanner/Editing/<Tab>/`; shared UI lives under `Ui/<Feature>/`; bridge widgets live under `Bridges/Widgets/`.
- Keep prefabs in the established `Assets/Prefabs/` hierarchy.
- Fix existing hierarchy objects in place. Deliberate, approved removal of dead UI is allowed after updating serialized references.

## Prefab rules (owned here, non-negotiable)

- **Reuse existing prefabs.** Before building UI, check `Assets/Prefabs/` for a similar control: `Assets/Prefabs/Gui/Simple Button.prefab`, `Assets/Prefabs/Gui/Icon and Text Toggle Button.prefab`, tab panels in `Assets/Prefabs/MainScene/Tabs/`, and tree/search composites. Never hand-build what a prefab already does.
- **Wire inside the parent prefab context**, never as scene overrides. If the things being wired live inside a prefab, edit that prefab, not the scene instance.
- **Delete dead UI from prefabs.** Never disable with `SetActive(false)` and leave it — remove unused objects outright.
- New tab panels follow the `* Tab.prefab` convention with a `UIContentTab` component (serialized `tab` int must match the `Tab` enum value).

## MVP wiring specifics

- Keep each view and its `I*View` interface with the owning module path described above.
- Register in `Composition/MainSceneLifetimeScope.cs`: view via `RegisterComponentInHierarchy`, presenter via `RegisterEntryPoint`. Updaters register `.As<IUpdater>()` (add `.AsSelf()` if presenters depend on the concrete updater, like `BridgesUpdater`).
- Input: reuse the shared `UpdatersShared` action map (LMB place / RMB delete) — no new bindings without asking.

## Editing via CLI (connected Editor)

Discover names with `unity list`. Useful commands: `create_gameobject`, `add_component`, `set_parent`, `set_transform`, `set_serialized_field`, `instantiate_prefab`, `set_active`, `rename_gameobject`, `delete_gameobject`, `save_prefab_contents`, `save_scene`. Prefer prefab-stage edits, then `save_prefab_contents`. Use `move_asset`/`rename_asset` for file moves (CLAUDE.md's GUID rule).

## Layout fidelity checklist (mockup → UI)

Match the mockup's LAYOUT, not its pixels:

1. Same sections in the same order, same controls per section.
2. Relative sizes preserved (a tree that dominates the mockup must dominate the panel).
3. Sizing strategy per element decided up front: fixed (buttons), stretch (search bar, tree), aspect-locked (icons). Use anchors + LayoutGroups (VerticalLayoutGroup for panel sections, GridLayoutGroup for palettes) instead of hand-placed rects.
4. Themed via the same sprites/fonts the neighboring panels use — copy component settings from an existing tab panel rather than restyling from scratch.

## Verify

1. Compile clean (CLAUDE.md verification ladder).
2. Capture the built panel with the `unity-ui-screenshot` workflow, compare against the mockup section by section.
3. Exit play mode, reopen scene without saving (or save scene deliberately if edits were in-scene and intended).
