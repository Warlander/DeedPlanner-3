using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Warlander.Deedplanner.Editing.Tests
{
    public class HeightSelectionTests
    {
        [TestCase(0, 0)]
        [TestCase(-4, 0)]
        [TestCase(0, -4)]
        [TestCase(400, 0)]
        [TestCase(0, 400)]
        public void MissedRayDoesNotSelectHeightHandles(float x, float z)
        {
            var updater = new HeightUpdater(null, null, null, null, null, null, null, null);
            var raycast = new RaycastHit { point = new Vector3(x, 0, z) };
            MethodInfo select = typeof(HeightUpdater).GetMethod("UpdateHoveredHandlesSimpleSelection",
                BindingFlags.Instance | BindingFlags.NonPublic);

            var handles = (IList)select.Invoke(updater, new object[] { raycast });

            Assert.That(handles, Is.Empty);
        }
    }
}
