using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Editing;
using Warlander.Deedplanner.Persistence;
using System.Reflection;
using System.Xml;
using System.Runtime.Serialization;
using NUnit.Framework;

namespace Warlander.Deedplanner.Bridges.Tests
{
    public class BridgeShapeChangeTests
    {
        [TestCase(false, 1)]
        [TestCase(true, 1)]
        [TestCase(false, 3)]
        [TestCase(true, 3)]
        public void OneTileBridge_RejectsArchWithoutMutatingState(bool vertical, int width)
        {
            Bridge bridge = CreateBridge(BridgeType.Flat, 1, width, vertical);
            BridgeShapeState before = bridge.CaptureShapeState();

            Assert.That(bridge.TryPrepareShapeChange(BridgeType.Arched, out _, out string error), Is.False);

            Assert.That(error, Is.EqualTo("Arched bridges need at least 2 tiles."));
            Assert.That(bridge.Type, Is.EqualTo(before.Type));
            Assert.That(bridge.GetSegmentsString(), Is.EqualTo(before.Segments));
            Assert.That(bridge.AdditionalData, Is.EqualTo(before.AdditionalData));
            Assert.That(bridge.CaptureShapeState().CopyFlatSupports(), Is.Null);
        }

        [TestCase(false, 2, 3)]
        [TestCase(true, 2, 3)]
        [TestCase(false, 38, 1)]
        [TestCase(true, 38, 3)]
        public void Conversion_UsesStoredOrientationAndLength(bool vertical, int length, int width)
        {
            Bridge bridge = CreateBridge(BridgeType.Flat, length, width, vertical);

            Assert.That(bridge.TryPrepareShapeChange(BridgeType.Arched, out BridgeShapeState state, out _), Is.True);

            Assert.That(bridge.Width, Is.EqualTo(width));
            Assert.That(state.Type, Is.EqualTo(BridgeType.Arched));
            Assert.That(state.Segments.Length, Is.EqualTo(length));
            Assert.That(state.AdditionalData, Is.EqualTo(5));
        }

        [Test]
        public void RememberedFlatSupports_AreRestoredForCurrentMaterial()
        {
            Bridge bridge = CreateBridge(BridgeType.Arched, 9, 2, false, "wood");
            bool[] supports = new bool[9];
            supports[2] = true;
            supports[6] = true;
            SetField(bridge, "_rememberedFlatSupports", supports);

            Assert.That(bridge.TryPrepareShapeChange(BridgeType.Flat, out BridgeShapeState state, out _), Is.True);

            Assert.That(BridgePartTypeUtils.DecodeSegments(state.Segments)
                .Select(part => part == BridgePartType.Support).ToArray(), Is.EqualTo(supports));
            Assert.That(state.Segments, Does.Not.Contain("b").And.Not.Contain("d").And.Not.Contain("e"));
            Assert.That(state.AdditionalData, Is.Zero);
        }

        [TestCase(5)]
        [TestCase(10)]
        [TestCase(15)]
        [TestCase(20)]
        public void RememberedArchSteepness_IsRestored(int steepness)
        {
            Bridge bridge = CreateBridge(BridgeType.Flat, 9, 2, false);
            SetField(bridge, "_rememberedArchSteepness", steepness);

            Assert.That(bridge.TryPrepareShapeChange(BridgeType.Arched, out BridgeShapeState state, out _), Is.True);

            Assert.That(state.AdditionalData, Is.EqualTo(steepness));
            Assert.That(bridge.Type, Is.EqualTo(BridgeType.Flat));
        }

        [Test]
        public void LeavingArch_RemembersItsCurrentSteepness()
        {
            Bridge bridge = CreateBridge(BridgeType.Arched, 9, 2, false);
            SetField(bridge, "additionalData", 20);
            SetField(bridge, "_rememberedArchSteepness", 5);

            Assert.That(bridge.TryPrepareShapeChange(BridgeType.Flat, out BridgeShapeState state, out _), Is.True);

            Assert.That(state.RememberedArchSteepness, Is.EqualTo(20));
        }

