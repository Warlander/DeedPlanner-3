using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Warlogic.Settings.Tests.Editor
{
    public class SettingsRegistryTests
    {
        [Test]
        public void Tabs_SortedBySortOrder()
        {
            var registry = new SettingsRegistry();
            registry.AddTab("b", "B", 2);
            registry.AddTab("a", "A", 1);
            registry.AddTab("c", "C", 3);

            Assert.AreEqual(new[] { "a", "b", "c" }, registry.Tabs.Select(t => t.Id).ToArray());
        }

        [Test]
        public void TabSettings_KeepInsertionOrder()
        {
            var registry = new SettingsRegistry();
            SettingsTab tab = registry.AddTab("general", "General");
            tab.Add(new FloatSetting("z", "Z", 0));
            tab.Add(new FloatSetting("a", "A", 0));

            Assert.AreEqual(new[] { "z", "a" }, tab.Settings.Select(s => s.Key).ToArray());
        }

        [Test]
        public void AddTab_DuplicateId_Throws()
        {
            var registry = new SettingsRegistry();
            registry.AddTab("general", "General");

            Assert.Throws<ArgumentException>(() => registry.AddTab("general", "Again"));
        }

        [Test]
        public void Add_DuplicateKeyAcrossTabs_Throws()
        {
            var registry = new SettingsRegistry();
            registry.AddTab("a", "A").Add(new FloatSetting("shared", "First", 0));
            SettingsTab second = registry.AddTab("b", "B");

            Assert.Throws<ArgumentException>(() => second.Add(new FloatSetting("shared", "Second", 0)));
        }

        [Test]
        public void GetSetting_ReturnsRegisteredSetting()
        {
            var registry = new SettingsRegistry();
            var setting = new FloatSetting("guiScale", "GUI Scale", 10);
            registry.AddTab("general", "General").Add(setting);

            Assert.AreSame(setting, registry.GetSetting("guiScale"));
            Assert.AreSame(setting, registry.GetSetting<float>("guiScale"));
            Assert.IsNull(registry.GetSetting("missing"));
        }

        [Test]
        public void SettingChanged_ForwardsSettingChange()
        {
            var registry = new SettingsRegistry();
            var setting = new FloatSetting("guiScale", "GUI Scale", 10);
            registry.AddTab("general", "General").Add(setting);
            ISetting notified = null;
            registry.SettingChanged += s => notified = s;

            setting.Value = 12;

            Assert.AreSame(setting, notified);
        }
    }
}
