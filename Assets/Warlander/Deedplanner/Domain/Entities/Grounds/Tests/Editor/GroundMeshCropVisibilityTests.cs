using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Warlander.Deedplanner.Domain.Entities.Grounds.Tests
{
    public class GroundMeshCropVisibilityTests
    {
        [Test]
        public void NestedCameraScopesRestorePreviousCropVisibility()
        {
            GameObject gameObject = new GameObject("Ground Mesh");
            Mesh renderMesh = new Mesh();
            renderMesh.vertices = new[] { Vector3.zero };
            try
            {
                GroundMesh groundMesh = gameObject.AddComponent<GroundMesh>();
                SetField(groundMesh, "<RenderMesh>k__BackingField", renderMesh);
                SetField(groundMesh, "uv2WithoutCrops", CreateUv(1));
                SetField(groundMesh, "uv2WithCrops", CreateUv(2));

                using (groundMesh.PrepareForCamera(true))
                {
                    Assert.That(renderMesh.uv2[0].x, Is.EqualTo(2));
                    using (groundMesh.PrepareForCamera(false))
                    {
                        Assert.That(renderMesh.uv2[0].x, Is.EqualTo(1));
                    }
                    Assert.That(renderMesh.uv2[0].x, Is.EqualTo(2));
                }
                Assert.That(renderMesh.uv2[0].x, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(renderMesh);
                Object.DestroyImmediate(gameObject);
            }
        }

        private static Vector2[] CreateUv(float textureIndex)
        {
            return new[] { new Vector2(textureIndex, 0) };
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}