        [Test]
        public void InvalidRememberedSupports_RejectWithoutOverwritingMemory()
        {
            Bridge bridge = CreateBridge(BridgeType.Arched, 9, 2, false);
            bool[] supports = new bool[9];
            SetField(bridge, "_rememberedFlatSupports", supports);

            Assert.That(bridge.TryPrepareShapeChange(BridgeType.Flat, out _, out string error), Is.False);

            Assert.That(error, Does.Contain("support layout"));
            Assert.That(bridge.Type, Is.EqualTo(BridgeType.Arched));
            Assert.That(bridge.CaptureShapeState().CopyFlatSupports(), Is.EqualTo(supports));
        }

        [Test]
        public void ShapeState_OwnsIndependentSupportSnapshots()
        {
            bool[] supports = { false, true, false };
            var state = new BridgeShapeState(BridgeType.Flat, "asa", 0, supports, 15);
            supports[1] = false;
            bool[] copy = state.CopyFlatSupports();
            copy[1] = false;

            Assert.That(state.CopyFlatSupports()[1], Is.True);
        }

        [Test]
        public void CurrentFlatEdits_ReplaceEarlierRememberedSupportsInPreparedState()
        {
            Bridge bridge = CreateBridge(BridgeType.Flat, 9, 2, false);
            SetField(bridge, "_rememberedFlatSupports", new bool[9]);

            Assert.That(bridge.TryPrepareShapeChange(BridgeType.Arched, out BridgeShapeState state, out _), Is.True);

            Assert.That(state.CopyFlatSupports(), Is.EqualTo(bridge.GetSupportPositions()));
            Assert.That(bridge.CaptureShapeState().CopyFlatSupports(), Is.EqualTo(new bool[9]));
        }

        [Test]
        public void MaterialCapabilities_RejectUnsupportedShape()
        {
            Bridge bridge = CreateBridge(BridgeType.Flat, 9, 2, false);
            SetData(bridge, new BridgeData("custom", 3, 0, null,
                new[] { BridgeType.Flat }, null, false));

            Assert.That(bridge.TryPrepareShapeChange(BridgeType.Arched, out _, out string error), Is.False);
            Assert.That(error, Does.Contain("material"));
        }

        [Test]
        public void MaterialCapabilities_RejectUnsupportedWidth()
        {
            Bridge bridge = CreateBridge(BridgeType.Flat, 9, 3, false, "wood");

            Assert.That(bridge.TryPrepareShapeChange(BridgeType.Arched, out _, out string error), Is.False);
            Assert.That(error, Does.Contain("width"));
        }

        [Test]
        public void RopeBridge_CannotConvert()
        {
            Bridge bridge = CreateBridge(BridgeType.Rope, 9, 1, false);
            Assert.That(bridge.TryPrepareShapeChange(BridgeType.Flat, out _, out string error), Is.False);
            Assert.That(error, Does.Contain("Rope"));
        }

        [Test]
        public void ExcessiveLength_CannotConvert()
        {
            Bridge bridge = CreateBridge(BridgeType.Flat, 38, 1, false);
            SetField(bridge, "secondX", 4 + 39 - 1);
            Assert.That(bridge.TryPrepareShapeChange(BridgeType.Arched, out _, out string error), Is.False);
            Assert.That(error, Does.Contain("length"));
        }

