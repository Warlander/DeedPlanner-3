using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Editing.Tests
{
    public class AreaSelectionOutlineTests
    {
        [TestCase(0.75f)]
        [TestCase(1f)]
        [TestCase(1.5f)]
        [TestCase(2f)]
        public void GoldWidthStaysConstantInScreenPixels(float scale)
        {
            using var fixture = new OutlineFixture(scale, new Vector2(10.2f, 20.3f));
            Vector3[] vertices = fixture.Mesh.vertices;
            Color32[] colors = fixture.Mesh.colors32;
            float minY = vertices.Where((v, i) => colors[i].a == 255).Min(v => v.y);

            Assert.That((20.3f - minY) * 2f * scale, Is.EqualTo(1.5f).Within(0.001f));
        }

        [Test]
        public void CornerHasOneGoldLayer()
        {
            using var fixture = new OutlineFixture(1f, Vector2.zero);
            Vector3[] vertices = fixture.Mesh.vertices;
            Color32[] colors = fixture.Mesh.colors32;
            int[] triangles = fixture.Mesh.triangles;
            int layers = 0;
            var point = new Vector2(0.23f, 0.41f);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                if (colors[a].a != 255 || colors[b].a != 255 || colors[c].a != 255) continue;
                float ab = Cross((Vector2)vertices[b] - (Vector2)vertices[a], point - (Vector2)vertices[a]);
                float bc = Cross((Vector2)vertices[c] - (Vector2)vertices[b], point - (Vector2)vertices[b]);
                float ca = Cross((Vector2)vertices[a] - (Vector2)vertices[c], point - (Vector2)vertices[c]);
                if (ab > 0 && bc > 0 && ca > 0 || ab < 0 && bc < 0 && ca < 0) layers++;
            }

            Assert.That(layers, Is.EqualTo(1), "Joined strokes must not blend twice at corners.");
        }

        [Test]
        public void OuterEdgeHasTransparentFeather()
        {
            using var fixture = new OutlineFixture(1f, Vector2.zero);

            Assert.That(fixture.Mesh.colors32.Any(c => c.a == 0), Is.True);
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        private sealed class OutlineFixture : System.IDisposable
        {
            private readonly GameObject _root;
            public Mesh Mesh { get; }

            public OutlineFixture(float scale, Vector2 origin)
            {
                _root = new GameObject("Outline canvas", typeof(Canvas));
                var canvas = _root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.scaleFactor = scale;
                var root = new GameObject("Outline", typeof(RectTransform), typeof(CanvasRenderer), typeof(AreaSelectionOutline));
                root.transform.SetParent(_root.transform, false);
                var graphic = root.GetComponent<AreaSelectionOutline>();
                graphic.SetSegments(new[]
                {
                    origin, origin + new Vector2(100, 0),
                    origin + new Vector2(100, 0), origin + new Vector2(100, 60),
                    origin + new Vector2(100, 60), origin + new Vector2(0, 60),
                    origin + new Vector2(0, 60), origin
                });
                Mesh = new Mesh();
                using var vertices = new VertexHelper();
                typeof(AreaSelectionOutline).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Invoke(graphic, new object[] { vertices });
                vertices.FillMesh(Mesh);
            }

            public void Dispose()
            {
                Object.DestroyImmediate(Mesh);
                Object.DestroyImmediate(_root);
            }
        }
    }
}
