---
name: unity-ui-screenshot
description: Capture real screenshots of DeedPlanner 3 UI panels from the connected Unity Editor via CLI, for UI design/mockup work. Use whenever a task involves designing, documenting, or mocking up DP3 UI — never infer layout from view code alone.
---

# Unity UI Screenshot — DeedPlanner Capture

View code tells what a panel contains, never how it looks. For any UI design, mockup, or doc work, capture the REAL panel first.

**Mockup fidelity rule:** mockups do NOT need to match the original UI's look (theme, colors, fonts) faithfully — but they SHOULD match the **layout** as closely as possible: which sections exist, their order, their relative sizes, what controls each holds. Layout-faithful mockups implement with fewer surprises. Capture first, copy structure, restyle freely.

For turning an approved mockup into real Unity UI, use the `unity-ui-build` skill.

## Prerequisites

- Editor connected: `unity status`. If not connected: `unity open "E:/Unity/DeedPlanner-3" --args "-automated"`, then `unity status --until-ready --project-path "E:/Unity/DeedPlanner-3" --timeout 120`. On SteamOS/Linux retain `--args "-automated -noaudio"`; a Play-mode FMOD crash was observed on 6000.3.20f1 and has not been rechecked on Linux with the new Editor.

Generic CLI and `script eval --file` rules live in `AGENTS.md` and `unity-cli`; do not duplicate them here.

## Capture constraint

- A raw camera `Render()` does not composite overlay canvases. Prefer Pipeline's `capture game_view --source screen`; Pipeline 0.10 can also composite overlay UI for URP/HDRP camera captures with `--overlay true`.
- Verify that Play-mode frames advance before trusting a capture; an unfocused or blocked Game view can show a stale frame.
- Guess-clicking coordinates with `runtime simulate_pointer` is brittle and resolution/origin dependent.

## Reliable capture workflow (play mode)

1. Enable `unity command editor set_auto_tick` and `unity command editor.playmode set_run_in_background`, then enter with `unity command editor.playmode start`.
2. Wait for the Editor with `unity status --until-ready --project-path "E:/Unity/DeedPlanner-3" --timeout 120`, then for the app with `unity command app_await_ready --timeout 130 -- --timeoutSeconds 120`. Confirm frames advance using the `unity-cli` verification loop.
3. Set up the target state with project commands: `map_load <path>` or `map_new`, then `tab_select <name>` and `camera_set <mode> [level]` as needed.
4. Wait for saves and rendered frames: `unity command await_idle`.
5. Capture to a file: `unity command capture game_view -- --source screen --output-path <path>`. Inspect the PNG; the active app panel is in the right column.
6. **Cleanup (mandatory):** `unity command editor.playmode stop`, restore the background override with `editor.playmode set_run_in_background --enabled false`, and disable auto-tick with `editor set_auto_tick --enabled false`, even when setup or capture fails.

## Reference layout (verified 2026-08-21)

Right column top-down: tab toggles grid (Ground, Height, Floors, Walls, Roofs, Objects / Labels, Borders, Bridges, Mirror, Tools, Menu) → level chooser (WU/ISO/3D/2D + levels 16..-6) → active tab panel. Floors panel: "Floor Orientation" serif header with compass N/W/E/S buttons, search bar, tree (Floors, Materials, Openings, Staircases, Unfinished).
