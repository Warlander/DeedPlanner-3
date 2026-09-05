using System.Collections.Generic;
using NUnit.Framework;

namespace Warlogic.Settings.Tests.Editor
{
    public class SettingApplyModeTests
    {
        private sealed class FakeStore : ISettingsStore
        {
            public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
            public int SaveCount;

            public bool TryLoad(string key, out string value)
            {
                return Values.TryGetValue(key, out value);
            }

            public void Save(string key, string value)
            {
                Values[key] = value;
                SaveCount++;
            }
        }

        private enum Quality
        {
            Low,
            High
        }

        [Test]
        public void Immediate_SetValue_AppliesAndNotifies()
        {
            var setting = new FloatSetting("s", "S", 1);
            int notifications = 0;
            setting.Changed += () => notifications++;

            setting.Value = 5;

            Assert.AreEqual(5, setting.Value);
            Assert.AreEqual(5, setting.StagedValue);
            Assert.AreEqual(1, notifications);
            Assert.IsFalse(setting.IsDirty);
        }

        [Test]
        public void Immediate_SetSameValue_DoesNotNotify()
        {
            var setting = new FloatSetting("s", "S", 1);
            int notifications = 0;
            setting.Changed += () => notifications++;

            setting.Value = 1;

            Assert.AreEqual(0, notifications);
        }

        [Test]
        public void Immediate_SetStagedValue_WritesThrough()
        {
            var setting = new FloatSetting("s", "S", 1);

            setting.StagedValue = 7;

            Assert.AreEqual(7, setting.Value);
            Assert.IsFalse(setting.IsDirty);
        }

        [Test]
        public void OnSave_SetValue_StagesWithoutApplying()
        {
            var setting = new FloatSetting("s", "S", 1, applyMode: ApplyMode.OnSave);
            int notifications = 0;
            setting.Changed += () => notifications++;

            setting.Value = 5;

            Assert.AreEqual(1, setting.Value);
            Assert.AreEqual(5, setting.StagedValue);
            Assert.IsTrue(setting.IsDirty);
            Assert.AreEqual(0, notifications);
        }

        [Test]
        public void OnSave_Commit_AppliesStagedAndNotifies()
        {
            var setting = new FloatSetting("s", "S", 1, applyMode: ApplyMode.OnSave);
            int notifications = 0;
            setting.Changed += () => notifications++;
            setting.StagedValue = 5;

            setting.Commit();

            Assert.AreEqual(5, setting.Value);
            Assert.IsFalse(setting.IsDirty);
            Assert.AreEqual(1, notifications);
        }

        [Test]
        public void OnSave_Revert_DiscardsStaged()
        {
            var setting = new FloatSetting("s", "S", 1, applyMode: ApplyMode.OnSave);
            setting.StagedValue = 5;

            setting.Revert();

            Assert.AreEqual(1, setting.Value);
            Assert.AreEqual(1, setting.StagedValue);
            Assert.IsFalse(setting.IsDirty);
        }

        [Test]
        public void OnSave_CommitWithoutChanges_DoesNothing()
        {
            var setting = new FloatSetting("s", "S", 1, applyMode: ApplyMode.OnSave);
            int notifications = 0;
            setting.Changed += () => notifications++;

            setting.Commit();

            Assert.AreEqual(1, setting.Value);
            Assert.AreEqual(0, notifications);
        }

        [Test]
        public void Validation_RejectsInvalidValue()
        {
            var setting = new FloatSetting("s", "S", 1) { Validator = v => v >= 0 };

            setting.Value = -3;

            Assert.AreEqual(1, setting.Value);
        }

        [Test]
        public void Validation_RejectsInvalidStagedValue()
        {
            var setting = new FloatSetting("s", "S", 1, applyMode: ApplyMode.OnSave) { Validator = v => v >= 0 };

            setting.StagedValue = -3;

            Assert.AreEqual(1, setting.StagedValue);
            Assert.IsFalse(setting.IsDirty);
        }

        [Test]
        public void MinMax_ClampsValue()
        {
            var setting = new FloatSetting("s", "S", 10, min: 5, max: 20);

            setting.Value = 100;
            Assert.AreEqual(20, setting.Value);

            setting.Value = 0;
            Assert.AreEqual(5, setting.Value);
        }

        [Test]
        public void EnableCondition_ControlsIsEnabled()
        {
            var flag = false;
            var setting = new BoolSetting("s", "S", false) { EnableCondition = () => flag };

            Assert.IsFalse(setting.IsEnabled());
            flag = true;
            Assert.IsTrue(setting.IsEnabled());
        }

        [Test]
        public void Immediate_SetValue_PersistsToStore()
        {
            var store = new FakeStore();
            var registry = new SettingsRegistry(store);
            var setting = new FloatSetting("s", "S", 1);
            registry.AddTab("general", "General").Add(setting);

            setting.Value = 3.5f;

            Assert.AreEqual("3.5", store.Values["s"]);
            Assert.AreEqual(1, store.SaveCount);
        }

        [Test]
        public void OnSave_StoreWrittenOnlyOnCommit()
        {
            var store = new FakeStore();
            var registry = new SettingsRegistry(store);
            var setting = new IntSetting("s", "S", 1, applyMode: ApplyMode.OnSave);
            registry.AddTab("general", "General").Add(setting);

            setting.Value = 9;
            Assert.IsFalse(store.Values.ContainsKey("s"));
            Assert.AreEqual(0, store.SaveCount);

            setting.Commit();
            Assert.AreEqual("9", store.Values["s"]);
            Assert.AreEqual(1, store.SaveCount);
        }

        [Test]
        public void Registration_LoadsPersistedValue()
        {
            var store = new FakeStore();
            store.Values["quality"] = "High";
            var registry = new SettingsRegistry(store);
            var setting = new EnumSetting<Quality>("quality", "Quality", Quality.Low);
            registry.AddTab("graphics", "Graphics").Add(setting);

            Assert.AreEqual(Quality.High, setting.Value);
            Assert.AreEqual(Quality.High, setting.StagedValue);
            Assert.IsFalse(setting.IsDirty);
        }

        [Test]
        public void Registration_IgnoresCorruptPersistedValue()
        {
            var store = new FakeStore();
            store.Values["s"] = "not-a-number";
            var registry = new SettingsRegistry(store);
            var setting = new FloatSetting("s", "S", 4);
            registry.AddTab("general", "General").Add(setting);

            Assert.AreEqual(4, setting.Value);
        }

        [Test]
        public void Registration_IgnoresPersistedValueFailingValidation()
        {
            var store = new FakeStore();
            store.Values["s"] = "-10";
            var registry = new SettingsRegistry(store);
            var setting = new FloatSetting("s", "S", 4) { Validator = v => v >= 0 };
            registry.AddTab("general", "General").Add(setting);

            Assert.AreEqual(4, setting.Value);
        }
    }
}