        [Test]
        public void MirroredConversion_WithUnsupportedMaterial_RejectsAllTargetsWithoutHistoryChanges()
        {
            var objects = new List<GameObject>();
            try
            {
                var mapObject = new GameObject("Bridge shape test map");
                objects.Add(mapObject);
                Map map = mapObject.AddComponent<Map>();
                SetField(map, "_mapRenderSettingsRetriever", new Warlander.Deedplanner.Settings.MapRenderSettings());
                var registry = new MapRegistry();
                registry.SetMap(map);
                var handler = (MapHandler)FormatterServices.GetUninitializedObject(typeof(MapHandler));
                SetField(handler, "_registry", registry);
                var bridges = new MapBridgesController(map, null);
                SetField(map, "_bridgesController", bridges);
                var symmetry = new SymmetrySession(handler);
                symmetry.PlaceVerticalAxis(35);
                var facade = new MapEditFacade(handler, symmetry, null, null, null);
                Bridge original = CreateBridge(BridgeType.Flat, 9, 2, false);
                Bridge mirrored = CreateBridge(BridgeType.Flat, 9, 2, false);
                SetField(mirrored, "firstX", 22);
                SetField(mirrored, "secondX", 30);
                SetData(mirrored, new BridgeData("flat only", 3, 0, CreateParts("stone"),
                    new[] { BridgeType.Flat }, null, false));
                AddParts(original, 4);
                AddParts(mirrored, 22);
                bridges.AddBridge(original);
                bridges.AddBridge(mirrored);
                int mutations = 0;
                map.CommandManager.Mutated += () => mutations++;

                Assert.That(facade.TryChangeBridgeShape(original, BridgeType.Arched, out string error), Is.False);

                Assert.That(error, Does.Contain("mirrored"));
                Assert.That(original.Type, Is.EqualTo(BridgeType.Flat));
                Assert.That(mirrored.Type, Is.EqualTo(BridgeType.Flat));
                Assert.That(original.CaptureShapeState().CopyFlatSupports(), Is.Null);
                Assert.That(mutations, Is.Zero);
                Assert.That(facade.TryChangeBridgeShape(original, BridgeType.Flat, out _), Is.True);
                Assert.That(mutations, Is.Zero);

                void AddParts(Bridge bridge, int startX)
                {
                    var parts = new List<BridgePart>();
                    for (int segment = 0; segment < 9; segment++)
                    {
                        for (int lane = 0; lane < 2; lane++)
                        {
                            var gameObject = new GameObject("Bridge test part");
                            objects.Add(gameObject);
                            BridgePart part = gameObject.AddComponent<BridgePart>();
                            var tile = (Tile)FormatterServices.GetUninitializedObject(typeof(Tile));
                            SetField(tile, "<X>k__BackingField", startX + segment);
                            SetField(tile, "<Y>k__BackingField", 7 + lane);
                            part.Tile = tile;
                            parts.Add(part);
                        }
                    }
                    SetField(bridge, "bridgeParts", parts);
                }
            }
            finally
            {
                foreach (GameObject gameObject in objects) UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static Bridge CreateBridge(BridgeType type, int length, int width, bool vertical,
            string materialName = "stone")
        {
            var bridge = (Bridge)FormatterServices.GetUninitializedObject(typeof(Bridge));
            var data = new BridgeData(materialName, materialName == "wood" ? 2 : 3, 0, CreateParts(materialName),
                new[] { BridgeType.Flat, BridgeType.Arched, BridgeType.Rope }, null, false);
            SetData(bridge, data);
            SetField(bridge, "bridgeType", type);
            SetField(bridge, "verticalOrientation", vertical);
            SetField(bridge, "firstX", 4);
            SetField(bridge, "firstY", 7);
            SetField(bridge, "secondX", 4 + (vertical ? width : length) - 1);
            SetField(bridge, "secondY", 7 + (vertical ? length : width) - 1);
            SetField(bridge, "additionalData", type == BridgeType.Arched ? 5 : 0);
            SetField(bridge, "segments", BridgePartTypeUtils.DecodeSegments(
                BridgeDefaults.GetDefaultSegments(type, data, length)));
            return bridge;
        }

        private static BridgePartData[] CreateParts(string materialName)
        {
            return Enum.GetValues(typeof(BridgePartType)).Cast<BridgePartType>()
                .Where(type => materialName != "wood" || (type != BridgePartType.Bracing
                    && type != BridgePartType.DoubleBracing && type != BridgePartType.DoubleAbutment))
                .Select(type =>
                {
                    var document = new XmlDocument();
                    document.LoadXml($"<part type='{type}'/>");
                    return new BridgePartData(null, document.DocumentElement);
                }).ToArray();
        }

        private static void SetData(Bridge bridge, BridgeData data)
        {
            typeof(Bridge).GetProperty("Data").SetValue(bridge, data);
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
