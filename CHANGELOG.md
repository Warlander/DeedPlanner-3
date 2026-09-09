# Changelog

## [3.4.0] - Unreleased

- Cave level controls and cave editing are now enabled, with Ground/Caves controls that swap with the active realm, reliable 3D transitions between surface and cave levels, independent per-camera levels, complete cave shells in 3D, distinct wall and floor rendering in planning views, absolute ceiling-height editing, concise height-editing guidance, optional ceiling-preserving floor edits, cave rendering controls, responsive underground views on large deeds, legacy-map clearance repair, consistent solid-rock painting across camera modes, and correct surface rendering
- Docks can now be planned over water from the Floors tab - paint dock decks with multiple materials and support types, with full undo, validation and save/load support
- New saving system with a home screen on startup listing your deeds with thumbnails - saves can now be renamed and deleted
- Quick save and auto-save with crash recovery - unsaved work is no longer lost on a crash
- Saves are now stored in Steam Cloud on Steam and in browser storage on the web version, with export still available for sharing
- Bridge pavements: bridge lanes can now be paved with different materials per lane, including new tarred planks
- Hovering over a segment in the bridge bar now highlights it in the world
- New 4K screenshot options, including 4K with UI visible (unbound by default, can be bound in settings)
- Lighting and visuals overhaul: tonemapping, bloom, ambient occlusion, improved anti-aliasing and retuned shadows
- Sharper water reflections on Ultra quality
- Settings window overhaul - a single immediate-apply tabbed window now covers all settings, with reliable cancellable key rebinding
- Errors now open a window with a copyable report you can attach when reporting bugs
- Richer tooltips, including accurate edge-aware placement and a tinted slope grid when hovering terrain on the Height tab
- New dragon HOTA statue model (the old one was oversized)
- Added aged log wall texture
- Improved Floor tab UI
- Funding links are now hidden in the Steam version (EULA requirement)
- Pastebin export is disabled on the web version (blocked by browsers)
- Fixed bridge editing clicks being blocked by walls
- Fixed bridges wider than long not reaching ground height at the far end
- Fixed bridge supports and deck not following terrain height edits
- Fixed decorations loading at the previous map's terrain heights
- Fixed decoration rotation snapping only working in one direction
- Fixed empty tooltip box appearing when dragging the map over terrain
- Fixed map being lost after a failed load
- Fixed redo order getting mixed up in grouped edits
- Fixed lower floors not darkening in 2D and isometric views
- Fixed compass not hiding when the UI is toggled off
- Fixed UI toggle (F10) desyncing the bridge bar and tabs
- Fixed ground smoothness rendering
- Fixed ground texture errors continuing after leaving Play Mode in the Editor
- Fixed textures sometimes failing to load
- Maps referencing objects missing from game data now load with fallbacks instead of failing
- Fixed exporting a map disabling auto-save recovery for later edits
- Fixed file save errors blocking all later saves and loads

## [3.3.2] - 2026-08-15

- Bridges can now be planned over existing buildings - clicking a building selects the tile under it, with the bridge level derived from where you clicked so it should always work reliably
- Bridge planning hover tooltip now shows the endpoint floor relative to your currently active floor
- Clicking an already selected tile now deselects the bridge endpoint
- Fixed a crash when clicking a bridge while on the Floors or Walls tab
- Fixed open bridge undersides rendering inside-out
- Added an option to toggle bridge visibility on and off
- Level selector now locks to the ground floor while on the Ground or Height tab
- Mac app name is now consistent with Windows and Linux (Steam fix)

## [3.3.1] - 2026-08-09

- Fixed rope bridge sag inaccurate calculations
- Fixed reversed facing of S-N bridge segments of wide bridges
- Fixed incorrect facing of first and last left and right segments of 3 tile wide bridges

## [3.3.0] - 2026-08-09

