using NUnit.Framework;
using UnityEngine;

namespace Warlogic.Settings.Tests.Editor
{
    public class PlayerPrefsJsonSettingsStoreTests
    {
        private const string TestKey = "warlogic.settings.tests";

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(TestKey);
        }

        [Test]
        public void Save_ThenNewInstance_RoundTrips()
        {
            var store = new PlayerPrefsJsonSettingsStore(TestKey);
            store.Save("guiScale", "12.5");
            store.Save("invertMouse", "true");

            var reloaded = new PlayerPrefsJsonSettingsStore(TestKey);

            Assert.IsTrue(reloaded.TryLoad("guiScale", out string scale));
            Assert.AreEqual("12.5", scale);
            Assert.IsTrue(reloaded.TryLoad("invertMouse", out string invert));
            Assert.AreEqual("true", invert);
            Assert.IsFalse(reloaded.TryLoad("missing", out _));
        }

        [Test]
        public void Save_OverwritesExistingKey()
        {
            var store = new PlayerPrefsJsonSettingsStore(TestKey);
            store.Save("s", "one");
            store.Save("s", "two");

            var reloaded = new PlayerPrefsJsonSettingsStore(TestKey);

            Assert.AreEqual("two", reloaded.TryLoad("s", out string value) ? value : null);
        }

        [Test]
        public void CorruptDocument_LoadsEmpty()
        {
            PlayerPrefs.SetString(TestKey, "{{{not json");
            PlayerPrefs.Save();

            var store = new PlayerPrefsJsonSettingsStore(TestKey);

            Assert.IsFalse(store.TryLoad("anything", out _));
        }
    }
}
