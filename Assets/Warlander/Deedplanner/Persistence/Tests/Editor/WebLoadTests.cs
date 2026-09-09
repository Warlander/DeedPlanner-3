using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Logging;

namespace Warlander.Deedplanner.Persistence.Tests
{
    public class WebLoadTests
    {
        private readonly List<GameObject> _mapObjects = new List<GameObject>();
        private MapRegistry _registry;

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject mapObject in _mapObjects)
            {
                if (mapObject)
                {
                    UnityEngine.Object.DestroyImmediate(mapObject);
                }
            }
        }

        [Test]
        public async Task LoadFromWebAsync_FailedDownloadPreservesSaveIdentity()
        {
            var pendingLoad = new TaskCompletionSource<Map>(TaskCreationOptions.RunContinuationsAsynchronously);
            SaveCoordinator coordinator = CreateCoordinator(pendingLoad.Task);
            var originalLocation = new MapLocation(SaveBackendId.File, "deed.MAP", "Deed");
            DateTime originalSaveTime = DateTime.UtcNow.AddMinutes(-1);
            SetProperty(coordinator, "CurrentLocation", originalLocation);
            SetProperty(coordinator, "LastSaveTimeUtc", originalSaveTime);
            int stateChanges = 0;
            coordinator.SaveStateChanged += () => stateChanges++;

            Task<bool> load = coordinator.LoadFromWebAsync("https://example.com/deed.map");
            Assert.That(coordinator.Busy, Is.True);
            Assert.That(await coordinator.LoadFromWebAsync("https://example.com/other.map"), Is.False);
            pendingLoad.SetResult(null);

            Assert.That(await load, Is.False);
            Assert.That(coordinator.Busy, Is.False);
            Assert.That(coordinator.CurrentLocation.Value.Locator, Is.EqualTo(originalLocation.Locator));
            Assert.That(coordinator.LastSaveTimeUtc, Is.EqualTo(originalSaveTime));
            Assert.That(stateChanges, Is.Zero);
        }

        [Test]
        public async Task LoadFromWebAsync_SupersededDownloadPreservesReplacementIdentity()
        {
            var pendingLoad = new TaskCompletionSource<Map>(TaskCreationOptions.RunContinuationsAsynchronously);
            SaveCoordinator coordinator = CreateCoordinator(pendingLoad.Task);
            Task<bool> load = coordinator.LoadFromWebAsync("https://example.com/deed.map");

            Map replacementMap = CreateMap("Replacement Map");
            _registry.SetMap(replacementMap);
            var replacementLocation = new MapLocation(SaveBackendId.File, "replacement.MAP", "Replacement");
            SetProperty(coordinator, "CurrentLocation", replacementLocation);
            int stateChanges = 0;
            coordinator.SaveStateChanged += () => stateChanges++;
            LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
            pendingLoad.SetResult(CreateMap("Downloaded Map"));

            Assert.That(await load, Is.False);
            Assert.That(_registry.CurrentMap, Is.SameAs(replacementMap));
            Assert.That(coordinator.CurrentLocation.Value.Locator, Is.EqualTo(replacementLocation.Locator));
            Assert.That(stateChanges, Is.Zero);
        }

        private SaveCoordinator CreateCoordinator(Task<Map> loadResult)
        {
            _registry = new MapRegistry();
            _registry.SetMap(CreateMap("Current Map"));

            var mapHandler = (MapHandler)FormatterServices.GetUninitializedObject(typeof(MapHandler));
            SetField(mapHandler, "_registry", _registry);
            SetField(mapHandler, "_loader", new MapLoader(null, null, new LoggerSource()));
            SetField(mapHandler, "_loadMapAsync", new Func<Uri, Task<Map>>(_ => loadResult));

            var coordinator = new SaveCoordinator(
                mapHandler, null, Array.Empty<ISaveBackend>(), null, null);
            SetField(coordinator, "_autoSaveScheduler", new StubAutoSaveScheduler());
            return coordinator;
        }

        private Map CreateMap(string name)
        {
            var gameObject = new GameObject(name);
            _mapObjects.Add(gameObject);
            return gameObject.AddComponent<Map>();
        }

        private static void SetProperty(object target, string propertyName, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null);
            property.SetValue(target, value);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private class StubAutoSaveScheduler : IAutoSaveScheduler
        {
            public Task AutoSaveNowAsync() => Task.CompletedTask;

            public Task<SavedMapInfo?> FindRecoverySlotAsync(MapLocation mainLocation) =>
                Task.FromResult<SavedMapInfo?>(null);

            public Task<SavedMapInfo?> FindNeverSavedRecoveryAsync() => Task.FromResult<SavedMapInfo?>(null);

            public Task DeleteSlotsAsync(MapLocation mainLocation) => Task.CompletedTask;
        }
    }
}