- bridges can now be placed, edited and removed, as well as loaded/saved to/from maps
- full render pipeline migration from legacy Unity rendering system to new URP
- water is fully reworked and optimized
- ultra quality is now default on all platforms
- added tooltips telling nearest decoration name
- reworked outlines - outlines hug what they are pointing at now instead of showing a box around it
- experimental support for Mac builds
- builds for all platforms now use IL2CPP - guarantying better performance
- fixed roofs sometimes not being recalculated
- fixed rare duplicate map load bug
- fixed rare duplicate texture load bug
- fixed some models missing their textures

## [3.2.2] - 2026-01-26

- [Warlander] Fixed roofs being invisible in WebGL builds of the game
- [Ksunyo] Fixed wide log windows incorrect appearance

## [3.2.1] - 2026-01-25

- [Warlander & Saroman] Added log walls
- [KSunyo] Added compass
- [Warlander] Upgraded Unity to 6000.3.4f2
- [Warlander] Refactored Wurm asset loading code to make it less of a headache to work with
- [Warlander] Implemented feature states system, used to toggle features (such as bridges) off and on depending on current environment - production builds, debug builds and editor

## [3.2.0] - 2025-10-12

- **Big UI overhaul (huge thanks to KSunyo!)**
- **Added customizable keybinds**
- **Added dozens of new items (huge thanks to Drogos!)**
- Added outlines
- Improved wireframe grid rendering. This is especially noticeable on distant terrain
- Added tab UI animations
- Upgraded Unity to 6000.0.58f2
- Fixed Unity security vulnerability
- Added sun angle slider (huge thanks to Drogos!)
- Fixed flickering in window title bar when moving windows around
- Fixed texture path combining bug causing some textures to load twice
- Optimized heightmap handles rendering and caching. This should significantly improve loading times of large maps, reduce RAM usage and improve performance on large maps during height editing
- Optimized UI rendering
- Optimized program loading speed
- Overhauled camera system
- Overhauled windows system
- Added new app icons
- Replaced obsolete file browser
- Transitioned from legacy Unity input system to a new input system
- Fixed WurmAssetsLoader failing to load some models with LOD
- Fixed isometric bounds for rectangular deeds (huge thanks to Drogos!)
- You can now rotate isometric view with page up and page down (huge thanks to Drogos!)
- You can now rotate items with ctrl/shift + scrollwheel before placing (huge thanks to Drogos!)
- You can now delete single items, by holding mouse over an item, so it goes red, and then press delete button to delete that single item. (huge thanks to Drogos!)
- (Windows only) Added support for file drag and drop to load maps
- (web only) Disabled saving and loading modes that depend on CORS proxy to work. Saving and loading to/from file will work as expected
- (Mac only) Improved Retina display support

## [3.1.1] - 2020-06-01

- Fixed some objects with LOD's having invalid texturing
- Fixed materials calculator floor-depending modes only working on floor 0
- Fixed unfinished palisade gates WebGL crash

## [3.1.0] - 2020-05-02

- Added materials calculations for:
  - Everything on map
  - Single building
  - Single floor of a building
  - Single room
- Added map warnings for:
  - Walls outside of known buildings
  - Walls on sloped terrain
- Added visibility toggles for:
  - All objects
  - Trees
  - Bushes
  - Ships
- Added materials caching (will improve performance for models that share textures)
- Added windows fade in/out animations
- Added support for movement multipliers for vertical FPP movement
- Added several tooltips to clarify some common questions about the program
- Made sure placement of objects won't be interrupted when pointing at walls
- Made program run in background during initial loading and map loading
- Made program cache models and textures in WebGL version in order to prevent excessive bandwidth use
- Improved performance of model and texture loading
- Fixed maps exported by in-game exporter not handling walls on map edges correctly
- Fixed map loading heavily slowing down or stalling completely when loading huge maps
- Upgraded Unity to 2019.3.0f3

## [3.0.7] - 2019-10-29

- Added missing tapestries
- Added all PMK flags, banners and tall banners
- Added small barrel racks and wine barrel racks
- Added template kingdom wagons
- Added fishing rods rack
- Added extra categories to many objects for easier search
- Renamed "Fireplace" to "Open fireplace"

