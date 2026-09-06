using System;
using System.IO;
using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;
using Warlander.Deedplanner.Cameras;
using Warlander.Deedplanner.Composition;
using Warlander.Deedplanner.Domain.Entities.Decorations;
using Warlander.Deedplanner.Domain.Entities.Floors;
using Warlander.Deedplanner.Domain.Entities.Roofs;
using Warlander.Deedplanner.Editing;
using Warlander.Deedplanner.Persistence;
using Warlander.Deedplanner.Ui.Home;

namespace Warlander.Deedplanner.Platform
{
    public static class AgentCommands
    {
        private const int MaxSurfaceLevels = 16;

        [CliCommand("app_await_ready", "Block until the app is fully loaded (playing, MainScene active, initial map present), then return app status. Use CLI --timeout above timeoutSeconds.")]
        public static async Task<object> AwaitReady(
            [CliArg("timeoutSeconds", "Max seconds to wait before failing")] int timeoutSeconds = 60,
            [CliArg("waitFrames", "Extra rendered frames to wait after ready (visual settle margin)")] int waitFrames = 1)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            while (DateTime.UtcNow < deadline)
            {
                if (CheckReady())
                {
                    await WaitFramesAsync(waitFrames);
                    return BuildStatus(true);
                }
                await Task.Delay(100);
            }
            throw new TimeoutException($"App not ready after {timeoutSeconds}s. Last state: {BuildStatus(false)}");
        }

        [CliCommand("app_status", "Instant structured snapshot: scene, map, tab, camera, save state. Does not wait.")]
        public static object AppStatus()
        {
            return BuildStatus(CheckReady());
        }

        [CliCommand("map_new", "Create a new empty map and hide the home screen.")]
        public static async Task<object> MapNew(
            [CliArg("width", "Map width in tiles")] int width = 25,
            [CliArg("height", "Map height in tiles")] int height = 25)
        {
            IObjectResolver resolver = RequirePlayMode();
            await resolver.Resolve<SaveCoordinator>().NewMapAsync(width, height);
            resolver.Resolve<IHomeScreenPresenter>().HideHomeScreen();
            return MapInfo();
        }

