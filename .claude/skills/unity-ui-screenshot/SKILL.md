---
name: unity-ui-screenshot
description: Capture real screenshots of DeedPlanner 3 UI panels from the connected Unity Editor via CLI, for UI design/mockup work. Use whenever a task involves designing, documenting, or mocking up DP3 UI — never infer layout from view code alone.
---

# Unity UI Screenshot — DeedPlanner Capture

View code tells what a panel contains, never how it looks. For any UI design, mockup, or doc work, capture the REAL panel first.

**Mockup fidelity rule:** mockups do NOT need to match the original UI's look (theme, colors, fonts) faithfully — but they SHOULD match the **layout** as closely as possible: which sections exist, their order, their relative sizes, what controls each holds. Layout-faithful mockups implement with fewer surprises. Capture first, copy structure, restyle freely.

For turning an approved mockup into real Unity UI, use the `unity-ui-build` skill.

## Prerequisites

- Editor connected: `unity status`. If not connected: `unity open "E:/Unity/DeedPlanner-3" --args "-automated"` as background task (never wait on it), continue other work, check status once later.

Generic CLI and `eval_file` rules live in `AGENTS.md` and `unity-cli`; do not duplicate them here.

## Capture constraint

- **Edit-mode capture of screen-space-overlay UI**: camera `Render()` does not composite overlay canvases. Result: blank grey image. Same for `capture_game_view` in edit mode (GameView does not repaint unfocused).
- Guess-clicking coordinates with `simulate_pointer` — brittle, resolution/origin dependent.

## Reliable capture workflow (play mode)

1. Enter play mode: `unity command editor_play`.
2. Wait for the app: `unity command app_await_ready --timeoutSeconds 120 --timeout 130`.
3. Set up the target state with project commands: `map_load <path>` or `map_new`, then `tab_select <name>` and `camera_set <mode> [level]` as needed.
4. Wait for saves and rendered frames: `unity command await_idle`.
5. Capture to a file: `unity command screenshot --output <path>`. Inspect the PNG; the active editor panel is in the right column.
6. **Cleanup (mandatory):** `unity command editor_stop`, even when setup or capture fails.

## Reference layout (verified 2026-08-21)

Right column top-down: tab toggles grid (Ground, Height, Floors, Walls, Roofs, Objects / Labels, Borders, Bridges, Mirror, Tools, Menu) → level chooser (WU/ISO/3D/2D + levels 16..-6) → active tab panel. Floors panel: "Floor Orientation" serif header with compass N/W/E/S buttons, search bar, tree (Floors, Materials, Openings, Staircases, Unfinished).