## [3.0.6] - 2019-10-22

- Added warnings when trying to edit openings/stairs/roofs on ground floor
- Added support for direct map loading from Dropbox and Google Drive
- Added GUI scaling option
- Added search box to all trees in GUI (grounds, floors, walls, objects)
- Added tile coordinates to height editing tooltips
- Added option to toggle GUI with F10 key
- Upgraded Unity version used for project from 2019.2.0f1 to 2019.2.9f1
- Optimized heightmap handles rendering
- Made ramp editing tool make ramp from anchor to selected point immediately if respecting original slopes is turned off
- Renamed Pastebin loading to web loading
- Moved donation buttons to menu instead of being visible everywhere
- Fixed decoration ghost not being displayed on proper floor
- Fixed centered decorations and vegetation being allowed to be placed on non-ground floors
- Fixed being able to overwrite decorations when it's not allowed
- Fixed not being able to load non-compressed DP maps from web
- Fixed chance of ground corruption when spamming ground fill function
- Fixed being able to move camera around when typing in input fields
- Fixed ramp editing tool not resetting its state correctly if action is canceled
- Fixed ramp editing tool incorrectly using height at ramp end as height as ramp start
- Fixed timing issues when swapping walls, creating time window during which you can accidentally place walls behind the swapped wall
- Fixed wall memory leak when wall is refreshed (for example, during the slope change)
- Fixed camera focus issues when on menu tab
- **Fixed program loading issues on non-Windows standalone versions**

## [3.0.5] - 2019-09-28

- Fixed non-removable decorations
- Fixed some roofs having incorrect models after every model check
- Fixed minor memory leak on map loading

## [3.0.4] - 2019-09-26

- Added asynchronous map loading
- Memory optimizations
- Optimized wall rendering
- Fixed download cache memory leak
- Fixed memory spikes on DXT textures loading
- Fixed ground not being shaded and disabled when switching floors in 2d and isometric views
- Fixed grass and tile height tooltips not appearing
- Fixed wall rotation not being read and saved properly
- Fixed corrupted caravels
- Fixed Y axis flipped on DP2-imported maps
- Fixed arched walls flipped on DP2-imported maps

## [3.0.3] - 2019-09-19

- Added Jackal trees
- Added Jackal structures (beacons and lodestone)
- Added all types of flower pots
- Added cupboard
- Added chicken coop
- Added small and large pottery amphora
- Added missing archaeology statues
- Made different types of marble planters actually different
- Improved readability of most text in the application
- Improved handling of long entity names in GUI lists and trees
- Changed names of all trees and bushes to include "tree" and "bush" in their names
- Changed names of archaeology statues to their in-game names
- Fixed undo/redo stack overflow and added safeguard to make sure it won't lead to map-corrupting bugs anymore

## [3.0.2] - 2019-09-08

- Optimized ground and grid rendering
- Optimized texture memory consumption
- Fixed Pastebin and web version saving working incorrectly in WebGL version
- Fixed being able to toggle all editing modes off
- Fixed mouse picking overlay of floors, walls and roofs
- Fixed decorations updater being able to get stuck during setting rotation
- Fixed incorrectly rendered models after using undo/redo on different floors
- Fixed banners not having kingdom textures
- Browser version: added ignoring ctrl+key browser shortcuts to make ctrl+c/ctrl+v work properly

## [3.0.1] - 2019-08-28

- Added option to pick ground type with alt+left/right click
- Added object corner snapping when "snap to grid" is enabled
- Added 2x anti-aliasing as default for WebGL version
- Fixed wrong resolution being used for in-game cameras
- Fixed objects tab saving user settings on every frame, leading to excessive HDD/SSD usage
- Fixed corner-aligned objects not being placed sometimes
- Fixed decorations floating around on slopes

## [3.0.0] - 2019-08-27

- Initial release