        [CliCommand("map_load", "Load a map from a file path and hide the home screen.")]
        public static async Task<object> MapLoad([CliArg("path", "Full path to the map file")] string path)
        {
            IObjectResolver resolver = RequirePlayMode();
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Map file not found: {path}");
            }
            var location = new MapLocation(SaveBackendId.File, path, Path.GetFileNameWithoutExtension(path));
            bool loaded = await resolver.Resolve<SaveCoordinator>().LoadAsync(location);
            if (!loaded)
            {
                throw new InvalidOperationException($"LoadAsync returned false for: {path}");
            }
            resolver.Resolve<IHomeScreenPresenter>().HideHomeScreen();
            return MapInfo();
        }

        [CliCommand("map_save", "Quicksave the current map to its current location. Fails if the map was never saved (no Save As from CLI yet).")]
        public static async Task<object> MapSave()
        {
            IObjectResolver resolver = RequirePlayMode();
            SaveCoordinator saves = resolver.Resolve<SaveCoordinator>();
            if (!saves.CanQuickSave)
            {
                throw new InvalidOperationException("Map has no current save location; quicksave unavailable.");
            }
            bool saved = await saves.QuickSaveAsync();
            return new { saved, location = saves.CurrentLocation?.Locator };
        }

        [CliCommand("map_info", "Compact map summary: size, dirty flag, location, entity counts (surface levels 0-15 only; cave content not counted).")]
        public static object MapInfo()
        {
            IObjectResolver resolver = RequirePlayMode();
            Domain.Map map = resolver.Resolve<MapHandler>().Map;
            if (map == null)
            {
                throw new InvalidOperationException("No map loaded.");
            }

            int walls = 0, floors = 0, roofs = 0, decorations = 0;
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    Domain.Tile tile = map[x, y];
                    if (tile == null)
                    {
                        continue;
                    }
                    for (int level = 0; level < MaxSurfaceLevels; level++)
                    {
                        if (tile.GetVerticalWallOrFence(level) != null) walls++;
                        if (tile.GetHorizontalWallOrFence(level) != null) walls++;
                        Domain.LevelEntity content = tile.GetTileContent(level);
                        if (content is Floor) floors++;
                        else if (content is Roof) roofs++;
                    }
                    foreach (Decoration _ in tile.GetDecorations()) decorations++;
                    if (tile.GetCentralDecoration() != null) decorations++;
                }
            }

            SaveCoordinator saves = resolver.Resolve<SaveCoordinator>();
            return new
            {
                width = map.Width,
                height = map.Height,
                dirty = map.IsDirty,
                location = saves.CurrentLocation?.Locator,
                walls,
                floors,
                roofs,
                decorations,
                bridges = map.Bridges.Count,
                docks = map.Docks.Count
            };
        }

        [CliCommand("tab_select", "Switch the active editing tab.")]
        public static object TabSelect([CliArg("name", "Tab name (case-insensitive)")] string name)
        {
            IObjectResolver resolver = RequirePlayMode();
            if (!Enum.TryParse(name, true, out Tab tab))
            {
                throw new ArgumentException($"Unknown tab '{name}'. Valid: {string.Join(", ", Enum.GetNames(typeof(Tab)))}");
            }
            TabContext tabContext = resolver.Resolve<TabContext>();
            tabContext.CurrentTab = tab;
            return new { tab = tabContext.CurrentTab.ToString() };
        }

        [CliCommand("camera_set", "Set camera mode (fpp/perspective, wurmian, top, iso/isometric) and optionally rendered level. No position control (controllers own it).")]
        public static object CameraSet(
            [CliArg("mode", "Camera mode")] string mode,
            [CliArg("level", "Rendered level; negative = cave. Omit to keep current")] int level = int.MinValue)
        {
            IObjectResolver resolver = RequirePlayMode();
            CameraMode cameraMode = ParseCameraMode(mode);
            MultiCamera camera = resolver.Resolve<CameraCoordinator>().Current;
            camera.CameraMode = cameraMode;
            if (level != int.MinValue)
            {
                camera.Level = level;
            }
            return new { mode = camera.CameraMode.ToString(), level = camera.Level };
        }

        [CliCommand("await_idle", "Wait until no save/load is in progress, then wait N rendered frames.")]
        public static async Task<object> AwaitIdle(
            [CliArg("frames", "Rendered frames to wait after idle")] int frames = 2,
            [CliArg("timeoutSeconds", "Max seconds to wait for idle")] int timeoutSeconds = 30)
        {
            IObjectResolver resolver = RequirePlayMode();
            SaveCoordinator saves = resolver.Resolve<SaveCoordinator>();
            DateTime deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            while (saves.Busy)
            {
                if (DateTime.UtcNow >= deadline)
                {
                    throw new TimeoutException($"Still busy after {timeoutSeconds}s.");
                }
                await Task.Delay(50);
            }
            await WaitFramesAsync(frames);
            return new { idle = true, frames };
        }

        [CliCommand("edit_undo", "Undo the last map edit. Empty stack is a silent no-op (CommandManager exposes no CanUndo).")]
        public static object EditUndo()
        {
            IObjectResolver resolver = RequirePlayMode();
            resolver.Resolve<MapHandler>().Map.CommandManager.Undo();
            return new { ok = true };
        }

        [CliCommand("edit_redo", "Redo the last undone map edit. Empty stack is a silent no-op.")]
        public static object EditRedo()
        {
            IObjectResolver resolver = RequirePlayMode();
            resolver.Resolve<MapHandler>().Map.CommandManager.Redo();
            return new { ok = true };
        }

        private static bool CheckReady()
        {
            try
            {
                if (!Application.isPlaying || SceneManager.GetActiveScene().name != SceneNames.MainScene)
                {
                    return false;
                }
                IObjectResolver resolver = ResolveScope();
                return resolver != null && resolver.Resolve<MapHandler>().Map != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static object BuildStatus(bool ready)
        {
            if (!Application.isPlaying)
            {
                return new { state = "editMode", ready = false };
            }

            IObjectResolver resolver = ResolveScope();
            if (resolver == null)
            {
                return new { state = "loading", activeScene = SceneManager.GetActiveScene().name, ready = false };
            }

            try
            {
                MapHandler mapHandler = resolver.Resolve<MapHandler>();
                SaveCoordinator saves = resolver.Resolve<SaveCoordinator>();
                TabContext tabContext = resolver.Resolve<TabContext>();
                IHomeScreenView homeScreen = resolver.Resolve<IHomeScreenView>();
                MultiCamera camera = resolver.Resolve<CameraCoordinator>().Current;
                Domain.Map map = mapHandler.Map;
                return new
                {
                    state = ready ? "ready" : "loading",
                    activeScene = SceneManager.GetActiveScene().name,
                    ready,
                    map = map != null
                        ? new { width = map.Width, height = map.Height, dirty = map.IsDirty }
                        : null,
                    currentTab = tabContext.CurrentTab.ToString(),
                    saveBusy = saves.Busy,
                    saveLocation = saves.CurrentLocation?.Locator,
                    homeScreenVisible = homeScreen.Visible,
                    camera = new { mode = camera.CameraMode.ToString(), level = camera.Level }
                };
            }
            catch (Exception)
            {
                return new { state = "loading", activeScene = SceneManager.GetActiveScene().name, ready = false };
            }
        }

        private static IObjectResolver ResolveScope()
        {
            try
            {
                return LifetimeScope.Find<MainSceneLifetimeScope>()?.Container;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static IObjectResolver RequirePlayMode()
        {
            if (!Application.isPlaying)
            {
                throw new InvalidOperationException("Requires play mode.");
            }
            IObjectResolver resolver = ResolveScope();
            if (resolver == null)
            {
                throw new InvalidOperationException("MainScene scope not available (app still loading?).");
            }
            return resolver;
        }

        private static CameraMode ParseCameraMode(string mode)
        {
            switch (mode.ToLowerInvariant())
            {
                case "fpp":
                case "perspective":
                    return CameraMode.Perspective;
                case "wurmian":
                    return CameraMode.Wurmian;
                case "top":
                    return CameraMode.Top;
                case "iso":
                case "isometric":
                    return CameraMode.Isometric;
                default:
                    throw new ArgumentException($"Unknown camera mode '{mode}'. Valid: fpp, wurmian, top, iso");
            }
        }

        private static async Task WaitFramesAsync(int frames)
        {
            int target = Time.frameCount + frames;
            while (Time.frameCount < target)
            {
                await Task.Delay(15);
            }
        }
    }
}
