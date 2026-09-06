using Warlander.Deedplanner.Ui;
using Warlogic.Settings;

namespace Warlander.Deedplanner.Settings
{
    /// <summary>
    /// Declares every game setting in one place, wires them into a registry backed by the
    /// PlayerPrefs JSON store, and exposes per-module translators for consumers.
    /// The Keybinds tab is not declared here - keybinds need the scene-scoped DPInput,
    /// see KeybindSettingsRegistrar.
    /// </summary>
    public sealed class DeedPlannerSettings
    {
        public SettingsRegistry Registry { get; }
        public ISettingsStore Store { get; }
        public CameraSettings Camera { get; }
        public EditingSettings Editing { get; }
        public UiSettings Ui { get; }
        public GraphicsOptions Graphics { get; }

        private DeedPlannerSettings(SettingsRegistry registry, ISettingsStore store, CameraSettings camera,
            EditingSettings editing, UiSettings ui, GraphicsOptions graphics)
        {
            Registry = registry;
            Store = store;
            Camera = camera;
            Editing = editing;
            Ui = ui;
            Graphics = graphics;
        }

        public static DeedPlannerSettings Create()
        {
            var store = new PlayerPrefsJsonSettingsStore();
            LegacySettingsMigration.Migrate(store);
            var registry = new SettingsRegistry(store);

            SettingsTab generalTab = registry.AddTab("general", "General");
            SettingsTab graphicsTab = registry.AddTab("graphics", "Graphics");
            SettingsTab camerasTab = registry.AddTab("cameras", "Cameras");

            var guiScale = new IntSetting("guiScale", "GUI Scale", 10, 5, 20, "Scales the whole user interface.");
            generalTab.Add(guiScale);
            var compassVisibility = new BoolSetting("compassVisibility", "Show Compass", true);
            generalTab.Add(compassVisibility);

            var waterQuality = new EnumSetting<WaterQuality>("waterQuality", "Water Quality", WaterQuality.Ultra,
                "Simple - very basic water that uses almost no resources\nHigh - fancy water without reflections\nUltra - fancy water with reflections");
            graphicsTab.Add(waterQuality);
            var qualityLevel = new EnumSetting<QualityLevel>("qualityLevel", "Quality Level",
                (QualityLevel) UnityEngine.QualitySettings.GetQualityLevel(), "Controls shadow quality, post-processing and ambient occlusion.");
            graphicsTab.Add(qualityLevel);

            var fppMouseSensitivity = new FloatSetting("fppMouseSensitivity", "Mouse Sensitivity", 0.5f, 0.05f, 3f);
            camerasTab.Add(fppMouseSensitivity);
            var fppKeyboardRotationSensitivity = new FloatSetting("fppKeyboardRotationSensitivity", "Keyboard Rotation Sensitivity", 60f, 10f, 180f);
            camerasTab.Add(fppKeyboardRotationSensitivity);
            var fppMovementSpeed = new FloatSetting("fppMovementSpeed", "First-Person Movement Speed", 16f, 1f, 64f);
            camerasTab.Add(fppMovementSpeed);
            var topMovementSpeed = new FloatSetting("topMovementSpeed", "Top-Down Movement Speed", 16f, 1f, 64f);
            camerasTab.Add(topMovementSpeed);
            var isoMovementSpeed = new FloatSetting("isoMovementSpeed", "Isometric Movement Speed", 16f, 1f, 64f);
            camerasTab.Add(isoMovementSpeed);
            var shiftSpeedModifier = new FloatSetting("shiftSpeedModifier", "Boost Speed Modifier", 5f, 1f, 20f);
            camerasTab.Add(shiftSpeedModifier);
            var controlSpeedModifier = new FloatSetting("controlSpeedModifier", "Precise Speed Modifier", 0.2f, 0.05f, 1f);
            camerasTab.Add(controlSpeedModifier);

            // editing settings live in their mode tabs (Height/Wall/Decoration), registered tab-less
            var heightDragSensitivity = new FloatSetting("heightDragSensitivity", "Height Drag Sensitivity", 0.5f, 0.05f, 2f);
            registry.Register(heightDragSensitivity);
            var heightRespectOriginalSlopes = new BoolSetting("heightRespectOriginalSlopes", "Respect Original Slopes", true);
            registry.Register(heightRespectOriginalSlopes);
            var wallAutomaticReverse = new BoolSetting("wallAutomaticReverse", "Automatic Wall Reverse", true);
            registry.Register(wallAutomaticReverse);
            var wallReverse = new BoolSetting("wallReverse", "Wall Reverse", false);
            registry.Register(wallReverse);
            var decorationSnapToGrid = new BoolSetting("decorationSnapToGrid", "Decoration Snap To Grid", false);
            registry.Register(decorationSnapToGrid);
            var decorationRotationSnapping = new BoolSetting("decorationRotationSnapping", "Decoration Rotation Snapping", false);
            registry.Register(decorationRotationSnapping);
            var decorationRotationSensitivity = new FloatSetting("decorationRotationSensitivity", "Decoration Rotation Sensitivity", 1f, 0.1f, 10f);
            registry.Register(decorationRotationSensitivity);

            var camera = new CameraSettings(fppMouseSensitivity, fppKeyboardRotationSensitivity,
                fppMovementSpeed, topMovementSpeed, isoMovementSpeed, shiftSpeedModifier, controlSpeedModifier);
            var editing = new EditingSettings(heightDragSensitivity, heightRespectOriginalSlopes,
                wallAutomaticReverse, wallReverse, decorationSnapToGrid, decorationRotationSnapping,
                decorationRotationSensitivity);
            var ui = new UiSettings(guiScale, compassVisibility);
            var graphics = new GraphicsOptions(waterQuality, qualityLevel);

            return new DeedPlannerSettings(registry, store, camera, editing, ui, graphics);
        }
    }
}
