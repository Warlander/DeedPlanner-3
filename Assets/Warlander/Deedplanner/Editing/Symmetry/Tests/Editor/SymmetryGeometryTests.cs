using NUnit.Framework;
using UnityEngine;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Domain.Entities.Grounds;
using System.Collections.Generic;

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
    }
}
