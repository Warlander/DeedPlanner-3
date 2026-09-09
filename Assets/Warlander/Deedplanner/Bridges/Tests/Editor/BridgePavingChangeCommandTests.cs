using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using UnityEngine;

namespace Warlander.Deedplanner.Bridges.Tests
{
    public class BridgePavingChangeCommandTests
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
        public void Undo_AfterPartsReplaced_UpdatesCurrentPart()
        {
            var bridge = (Bridge)FormatterServices.GetUninitializedObject(typeof(Bridge));
            BridgePart originalPart = CreatePart(bridge, 2, 1);
            BridgePart replacementPart = CreatePart(bridge, 2, 1);
            var pavement = new BridgePavementData("Stone", "stone", null, null);
            SetProperty(replacementPart, "Pavement", pavement);

            var command = new BridgePavingChangeCommand(
                new[] { originalPart }, new BridgePavementData[] { null }, new[] { pavement });
            SetField(bridge, "bridgeParts", new List<BridgePart> { replacementPart });

            command.Undo();

            Assert.That(replacementPart.Pavement, Is.Null);
        }

        private BridgePart CreatePart(Bridge bridge, int segmentIndex, int laneIndex)
        {
            var gameObject = new GameObject("Bridge Part");
            _objects.Add(gameObject);
            BridgePart part = gameObject.AddComponent<BridgePart>();
            SetProperty(part, "ParentBridge", bridge);
            SetProperty(part, "SegmentIndex", segmentIndex);
            SetProperty(part, "LaneIndex", laneIndex);
            return part;
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
    }
}
