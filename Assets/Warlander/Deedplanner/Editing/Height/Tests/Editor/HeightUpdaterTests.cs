using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Warlander.Deedplanner.Editing.Tests
{
    public class HeightUpdaterTests
    {
        [Test]
        public void UpdateHoveredHandlesSimpleSelection_MissedRaycastReturnsNoHandles()
        {
            var updater = new HeightUpdater(null, null, null, null, null, null, null, null);
            MethodInfo method = typeof(HeightUpdater).GetMethod(
                "UpdateHoveredHandlesSimpleSelection", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);

            var handles = (List<HeightmapHandle>)method.Invoke(updater, new object[] { default(RaycastHit) });

            Assert.That(handles, Is.Empty);
        }
    }
}
