using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Warlander.Deedplanner.Settings;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Logging;
using Warlogic.Settings;
using VContainer.Unity;

namespace Warlander.Deedplanner.Tests
{
    public class LegacySettingsMigrationTests
    {
        private const string StoreKey = "warlogic.settings.migration-tests";
        private const string PropertiesKey = "properties.migration-tests";
        private const string InputSettingsKey = "inputSettings.migration-tests";

        private const string FullLegacyXml =
            "<DPSettings>" +
            "<FppMouseSensitivity>0.75</FppMouseSensitivity>" +
            "<FppKeyboardRotationSensitivity>90</FppKeyboardRotationSensitivity>" +
            "<FppMovementSpeed>24</FppMovementSpeed>" +
            "<TopMovementSpeed>8</TopMovementSpeed>" +
            "<IsoMovementSpeed>12</IsoMovementSpeed>" +
            "<FppShiftModifier>7</FppShiftModifier>" +
            "<FppControlModifier>0.5</FppControlModifier>" +
            "<HeightDragSensitivity>1.25</HeightDragSensitivity>" +
            "<HeightRespectOriginalSlopes>false</HeightRespectOriginalSlopes>" +
            "<WallAutomaticReverse>false</WallAutomaticReverse>" +
            "<WallReverse>true</WallReverse>" +
            "<DecorationSnapToGrid>true</DecorationSnapToGrid>" +
            "<DecorationRotationSnapping>true</DecorationRotationSnapping>" +
            "<DecorationRotationSensitivity>2.5</DecorationRotationSensitivity>" +
            "<GuiScale>14</GuiScale>" +
            "<WaterQuality>SIMPLE</WaterQuality>" +
            "<CompassVisibility>false</CompassVisibility>" +
            "</DPSettings>";

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(StoreKey);
            PlayerPrefs.DeleteKey(PropertiesKey);
            PlayerPrefs.DeleteKey(InputSettingsKey);
            PlayerPrefs.Save();
        }

        [Test]
        public void TranslatePropertiesXml_FullDocument_TranslatesAllKeys()
        {
            Dictionary<string, string> result = LegacySettingsMigration.TranslatePropertiesXml(FullLegacyXml);

            Assert.AreEqual(17, result.Count);
            Assert.AreEqual("0.75", result["fppMouseSensitivity"]);
            Assert.AreEqual("90", result["fppKeyboardRotationSensitivity"]);
            Assert.AreEqual("24", result["fppMovementSpeed"]);
            Assert.AreEqual("8", result["topMovementSpeed"]);
            Assert.AreEqual("12", result["isoMovementSpeed"]);
            Assert.AreEqual("7", result["shiftSpeedModifier"]);
            Assert.AreEqual("0.5", result["controlSpeedModifier"]);
            Assert.AreEqual("1.25", result["heightDragSensitivity"]);
            Assert.AreEqual("false", result["heightRespectOriginalSlopes"]);
            Assert.AreEqual("false", result["wallAutomaticReverse"]);
            Assert.AreEqual("true", result["wallReverse"]);
            Assert.AreEqual("true", result["decorationSnapToGrid"]);
            Assert.AreEqual("true", result["decorationRotationSnapping"]);
            Assert.AreEqual("2.5", result["decorationRotationSensitivity"]);
            Assert.AreEqual("14", result["guiScale"]);
            Assert.AreEqual("Simple", result["waterQuality"]);
            Assert.AreEqual("false", result["compassVisibility"]);
        }

        [Test]
        public void TranslatePropertiesXml_StringRotationSensitivity_RetypesToFloat()
        {
            Dictionary<string, string> result = LegacySettingsMigration.TranslatePropertiesXml(
                "<DPSettings><DecorationRotationSensitivity>1</DecorationRotationSensitivity></DPSettings>");

            Assert.AreEqual("1", result["decorationRotationSensitivity"]);
        }

