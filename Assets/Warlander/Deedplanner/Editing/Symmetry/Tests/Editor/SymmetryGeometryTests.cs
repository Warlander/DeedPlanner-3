using NUnit.Framework;
using UnityEngine;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Domain.Entities.Grounds;
using System.Collections.Generic;
using System.Reflection;
using Warlander.Deedplanner.Logging;

namespace Warlander.Deedplanner.Editing.Tests
{
    public class SymmetryGeometryTests
    {
        [TestCase(0f, 0)]
        [TestCase(1f, 0)]
        [TestCase(2f, 1)]
        [TestCase(3f, 2)]
        [TestCase(4f, 2)]
        [TestCase(6f, 3)]
        public void SnapAxisCoordinate2_ChoosesNearestAndBreaksTiesTowardGridLines(float world, int expected)
        {
            Assert.That(SymmetryGeometry.SnapAxisCoordinate2(world), Is.EqualTo(expected));
        }

        [Test]
        public void Snapshot_ReflectsCellsAndVerticesAroundSameAxis()
        {
            var snapshot = new SymmetrySnapshot(true, 6, true, 9);

            Assert.That(snapshot.ReflectCellX(1), Is.EqualTo(4));
            Assert.That(snapshot.ReflectVertexX(1), Is.EqualTo(5));
            Assert.That(snapshot.ReflectCellY(2), Is.EqualTo(6));
            Assert.That(snapshot.ReflectVertexY(2), Is.EqualTo(7));
        }

        [TestCase(RoadDirection.NW, SymmetryTransform.ReflectX, RoadDirection.NE)]
        [TestCase(RoadDirection.NW, SymmetryTransform.ReflectY, RoadDirection.SW)]
        [TestCase(RoadDirection.NW, SymmetryTransform.ReflectX | SymmetryTransform.ReflectY, RoadDirection.SE)]
        [TestCase(RoadDirection.Center, SymmetryTransform.ReflectX | SymmetryTransform.ReflectY, RoadDirection.Center)]
        public void RoadDirection_TransformsWithGeometry(RoadDirection input, SymmetryTransform transform,
            RoadDirection expected)
        {
            Assert.That(transform.Transform(input), Is.EqualTo(expected));
        }

        [TestCase(EntityOrientation.Left, SymmetryTransform.ReflectX, EntityOrientation.Right)]
        [TestCase(EntityOrientation.Up, SymmetryTransform.ReflectY, EntityOrientation.Down)]
        [TestCase(EntityOrientation.Left, SymmetryTransform.ReflectY, EntityOrientation.Left)]
        public void FloorOrientation_TransformsWithGeometry(EntityOrientation input, SymmetryTransform transform,
            EntityOrientation expected)
        {
            Assert.That(transform.Transform(input), Is.EqualTo(expected));
        }

