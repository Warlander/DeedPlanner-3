using System;
using NUnit.Framework;
using UnityEngine;

namespace Warlander.Deedplanner.Domain.Tests
{
    public class MapLevelRendererTests
    {
        private RenderFixture _fixture;

        [SetUp]
        public void SetUp()
        {
            _fixture = new RenderFixture();
        }

        [TearDown]
        public void TearDown()
        {
            _fixture.Dispose();
        }

        [Test]
        public void PrepareForCameraFiltersSurfaceLevelsAndGrid()
        {
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(1, false, true)))
            {
                Assert.That(_fixture.Surface[0].Renderer.forceRenderingOff, Is.False);
                Assert.That(GetBaseColor(_fixture.Surface[0].Renderer).r, Is.EqualTo(0.6f));
                Assert.That(_fixture.Surface[1].Renderer.forceRenderingOff, Is.False);
                Assert.That(GetBaseColor(_fixture.Surface[1].Renderer).r, Is.EqualTo(1f));
                Assert.That(_fixture.Surface[2].Renderer.forceRenderingOff, Is.True);
                Assert.That(_fixture.Surface[2].Collider.enabled, Is.False);
                Assert.That(_fixture.Cave[0].Renderer.forceRenderingOff, Is.True);
                Assert.That(_fixture.SurfaceGrid.Renderer.forceRenderingOff, Is.False);
                Assert.That(_fixture.CaveGrid.Renderer.forceRenderingOff, Is.True);
                Assert.That(_fixture.CaveShell.Renderer.forceRenderingOff, Is.True);
                Assert.That(_fixture.SurfaceGrid.Root.localPosition.y, Is.EqualTo(3.01f));
            }
        }

        [Test]
        public void PrepareForCameraUsesRelativeCaveStoreys()
        {
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(-2, false, true)))
            {
                Assert.That(_fixture.Surface[0].Renderer.forceRenderingOff, Is.True);
                Assert.That(_fixture.Cave[0].Renderer.forceRenderingOff, Is.False);
                Assert.That(GetBaseColor(_fixture.Cave[0].Renderer).r, Is.EqualTo(0.6f));
                Assert.That(_fixture.Cave[1].Renderer.forceRenderingOff, Is.False);
                Assert.That(GetBaseColor(_fixture.Cave[1].Renderer).r, Is.EqualTo(1f));
                Assert.That(_fixture.Cave[2].Renderer.forceRenderingOff, Is.True);
                Assert.That(GetBaseColor(_fixture.CaveShell.Renderer).r, Is.EqualTo(0.6f));
                Assert.That(_fixture.CaveGrid.Renderer.forceRenderingOff, Is.False);
                Assert.That(_fixture.CaveGrid.Root.localPosition.y, Is.EqualTo(3f));
            }
        }

        [Test]
        public void NestedScopesRestoreExactPriorState()
        {
            MaterialPropertyBlock originalBlock = new MaterialPropertyBlock();
            originalBlock.SetColor(ShaderPropertyIds.BaseColor, Color.cyan);
            _fixture.Surface[0].Renderer.SetPropertyBlock(originalBlock);
            _fixture.Surface[0].Collider.enabled = false;

            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, false, true)))
            {
                Assert.That(GetBaseColor(_fixture.Surface[0].Renderer), Is.EqualTo(Color.white));

                using (_fixture.Renderer.PrepareForCamera(new MapRenderView(-1, false, false)))
                {
                    Assert.That(_fixture.Surface[0].Renderer.forceRenderingOff, Is.True);
                    Assert.That(_fixture.Cave[0].Renderer.forceRenderingOff, Is.False);
                }

                Assert.That(_fixture.Surface[0].Renderer.forceRenderingOff, Is.False);
                Assert.That(GetBaseColor(_fixture.Surface[0].Renderer), Is.EqualTo(Color.white));
                Assert.That(_fixture.Surface[0].Collider.enabled, Is.False);
            }

            Assert.That(_fixture.Surface[0].Renderer.forceRenderingOff, Is.False);
            Assert.That(GetBaseColor(_fixture.Surface[0].Renderer), Is.EqualTo(Color.cyan));
            Assert.That(_fixture.Surface[0].Collider.enabled, Is.False);
        }

        private static Color GetBaseColor(Renderer renderer)
        {
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            return propertyBlock.GetColor(ShaderPropertyIds.BaseColor);
        }

        private sealed class RenderFixture : IDisposable
        {
            private readonly GameObject _container = new GameObject("Map rendering test");

            public MapLevelRenderer Renderer { get; } = new MapLevelRenderer();
            public RenderTarget[] Surface { get; }
            public RenderTarget[] Cave { get; }
            public RenderTarget SurfaceGrid { get; }
            public RenderTarget CaveGrid { get; }
            public RenderTarget CaveShell { get; }

            public RenderFixture()
            {
                Surface = CreateLevels("Surface");
                Cave = CreateLevels("Cave");
                SurfaceGrid = CreateTarget("Surface grid");
                CaveGrid = CreateTarget("Cave grid");
                CaveShell = CreateTarget("Cave shell");

                Renderer.Initialize(GetRoots(Surface), GetRoots(Cave), SurfaceGrid.Root, CaveGrid.Root,
                    CaveShell.Root, null, null);
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(_container);
            }

            private RenderTarget[] CreateLevels(string prefix)
            {
                RenderTarget[] levels = new RenderTarget[3];
                for (int i = 0; i < levels.Length; i++)
                    levels[i] = CreateTarget(prefix + " " + i);
                return levels;
            }

            private RenderTarget CreateTarget(string name)
            {
                GameObject root = new GameObject(name);
                root.transform.SetParent(_container.transform);
                GameObject child = new GameObject("Content", typeof(MeshRenderer), typeof(BoxCollider));
                child.transform.SetParent(root.transform);
                return new RenderTarget(root.transform, child.GetComponent<Renderer>(), child.GetComponent<Collider>());
            }

            private static Transform[] GetRoots(RenderTarget[] targets)
            {
                Transform[] roots = new Transform[targets.Length];
                for (int i = 0; i < targets.Length; i++)
                    roots[i] = targets[i].Root;
                return roots;
            }
        }

        private sealed class RenderTarget
        {
            public Transform Root { get; }
            public Renderer Renderer { get; }
            public Collider Collider { get; }

            public RenderTarget(Transform root, Renderer renderer, Collider collider)
            {
                Root = root;
                Renderer = renderer;
                Collider = collider;
            }
        }
    }
}