        [Test]
        public void TranslatePropertiesXml_MalformedXml_ReturnsEmpty()
        {
            Dictionary<string, string> result = LegacySettingsMigration.TranslatePropertiesXml("{{{not xml");

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void TranslatePropertiesXml_MissingElements_SkipsThem()
        {
            Dictionary<string, string> result = LegacySettingsMigration.TranslatePropertiesXml(
                "<DPSettings><GuiScale>12</GuiScale></DPSettings>");

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("12", result["guiScale"]);
        }

        [Test]
        public void TranslatePropertiesXml_InvalidValues_SkipsThem()
        {
            Dictionary<string, string> result = LegacySettingsMigration.TranslatePropertiesXml(
                "<DPSettings><GuiScale>abc</GuiScale><WaterQuality>Medium</WaterQuality></DPSettings>");

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Migrate_SeededLegacyPrefs_MovesValuesAndDeletesConfirmedProperties()
        {
            PlayerPrefs.SetString(PropertiesKey, FullLegacyXml);
            PlayerPrefs.SetString(InputSettingsKey, "[{\"action\":\"Map/Forward\",\"path\":\"<Keyboard>/w\"}]");
            PlayerPrefs.Save();
            var store = new PlayerPrefsJsonSettingsStore(StoreKey);

            LegacySettingsMigration.Migrate(store, PropertiesKey, InputSettingsKey);

            Assert.IsTrue(store.TryLoad("fppMouseSensitivity", out string sensitivity));
            Assert.AreEqual("0.75", sensitivity);
            Assert.IsTrue(store.TryLoad("decorationRotationSensitivity", out string rotation));
            Assert.AreEqual("2.5", rotation);
            Assert.IsTrue(store.TryLoad("waterQuality", out string water));
            Assert.AreEqual("Simple", water);
            Assert.IsTrue(store.TryLoad(LegacySettingsMigration.BindingOverridesKey, out string overrides));
            Assert.AreEqual("[{\"action\":\"Map/Forward\",\"path\":\"<Keyboard>/w\"}]", overrides);
            Assert.IsFalse(PlayerPrefs.HasKey(PropertiesKey));
            Assert.IsTrue(PlayerPrefs.HasKey(InputSettingsKey));

            var reloaded = new PlayerPrefsJsonSettingsStore(StoreKey);
            Assert.IsTrue(reloaded.TryLoad("guiScale", out string guiScale));
            Assert.AreEqual("14", guiScale);
        }

        [Test]
        public void Migrate_NoLegacyPrefs_LeavesStoreUntouched()
        {
            var store = new PlayerPrefsJsonSettingsStore(StoreKey);

            LegacySettingsMigration.Migrate(store, PropertiesKey, InputSettingsKey);

            Assert.IsFalse(store.TryLoad("guiScale", out _));
            Assert.IsFalse(PlayerPrefs.HasKey(StoreKey));
        }

        [Test]
        public void Migrate_ExistingBindingOverrides_KeepsNewerValue()
        {
            var store = new PlayerPrefsJsonSettingsStore(StoreKey);
            store.Save(LegacySettingsMigration.BindingOverridesKey, "[]");
            PlayerPrefs.SetString(InputSettingsKey, "[{\"action\":\"Map/Forward\",\"path\":\"<Keyboard>/w\"}]");
            PlayerPrefs.Save();

            LegacySettingsMigration.Migrate(store, PropertiesKey, InputSettingsKey);

            Assert.IsTrue(store.TryLoad(LegacySettingsMigration.BindingOverridesKey, out string overrides));
            Assert.AreEqual("[]", overrides);
            Assert.IsTrue(PlayerPrefs.HasKey(InputSettingsKey));
        }

        [Test]
        public void Migrate_ExistingUnifiedProperty_KeepsNewerValue()
        {
            var store = new PlayerPrefsJsonSettingsStore(StoreKey);
            store.Save("guiScale", "18");
            PlayerPrefs.SetString(PropertiesKey, FullLegacyXml);

            LegacySettingsMigration.Migrate(store, PropertiesKey, InputSettingsKey);

            Assert.IsTrue(store.TryLoad("guiScale", out string guiScale));
            Assert.AreEqual("18", guiScale);
            Assert.IsFalse(PlayerPrefs.HasKey(PropertiesKey));
        }

        [TestCase("{{{not xml")]
        [TestCase("<DPSettings />")]
        public void Migrate_UntranslatableProperties_KeepsLegacyData(string xml)
        {
            PlayerPrefs.SetString(PropertiesKey, xml);
            var store = new PlayerPrefsJsonSettingsStore(StoreKey);

            LegacySettingsMigration.Migrate(store, PropertiesKey, InputSettingsKey);

            Assert.IsTrue(PlayerPrefs.HasKey(PropertiesKey));
            Assert.AreEqual(xml, PlayerPrefs.GetString(PropertiesKey));
        }

        [Test]
        public void Migrate_UnconfirmedPropertyWrite_KeepsLegacyData()
        {
            PlayerPrefs.SetString(PropertiesKey, "<DPSettings><GuiScale>12</GuiScale></DPSettings>");

            LegacySettingsMigration.Migrate(new RejectingStore(), PropertiesKey, InputSettingsKey);

            Assert.IsTrue(PlayerPrefs.HasKey(PropertiesKey));
        }

        private sealed class RejectingStore : ISettingsStore
        {
            public bool TryLoad(string key, out string value)
            {
                value = null;
                return false;
            }

            public void Save(string key, string value) { }
        }
    }

    public class InputSettingsTests
    {
        [Test]
        public void Initialize_InvalidBindings_UsesDefaultsAndEnablesInput()
        {
            var input = new DPInput();
            var store = new MemoryStore();
            store.Save(LegacySettingsMigration.BindingOverridesKey, "not json");
            var loggerSource = new RecordingLoggerSource();
            var settings = new InputSettings(input, store, loggerSource);

            Assert.DoesNotThrow(() => ((IInitializable) settings).Initialize());
            Assert.IsTrue(input.UI.enabled);
            Assert.IsNotNull(loggerSource.Logger.LastWarning);

            input.Disable();
            Object.DestroyImmediate(input.asset);
        }

        private sealed class MemoryStore : ISettingsStore
        {
            private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

            public bool TryLoad(string key, out string value)
            {
                return _values.TryGetValue(key, out value);
            }

            public void Save(string key, string value)
            {
                _values[key] = value;
            }
        }

        private sealed class RecordingLoggerSource : ILoggerSource
        {
            public readonly RecordingLogger Logger = new RecordingLogger();

            public ICategoryLogger Create(LogCategory category)
            {
                return Logger;
            }
        }

        private sealed class RecordingLogger : ICategoryLogger
        {
            public string LastWarning { get; private set; }

            public void Message(string message) { }

            public void Warning(string message)
            {
                LastWarning = message;
            }

            public void Error(string message) { }

            public void Exception(System.Exception exception) { }

            public void Write(LogType type, string message) { }
        }
    }

    public class DeedPlannerSettingsTests
    {
        [Test]
        public void CaveOccupiedCellPolicy_MissingKeyUsesPreserveAndHide()
        {
            var store = new MemoryStore();
            store.Save("guiScale", "12");

            DeedPlannerSettings settings = DeedPlannerSettings.Create(new RecordingLogger(), store);

            Assert.AreEqual(Caves.CaveOccupiedCellPolicy.PreserveAndHide,
                settings.Editing.CaveOccupiedCellPolicy);
        }

        [TestCase(Caves.CaveOccupiedCellPolicy.PreserveAndHide)]
        [TestCase(Caves.CaveOccupiedCellPolicy.DeleteCellContent)]
        [TestCase(Caves.CaveOccupiedCellPolicy.PreventSolidifying)]
        public void CaveOccupiedCellPolicy_RoundTrips(Caves.CaveOccupiedCellPolicy policy)
        {
            var store = new MemoryStore();
            DeedPlannerSettings settings = DeedPlannerSettings.Create(new RecordingLogger(), store);

            settings.Editing.CaveOccupiedCellPolicy = policy;
            DeedPlannerSettings reloaded = DeedPlannerSettings.Create(new RecordingLogger(), store);

            Assert.AreEqual(policy, reloaded.Editing.CaveOccupiedCellPolicy);
        }

        [Test]
        public void CaveOccupiedCellPolicy_InvalidValueUsesDefaultAndWarns()
        {
            var store = new MemoryStore();
            store.Save("caveOccupiedCellPolicy", "Unsupported");
            var logger = new RecordingLogger();

            DeedPlannerSettings settings = DeedPlannerSettings.Create(logger, store);

            Assert.AreEqual(Caves.CaveOccupiedCellPolicy.PreserveAndHide,
                settings.Editing.CaveOccupiedCellPolicy);
            StringAssert.Contains("caveOccupiedCellPolicy", logger.LastWarning);
        }

        private sealed class MemoryStore : ISettingsStore
        {
            private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

            public bool TryLoad(string key, out string value)
            {
                return _values.TryGetValue(key, out value);
            }

            public void Save(string key, string value)
            {
                _values[key] = value;
            }
        }

        private sealed class RecordingLogger : ICategoryLogger
        {
            public string LastWarning { get; private set; }

            public void Message(string message) { }

            public void Warning(string message)
            {
                LastWarning = message;
            }

            public void Error(string message) { }

            public void Exception(System.Exception exception) { }

            public void Write(LogType type, string message) { }
        }
    }
}