        [TestCase(0.25f, SymmetryTransform.ReflectX, 6.0331855f)]
        [TestCase(0.25f, SymmetryTransform.ReflectY, 2.8915927f)]
        [TestCase(0.25f, SymmetryTransform.ReflectX | SymmetryTransform.ReflectY, 3.3915927f)]
        public void DecorationRotation_TransformsWithGeometry(float input, SymmetryTransform transform,
            float expected)
        {
            Assert.That(transform.TransformRotation(input), Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void ApplyingSameReflectionTwice_RestoresOriginalValues()
        {
            var snapshot = new SymmetrySnapshot(true, 7, true, 8);

            Assert.That(snapshot.ReflectCellX(snapshot.ReflectCellX(2)), Is.EqualTo(2));
            Assert.That(snapshot.ReflectVertexY(snapshot.ReflectVertexY(3)), Is.EqualTo(3));
            Assert.That(SymmetryTransform.ReflectX.Transform(
                SymmetryTransform.ReflectX.Transform(RoadDirection.SW)), Is.EqualTo(RoadDirection.SW));
            Assert.That(SymmetryTransform.ReflectY.Transform(
                SymmetryTransform.ReflectY.Transform(EntityOrientation.Up)), Is.EqualTo(EntityOrientation.Up));
        }

        [Test]
        public void HeightPatch_IdentityWinsWhenSelectedVerticesReflectOntoEachOther()
        {
            var patch = new SymmetryHeightPatch();
            var snapshot = new SymmetrySnapshot(true, 4, false, 0);
            var values = new Dictionary<int, int>();

            patch.Add(snapshot, 6, 6, 1, 2, 15);
            patch.Add(snapshot, 6, 6, 3, 2, 25);
            patch.Apply((x, y, value) => values[x] = value);

            Assert.That(values[1], Is.EqualTo(15));
            Assert.That(values[3], Is.EqualTo(25));
        }

        [Test]
        public void HeightPatch_ClipsTargetsOutsideMap()
        {
            var patch = new SymmetryHeightPatch();
            var snapshot = new SymmetrySnapshot(true, 20, false, 0);
            var values = new Dictionary<int, int>();

            patch.Add(snapshot, 6, 6, 2, 2, 10);
            patch.Apply((x, y, value) => values[x] = value);

            Assert.That(values, Has.Count.EqualTo(1));
            Assert.That(values[2], Is.EqualTo(10));
        }

        [Test]
        public void CellTargets_ExpandAcrossBothAxesWithTransforms()
        {
            Map map = CreateMapBounds(8, 8);
            var targets = new List<SymmetryCellTarget>();
            var snapshot = new SymmetrySnapshot(true, 6, true, 8);

            InvokeForEachCell(map, snapshot, 1, 2, targets.Add);

            Assert.That(targets, Has.Count.EqualTo(4));
            AssertTarget(targets[0], 1, 2, SymmetryTransform.Identity);
            AssertTarget(targets[1], 4, 2, SymmetryTransform.ReflectX);
            AssertTarget(targets[2], 1, 5, SymmetryTransform.ReflectY);
            AssertTarget(targets[3], 4, 5, SymmetryTransform.ReflectX | SymmetryTransform.ReflectY);
            Object.DestroyImmediate(map.gameObject);
        }

        [Test]
        public void CellTargets_DeduplicateTargetsOnBothAxes()
        {
            Map map = CreateMapBounds(4, 4);
            var targets = new List<SymmetryCellTarget>();
            var snapshot = new SymmetrySnapshot(true, 3, true, 3);

            InvokeForEachCell(map, snapshot, 1, 1, targets.Add);

            Assert.That(targets, Has.Count.EqualTo(1));
            AssertTarget(targets[0], 1, 1, SymmetryTransform.Identity);
            Object.DestroyImmediate(map.gameObject);
        }

        [Test]
        public void CellTargets_KeepIdentityAndClipOutsideReflections()
        {
            Map map = CreateMapBounds(4, 4);
            var targets = new List<SymmetryCellTarget>();
            var snapshot = new SymmetrySnapshot(true, 20, true, 20);

            InvokeForEachCell(map, snapshot, 1, 1, targets.Add);

            Assert.That(targets, Has.Count.EqualTo(1));
            AssertTarget(targets[0], 1, 1, SymmetryTransform.Identity);
            Object.DestroyImmediate(map.gameObject);
        }

        [Test]
        public void VertexTargets_ReflectAroundVertexCoordinates()
        {
            Map map = CreateMapBounds(8, 8);
            var targets = new List<SymmetryVertexTarget>();
            var snapshot = new SymmetrySnapshot(true, 6, true, 8);

            InvokeForEachVertex(map, snapshot, 1, 2, targets.Add);

            Assert.That(targets, Has.Count.EqualTo(4));
            AssertTarget(targets[0], 1, 2, SymmetryTransform.Identity);
            AssertTarget(targets[1], 5, 2, SymmetryTransform.ReflectX);
            AssertTarget(targets[2], 1, 6, SymmetryTransform.ReflectY);
            AssertTarget(targets[3], 5, 6, SymmetryTransform.ReflectX | SymmetryTransform.ReflectY);
            Object.DestroyImmediate(map.gameObject);
        }

        [Test]
        public void MirroredAction_UndoesAndRedoesEveryTargetTogether()
        {
            var values = new int[4];
            var history = new CommandManager(10);
            for (int i = 0; i < values.Length; i++)
            {
                history.AddToActionAndExecute(new SetArrayValue(values, i, 1));
            }
            history.FinishAction();

            history.Undo();
            Assert.That(values, Is.EqualTo(new[] { 0, 0, 0, 0 }));

            history.Redo();
            Assert.That(values, Is.EqualTo(new[] { 1, 1, 1, 1 }));
        }

        [Test]
        public void CompletePlacement_RejectedDropClearsPlacementState()
        {
            var updater = new DecorationUpdater(null, null, null, null, null, null, null, null,
                new LoggerSource(), null, null, null);
            var ghost = new GameObject("Decoration ghost");
            ghost.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            SetField(updater, "_ghostObject", ghost);
            SetField(updater, "_placingDecoration", true);
            SetField(updater, "_isScrollRotate", true);
            SetField(updater, "_rotation", 45f);

            MethodInfo method = typeof(DecorationUpdater).GetMethod("CompletePlacement",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(updater, new object[] { false, null, 0 });

            Assert.That(GetField<bool>(updater, "_placingDecoration"), Is.False);
            Assert.That(GetField<bool>(updater, "_isScrollRotate"), Is.False);
            Assert.That(GetField<float>(updater, "_rotation"), Is.Zero);
            Assert.That(ghost.transform.localRotation, Is.EqualTo(Quaternion.identity));
            Object.DestroyImmediate(ghost);
        }

        private static Map CreateMapBounds(int width, int height)
        {
            Map map = new GameObject("Symmetry bounds").AddComponent<Map>();
            SetField(map, "_tileGrid", new MapTileGrid(width, height));
            return map;
        }

        private static void InvokeForEachCell(Map map, SymmetrySnapshot snapshot, int x, int y,
            System.Action<SymmetryCellTarget> action)
        {
            MethodInfo method = typeof(MapEditFacade).GetMethod("ForEachCell",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(null, new object[] { map, snapshot, x, y, action });
        }

        private static void InvokeForEachVertex(Map map, SymmetrySnapshot snapshot, int x, int y,
            System.Action<SymmetryVertexTarget> action)
        {
            MethodInfo method = typeof(MapEditFacade).GetMethod("ForEachVertex",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(null, new object[] { map, snapshot, x, y, action });
        }

        private static void AssertTarget(SymmetryCellTarget target, int x, int y,
            SymmetryTransform transform)
        {
            Assert.That(target.X, Is.EqualTo(x));
            Assert.That(target.Y, Is.EqualTo(y));
            Assert.That(target.Transform, Is.EqualTo(transform));
        }

        private static void AssertTarget(SymmetryVertexTarget target, int x, int y,
            SymmetryTransform transform)
        {
            Assert.That(target.X, Is.EqualTo(x));
            Assert.That(target.Y, Is.EqualTo(y));
            Assert.That(target.Transform, Is.EqualTo(transform));
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private static T GetField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (T)field.GetValue(target);
        }

        private sealed class SetArrayValue : IReversibleCommand
        {
            private readonly int[] _values;
            private readonly int _index;
            private readonly int _before;
            private readonly int _after;

            public SetArrayValue(int[] values, int index, int after)
            {
                _values = values;
                _index = index;
                _before = values[index];
                _after = after;
            }

            public void Execute() => _values[_index] = _after;
            public void Undo() => _values[_index] = _before;
            public void DisposeUndo() { }
            public void DisposeRedo() { }
        }
    }
}
