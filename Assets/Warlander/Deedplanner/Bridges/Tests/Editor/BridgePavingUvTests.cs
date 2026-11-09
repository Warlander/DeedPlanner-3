using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Warlander.Deedplanner.Domain;

namespace Warlander.Deedplanner.Bridges.Tests
{
    public class BridgePavingUvTests
    {
        [TestCase(EntityOrientation.Down, 0)]
        [TestCase(EntityOrientation.Up, 0)]
        [TestCase(EntityOrientation.Left, 0)]
        [TestCase(EntityOrientation.Right, 0)]
        [TestCase(EntityOrientation.Down, 8)]
        [TestCase(EntityOrientation.Up, 8)]
        [TestCase(EntityOrientation.Left, 8)]
        [TestCase(EntityOrientation.Right, 8)]
        public void PavingKeepsTextureAndTangentDirectionWhenModelFacingChanges(EntityOrientation facing, int skew)
        {
            var root = new GameObject("Paving UV test");
            BridgePart part = root.AddComponent<BridgePart>();
            Mesh mesh = null;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            try
            {
                bool vertical = facing == EntityOrientation.Down || facing == EntityOrientation.Up;
                float angle = facing == EntityOrientation.Left ? 90
                    : facing == EntityOrientation.Up ? 180 : facing == EntityOrientation.Right ? 270 : 0;
                root.transform.rotation = Quaternion.Euler(0, angle, 0);
                root.transform.position = facing == EntityOrientation.Down ? new Vector3(0, 2, 4)
                    : facing == EntityOrientation.Up ? new Vector3(4, 2, 0)
                    : facing == EntityOrientation.Left ? new Vector3(4, 2, 4) : new Vector3(0, 2, 0);
                typeof(BridgePart).GetField("_renderOrientation", flags).SetValue(part, facing);
                typeof(BridgePart).GetField("_skew", flags).SetValue(part, skew);
                var overlay = (GameObject)typeof(BridgePart).GetMethod("CreatePavingOverlay", flags).Invoke(part, null);
                mesh = overlay.GetComponent<MeshFilter>().sharedMesh;

                Vector3[] vertices = mesh.vertices;
                Vector2[] uv = mesh.uv;
                Vector4[] tangents = mesh.tangents;
                Assert.That(tangents.Length, Is.EqualTo(vertices.Length));
                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector3 world = root.transform.TransformPoint(vertices[i]);
                    Vector2 expected = vertical ? new Vector2(world.x / 4, world.z / 4)
                        : new Vector2(1 - world.z / 4, world.x / 4);
                    Assert.That(Vector2.Distance(uv[i], expected), Is.LessThan(0.00001f), $"Vertex {i}");
                    Vector3 tangent = root.transform.TransformDirection((Vector3)tangents[i]).normalized;
                    Assert.That(Vector3.Dot(tangent, vertical ? Vector3.right : Vector3.back),
                        Is.GreaterThan(0.999f), $"Tangent {i}");
                }
            }
            finally
            {
                if (mesh) Object.DestroyImmediate(mesh);
                typeof(BridgePart).GetField("_pavingMesh", flags).SetValue(part, null);
                Object.DestroyImmediate(root);
            }
        }
    }
}