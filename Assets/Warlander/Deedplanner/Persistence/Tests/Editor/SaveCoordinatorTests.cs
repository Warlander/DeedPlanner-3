using System.Reflection;
using NUnit.Framework;

namespace Warlander.Deedplanner.Persistence.Tests
{
    public class SaveCoordinatorTests
    {
        private static readonly MethodInfo SelectCurrentLocationAfterSave = typeof(SaveCoordinator).GetMethod(
            "SelectCurrentLocationAfterSave", BindingFlags.NonPublic | BindingFlags.Static);

        [Test]
        public void SelectCurrentLocationAfterSave_ExportPreservesWritableLocation()
        {
            var current = new MapLocation(SaveBackendId.File, "deed.MAP", "Deed");
            var exported = new MapLocation(SaveBackendId.Pastebin, "https://pastebin.com/raw/key", "Deed");

            MapLocation? result = InvokeSelection(current, exported, SaveCapabilities.Save);

            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Value.BackendId, Is.EqualTo(current.BackendId));
            Assert.That(result.Value.Locator, Is.EqualTo(current.Locator));
        }

        [Test]
        public void SelectCurrentLocationAfterSave_ExportPreservesNeverSavedIdentity()
        {
            var exported = new MapLocation(SaveBackendId.WebFile, "Untitled.MAP", "Untitled");

            MapLocation? result = InvokeSelection(null, exported, SaveCapabilities.Save);

            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void SelectCurrentLocationAfterSave_WritableSaveReplacesLocation()
        {
            var current = new MapLocation(SaveBackendId.File, "old.MAP", "Old");
            var saved = new MapLocation(SaveBackendId.File, "new.MAP", "New");

            MapLocation? result = InvokeSelection(
                current, saved, SaveCapabilities.Save | SaveCapabilities.Overwrite);

            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Value.BackendId, Is.EqualTo(saved.BackendId));
            Assert.That(result.Value.Locator, Is.EqualTo(saved.Locator));
        }

        private static MapLocation? InvokeSelection(
            MapLocation? currentLocation, MapLocation savedLocation, SaveCapabilities capabilities)
        {
            Assert.That(SelectCurrentLocationAfterSave, Is.Not.Null);
            object result = SelectCurrentLocationAfterSave.Invoke(
                null, new object[] { currentLocation, savedLocation, capabilities });
            return result == null ? (MapLocation?)null : (MapLocation)result;
        }
    }
}
