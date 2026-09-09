using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Warlander.Deedplanner.Domain.Entities.Floors;

namespace Warlander.Deedplanner.Editing.Tests
{
    public class WallUpdaterTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject gameObject in _objects)
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ShouldReverseAutomatically_MissingHorizontalNeighborUsesNoFloor()
        {
            Assert.That(ShouldReverseAutomatically(CreateFloor(), null, true), Is.True);
        }

        [Test]
        public void ShouldReverseAutomatically_MissingVerticalNeighborUsesNoFloor()
        {
            Assert.That(ShouldReverseAutomatically(null, null, false), Is.False);
        }

        [Test]
        public void GetFloor_MissingBoundaryTileReturnsNull()
        {
            MethodInfo method = typeof(WallUpdater).GetMethod(
                "GetFloor", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);

            var floor = (Floor)method.Invoke(null, new object[] { null, 0 });

            Assert.That(floor, Is.Null);
        }

        private Floor CreateFloor()
        {
            var gameObject = new GameObject("Floor");
            _objects.Add(gameObject);
            return gameObject.AddComponent<Floor>();
        }

        private static bool ShouldReverseAutomatically(Floor currentFloor, Floor nearFloor, bool horizontal)
        {
            MethodInfo method = typeof(WallUpdater).GetMethod(
                "ShouldReverseAutomatically", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (bool)method.Invoke(null, new object[] { currentFloor, nearFloor, horizontal });
        }
    }
}
