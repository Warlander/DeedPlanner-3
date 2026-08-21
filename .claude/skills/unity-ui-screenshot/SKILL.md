---
name: unity-ui-screenshot
description: Capture real screenshots of DeedPlanner 3 UI panels from the connected Unity Editor via CLI, for UI design/mockup work. Use whenever a task involves designing, documenting, or mocking up DP3 UI — never infer layout from view code alone.
---

# Unity UI Screenshot

View code tells what a panel contains, never how it looks. For any UI design, mockup, or doc work, capture the REAL panel first.

**Mockup fidelity rule:** mockups do NOT need to match the original UI's look (theme, colors, fonts) faithfully — but they SHOULD match the **layout** as closely as possible: which sections exist, their order, their relative sizes, what controls each holds. Layout-faithful mockups implement with fewer surprises. Capture first, copy structure, restyle freely.

For turning an approved mockup into real Unity UI, use the `unity-ui-build` skill.

## Prerequisites

- Editor connected: `unity status`. If not connected: `unity open "E:/Unity/DeedPlanner-3" --args "-automated"` as background task (never wait on it), continue other work, check status once later.

## eval_file rules (hard-won, do not skip)

`eval_file` wraps file content in an `Execute()` method body:

- **No `using` directives, no class/method definitions** — statements only.
- **Fully qualify everything**: `UnityEngine.Object.FindObjectsOfType<...>`, `System.IO.Path`, `TMPro.TMP_Text`.
- `UnityEngine.Object` collides with `object` — always write the full name.
- Must end with an explicit `return <value>;` (bare expressions rejected).
- Write scripts to SYSTEM TEMP (`$env:TEMP`), never under `Assets/` (breaks compile, spams SourceAssetDB).
- Invoke: `unity command eval_file --file <path> --json`

## What does NOT work

- **Edit-mode capture of screen-space-overlay UI**: camera `Render()` does not composite overlay canvases. Result: blank grey image. Same for `capture_game_view` in edit mode (GameView does not repaint unfocused).
- Guess-clicking coordinates with `simulate_pointer` — brittle, resolution/origin dependent.

## Reliable capture workflow (play mode)

1. Open the scene if needed: eval `UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");`
2. `unity command editor_focus` (play entry stalls unfocused unless Editor launched with `-automated`), then `unity command editor_play`.
3. App boots to the HOME SCREEN. Drive it via component methods, not pointer coordinates:
   - Load a map: find `UnityEngine.UI.Button` whose child `TMPro.TMP_Text.text` matches the save name, call `btn.onClick.Invoke()`.
   - Select a tab: find `UnityEngine.UI.Toggle` by name (e.g. "Floors" under Tab Toggles Panel), set `tg.isOn = true`.
4. Wait a few seconds for load, then screenshot: eval `UnityEngine.ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dp_ui.png"));` — play-mode capture includes overlay UI. Wait ~2s, Read the PNG (full screen, 2560x1296 — panel is the right column).
5. **Cleanup (mandatory)**: `unity command editor_stop`, then reopen the scene via `OpenScene` (no save) to discard any scene mutations made during setup.

## Reference layout (verified 2026-08-21)

Right column top-down: tab toggles grid (Ground, Height, Floors, Walls, Roofs, Objects / Labels, Borders, Bridges, Mirror, Tools, Menu) → level chooser (WU/ISO/3D/2D + levels 16..-6) → active tab panel. Floors panel: "Floor Orientation" serif header with compass N/W/E/S buttons, search bar, tree (Floors, Materials, Openings, Staircases, Unfinished).
