using System;
using System.Collections.Generic;
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
        public void UnmodifiedRendererKeepsTheExistingPropertyBlockBatchingPath()
        {
            Renderer renderer = _fixture.Surface[0].Renderer;
            Assert.That(renderer.HasPropertyBlock(), Is.False);
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false)))
            {
                Assert.That(renderer.HasPropertyBlock(), Is.False);
            }
            Assert.That(renderer.HasPropertyBlock(), Is.True);
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            Assert.That(block.isEmpty, Is.True);
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false)))
            {
                Assert.That(renderer.HasPropertyBlock(), Is.True);
                using (_fixture.Renderer.PrepareForCamera(new MapRenderView(-1, true, false)))
                {
                    Assert.That(renderer.HasPropertyBlock(), Is.True);
                    renderer.GetPropertyBlock(block);
                    Assert.That(block.isEmpty, Is.True);
                }
                renderer.GetPropertyBlock(block);
                Assert.That(block.isEmpty, Is.True);
            }
            Assert.That(renderer.HasPropertyBlock(), Is.True);
            renderer.GetPropertyBlock(block);
            Assert.That(block.isEmpty, Is.True);
        }

        [Test]
        public void ModelShaderFullMapScopesTemporarilyClearWarmedEmptyBlocks()
        {
            Renderer renderer = _fixture.Surface[0].Renderer;
            Material material = _fixture.CreateMaterial("Warlander/ModelShader");
            var original = new Color(0.8f, 0.4f, 0.2f, 0.25f);
            material.SetColor(ShaderPropertyIds.BaseColor, original);
            renderer.sharedMaterial = material;
            Assert.That(renderer.HasPropertyBlock(), Is.False);
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false)))
            {
                Assert.That(renderer.HasPropertyBlock(), Is.False);
            }
            AssertEmptyPropertyBlock(renderer);
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, false, false)))
            {
                AssertEmptyPropertyBlock(renderer);
            }

            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false)))
            {
                Assert.That(renderer.HasPropertyBlock(), Is.False);
                using (_fixture.Renderer.PrepareForCamera(new MapRenderView(1, false, false)))
                {
                    Assert.That(GetBaseColor(renderer).r, Is.EqualTo(0.48f).Within(0.001f));
                    Assert.That(GetBaseColor(renderer).a, Is.EqualTo(original.a));
                    using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false)))
                    {
                        Assert.That(GetBaseColor(renderer), Is.EqualTo(original));
                    }
                    Assert.That(GetBaseColor(renderer).r, Is.EqualTo(0.48f).Within(0.001f));
                }
                AssertEmptyPropertyBlock(renderer);
            }
            AssertEmptyPropertyBlock(renderer);
            Assert.That(GetEffectiveBaseColor(renderer), Is.EqualTo(original));
        }

        [Test]
        public void ModelShaderNonemptyOverridesRemainAfterNestedViews()
        {
            Renderer renderer = _fixture.Surface[0].Renderer;
            renderer.sharedMaterial = _fixture.CreateMaterial("Warlander/ModelShader");
            var original = new Color(0.8f, 0.4f, 0.2f, 0.45f);
            var shear = new Vector4(0.2f, -0.1f, 0, 0);
            int valueId = Shader.PropertyToID("_ModelOverrideValue");
            var block = new MaterialPropertyBlock();
            block.SetColor(ShaderPropertyIds.BaseColor, original);
            block.SetVector(ShaderPropertyIds.ShearY, shear);
            block.SetFloat(valueId, 17);
            renderer.SetPropertyBlock(block);

            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false)))
            {
                Assert.That(renderer.HasPropertyBlock(), Is.True);
                Assert.That(GetBaseColor(renderer), Is.EqualTo(original));
            }

            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(1, false, false)))
            {
                using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false)))
                {
                    renderer.GetPropertyBlock(block);
                    Assert.That(block.GetColor(ShaderPropertyIds.BaseColor), Is.EqualTo(original));
                    Assert.That(block.GetVector(ShaderPropertyIds.ShearY), Is.EqualTo(shear));
                    Assert.That(block.GetFloat(valueId), Is.EqualTo(17));
                }
                Assert.That(GetBaseColor(renderer).a, Is.EqualTo(original.a));
            }
            Assert.That(renderer.HasPropertyBlock(), Is.True);
            renderer.GetPropertyBlock(block);
            Assert.That(block.GetColor(ShaderPropertyIds.BaseColor), Is.EqualTo(original));
            Assert.That(block.GetVector(ShaderPropertyIds.ShearY), Is.EqualTo(shear));
            Assert.That(block.GetFloat(valueId), Is.EqualTo(17));
        }

        [Test]
        public void MixedModelShaderMaterialsKeepTheExistingEmptyPropertyBlock()
        {
            Renderer renderer = _fixture.Surface[0].Renderer;
            renderer.sharedMaterials = new[] { _fixture.CreateMaterial("Warlander/ModelShader"),
                _fixture.CreateMaterial("Universal Render Pipeline/Unlit") };
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false))) { }
            AssertEmptyPropertyBlock(renderer);
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false)))
            {
                AssertEmptyPropertyBlock(renderer);
            }
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(1, false, false)))
            {
                Assert.That(GetBaseColor(renderer).r, Is.EqualTo(0.6f));
            }
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            Assert.That(renderer.HasPropertyBlock(), Is.True);
            Assert.That(block.isEmpty, Is.True);
        }

        [Test]
        public void ChangedShaderAndReplacementRendererUseTheirCurrentMaterials()
        {
            Renderer renderer = _fixture.Surface[0].Renderer;
            Material modelMaterial = _fixture.CreateMaterial("Warlander/ModelShader");
            Material otherMaterial = _fixture.CreateMaterial("Universal Render Pipeline/Unlit");
            Shader modelShader = modelMaterial.shader;
            renderer.sharedMaterial = modelMaterial;
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false))) { }
            AssertEmptyPropertyBlock(renderer);

            modelMaterial.shader = otherMaterial.shader;
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false)))
            {
                AssertEmptyPropertyBlock(renderer);
            }
            AssertEmptyPropertyBlock(renderer);
            modelMaterial.shader = modelShader;
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false)))
            {
                Assert.That(renderer.HasPropertyBlock(), Is.False);
            }
            AssertEmptyPropertyBlock(renderer);

            UnityEngine.Object.DestroyImmediate(renderer.gameObject);
            var replacement = new GameObject("Replacement material test", typeof(MeshRenderer));
            replacement.transform.SetParent(_fixture.Surface[0].Root);
            renderer = replacement.GetComponent<Renderer>();
            renderer.sharedMaterial = otherMaterial;
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false))) { }
            AssertEmptyPropertyBlock(renderer);
            renderer.sharedMaterial = modelMaterial;
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false)))
            {
                Assert.That(renderer.HasPropertyBlock(), Is.False);
            }
            AssertEmptyPropertyBlock(renderer);
        }

        [Test]
        public void ChangedMaterialDefaultShearControlsTemporaryClearing()
        {
            Renderer renderer = _fixture.Surface[0].Renderer;
            Material material = _fixture.CreateMaterial("Warlander/ModelShader");
            renderer.sharedMaterial = material;
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false))) { }
            AssertEmptyPropertyBlock(renderer);
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false)))
            {
                Assert.That(renderer.HasPropertyBlock(), Is.False);
            }
            material.SetVector(ShaderPropertyIds.ShearY, new Vector4(0.1f, -0.05f, 0, 0));
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false)))
            {
                AssertEmptyPropertyBlock(renderer);
            }
            AssertEmptyPropertyBlock(renderer);
            material.SetVector(ShaderPropertyIds.ShearY, Vector4.zero);
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false)))
            {
                Assert.That(renderer.HasPropertyBlock(), Is.False);
            }
            AssertEmptyPropertyBlock(renderer);
        }

        [TestCase(false, false, false)]
        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        [TestCase(false, false, true)]
        public void IncompatibleMaterialDefaultsKeepOtherModelsOnTheLegacyPath(
            bool nonemptyBlock, bool lateRenderer, bool multipleMaterials)
        {
            Renderer ordinary = _fixture.Surface[0].Renderer;
            ordinary.sharedMaterial = _fixture.CreateMaterial("Warlander/ModelShader");
            Material material = _fixture.CreateMaterial("Warlander/ModelShader");
            Renderer incompatible = lateRenderer ? null : _fixture.Surface[1].Renderer;
            if (incompatible) incompatible.sharedMaterial = material;
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false))) { }
            AssertEmptyPropertyBlock(ordinary);

            if (lateRenderer)
            {
                var added = new GameObject("Late incompatible model", typeof(MeshRenderer));
                added.transform.SetParent(_fixture.Surface[1].Root);
                incompatible = added.GetComponent<Renderer>();
                incompatible.sharedMaterial = material;
                Assert.That(incompatible.HasPropertyBlock(), Is.False);
            }
            if (multipleMaterials)
                incompatible.sharedMaterials = new[] { _fixture.CreateMaterial("Universal Render Pipeline/Unlit"), material };
            if (nonemptyBlock)
            {
                var block = new MaterialPropertyBlock();
                block.SetColor(ShaderPropertyIds.BaseColor, Color.cyan);
                incompatible.SetPropertyBlock(block);
            }

            material.SetVector(ShaderPropertyIds.ShearY, new Vector4(0.1f, -0.05f, 0, 0));
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false)))
            {
                AssertEmptyPropertyBlock(ordinary);
                if (nonemptyBlock) Assert.That(GetBaseColor(incompatible), Is.EqualTo(Color.cyan));
            }
            material.SetVector(ShaderPropertyIds.ShearY, Vector4.zero);
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false)))
            {
                Assert.That(ordinary.HasPropertyBlock(), Is.False);
                if (nonemptyBlock) Assert.That(GetBaseColor(incompatible), Is.EqualTo(Color.cyan));
            }
            AssertEmptyPropertyBlock(ordinary);
            material.SetVector(ShaderPropertyIds.ShearY, new Vector4(0.1f, -0.05f, 0, 0));
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false)))
            {
                AssertEmptyPropertyBlock(ordinary);
            }
            AssertEmptyPropertyBlock(ordinary);
        }

        [Test]
        public void NewOverridesDuringTemporaryClearingSurviveScopeDisposal()
        {
            Renderer renderer = _fixture.Surface[0].Renderer;
            renderer.sharedMaterial = _fixture.CreateMaterial("Warlander/ModelShader");
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false))) { }
            var block = new MaterialPropertyBlock();
            int valueId = Shader.PropertyToID("_LateModelOverride");
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false)))
            {
                Assert.That(renderer.HasPropertyBlock(), Is.False);
                block.SetColor(ShaderPropertyIds.BaseColor, Color.magenta);
                block.SetFloat(valueId, 23);
                renderer.SetPropertyBlock(block);
                renderer.forceRenderingOff = true;
            }
            renderer.GetPropertyBlock(block);
            Assert.That(block.GetColor(ShaderPropertyIds.BaseColor), Is.EqualTo(Color.magenta));
            Assert.That(block.GetFloat(valueId), Is.EqualTo(23));
            Assert.That(renderer.forceRenderingOff, Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NestedHiddenViewsRetainOriginalVisibilityForInnerSurfaceView(bool originallyHidden)
        {
            Renderer renderer = _fixture.Surface[0].Renderer;
            renderer.forceRenderingOff = originallyHidden;
            var block = new MaterialPropertyBlock();
            block.SetColor(ShaderPropertyIds.BaseColor, Color.cyan);
            renderer.SetPropertyBlock(block);

            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(-1, true, false)))
            {
                Assert.That(renderer.forceRenderingOff, Is.True);
                using (_fixture.Renderer.PrepareForCamera(new MapRenderView(-2, true, false)))
                {
                    Assert.That(renderer.forceRenderingOff, Is.True);
                    using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false)))
                    {
                        Assert.That(renderer.forceRenderingOff, Is.EqualTo(originallyHidden));
                        Assert.That(GetBaseColor(renderer), Is.EqualTo(Color.cyan));
                    }
                    Assert.That(renderer.forceRenderingOff, Is.True);
                }
                Assert.That(renderer.forceRenderingOff, Is.True);
            }
            Assert.That(renderer.forceRenderingOff, Is.EqualTo(originallyHidden));
            Assert.That(GetBaseColor(renderer), Is.EqualTo(Color.cyan));
        }

        [Test]
        public void ReusedPropertyBlocksPreserveRendererOverridesWithoutLeakingProperties()
        {
            int valueId = Shader.PropertyToID("_SnapshotValue");
            int vectorId = Shader.PropertyToID("_SnapshotVector");
            int textureId = Shader.PropertyToID("_SnapshotTexture");
            var vector = new Vector4(1, 2, 3, 4);
            var texture = new Texture2D(1, 1);
            var block = new MaterialPropertyBlock();
            try
            {
                for (int pass = 0; pass < 2; pass++)
                {
                    Renderer rich = _fixture.Surface[pass].Renderer;
                    Renderer plain = _fixture.Surface[1 - pass].Renderer;
                    block.Clear();
                    block.SetFloat(valueId, 7);
                    block.SetVector(vectorId, vector);
                    block.SetTexture(textureId, texture);
                    rich.SetPropertyBlock(block);
                    block.Clear();
                    plain.SetPropertyBlock(block);

                    void AssertOverrides()
                    {
                        rich.GetPropertyBlock(block);
                        Assert.That(block.GetFloat(valueId), Is.EqualTo(7));
                        Assert.That(block.GetVector(vectorId), Is.EqualTo(vector));
                        Assert.That(block.GetTexture(textureId), Is.SameAs(texture));
                        plain.GetPropertyBlock(block);
                        Assert.That(block.HasFloat(valueId), Is.False);
                        Assert.That(block.GetVector(vectorId), Is.EqualTo(Vector4.zero));
                        Assert.That(block.GetTexture(textureId), Is.Null);
                    }

                    using (_fixture.Renderer.PrepareForCamera(new MapRenderView(2, false, false)))
                    {
                        AssertOverrides();
                    }
                    AssertOverrides();
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void NestedSiblingScopesPreserveTheOuterSnapshot()
        {
            Renderer renderer = _fixture.Surface[0].Renderer;
            var block = new MaterialPropertyBlock();
            block.SetColor(ShaderPropertyIds.BaseColor, Color.cyan);
            renderer.SetPropertyBlock(block);

            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(1, false, true)))
            {
                for (int i = 0; i < 2; i++)
                {
                    using (_fixture.Renderer.PrepareForCamera(new MapRenderView(2, false, false)))
                    {
                        Assert.That(GetBaseColor(renderer).g, Is.EqualTo(0.25f));
                        Assert.That(_fixture.SurfaceGrid.Renderer.forceRenderingOff, Is.True);
                    }
                    Assert.That(GetBaseColor(renderer).g, Is.EqualTo(0.6f));
                    Assert.That(_fixture.SurfaceGrid.Renderer.forceRenderingOff, Is.False);
                    Assert.That(_fixture.SurfaceGrid.Root.localPosition.y, Is.EqualTo(3.01f));
                }
            }
            Assert.That(GetBaseColor(renderer), Is.EqualTo(Color.cyan));
        }

        [Test]
        public void DisposingAnOldScopeDoesNotChangeTheCurrentScope()
        {
            Renderer renderer = _fixture.Surface[0].Renderer;
            IDisposable oldScope = _fixture.Renderer.PrepareForCamera(new MapRenderView(1, false, false));
            oldScope.Dispose();

            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(2, false, false)))
            {
                oldScope.Dispose();
                Assert.That(GetBaseColor(renderer).r, Is.EqualTo(0.25f));
                Assert.That(_fixture.SurfaceGrid.Root.localPosition.y, Is.EqualTo(6.01f));
            }
            Assert.That(GetEffectiveBaseColor(renderer), Is.EqualTo(Color.white));
            Assert.That(_fixture.SurfaceGrid.Root.localPosition, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void DestroyedCapturedRendererDoesNotBreakLaterScopes()
        {
            Renderer deleted = _fixture.Surface[0].Renderer;
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(1, false, false)))
            {
                UnityEngine.Object.DestroyImmediate(deleted.gameObject);
            }

            var replacement = new GameObject("Replacement model", typeof(MeshRenderer));
            replacement.transform.SetParent(_fixture.Surface[0].Root);
            Renderer renderer = replacement.GetComponent<Renderer>();
            var block = new MaterialPropertyBlock();
            block.SetColor(ShaderPropertyIds.BaseColor, Color.magenta);
            renderer.SetPropertyBlock(block);
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(1, false, false)))
            {
                Assert.That(GetBaseColor(renderer).r, Is.EqualTo(0.6f));
                Assert.That(GetBaseColor(renderer).g, Is.Zero);
            }
            Assert.That(GetBaseColor(renderer), Is.EqualTo(Color.magenta));
        }

        [Test]
        public void CameraPassRescansChangedHierarchyAndRestoresNestedViews()
        {
            var surfaceView = new MapRenderView(2, false, true);
            using (_fixture.Renderer.PrepareForCamera(surfaceView)) { }

            Renderer moved = _fixture.Surface[0].Renderer;
            moved.transform.SetParent(_fixture.Cave[1].Root);
            var addedObject = new GameObject("Added model", typeof(MeshRenderer));
            addedObject.transform.SetParent(_fixture.Surface[1].Root);
            Renderer added = addedObject.GetComponent<Renderer>();
            var block = new MaterialPropertyBlock();
            block.SetColor(ShaderPropertyIds.BaseColor, Color.cyan);
            moved.SetPropertyBlock(block);
            block.SetColor(ShaderPropertyIds.BaseColor, Color.magenta);
            added.SetPropertyBlock(block);

            using (_fixture.Renderer.PrepareForCamera(surfaceView))
            {
                Assert.That(moved.forceRenderingOff, Is.True);
                Assert.That(added.forceRenderingOff, Is.False);
                Assert.That(GetBaseColor(added).r, Is.EqualTo(0.6f));
                using (_fixture.Renderer.PrepareForCamera(new MapRenderView(-2, false, false)))
                {
                    Assert.That(moved.forceRenderingOff, Is.False);
                    Assert.That(GetBaseColor(moved), Is.EqualTo(Color.cyan));
                    Assert.That(added.forceRenderingOff, Is.True);
                }
                Assert.That(moved.forceRenderingOff, Is.True);
                Assert.That(added.forceRenderingOff, Is.False);
                Assert.That(GetBaseColor(added).r, Is.EqualTo(0.6f));
            }
            Assert.That(moved.forceRenderingOff, Is.False);
            Assert.That(added.forceRenderingOff, Is.False);
            Assert.That(GetBaseColor(moved), Is.EqualTo(Color.cyan));
            Assert.That(GetBaseColor(added), Is.EqualTo(Color.magenta));
            Assert.That(_fixture.SurfaceGrid.Root.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(_fixture.CaveGrid.Root.localPosition, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void PrepareForCameraFiltersSurfaceLevelsAndGrid()
        {
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(1, false, true)))
            {
                Assert.That(_fixture.Surface[0].Renderer.forceRenderingOff, Is.False);
                Assert.That(GetBaseColor(_fixture.Surface[0].Renderer).r, Is.EqualTo(0.6f));
                Assert.That(_fixture.Surface[1].Renderer.forceRenderingOff, Is.False);
                Assert.That(GetEffectiveBaseColor(_fixture.Surface[1].Renderer).r, Is.EqualTo(1f));
                Assert.That(_fixture.Surface[2].Renderer.forceRenderingOff, Is.True);
                Assert.That(_fixture.Surface[2].Collider.enabled, Is.True);
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
                Assert.That(GetEffectiveBaseColor(_fixture.Cave[1].Renderer).r, Is.EqualTo(1f));
                Assert.That(_fixture.Cave[2].Renderer.forceRenderingOff, Is.True);
                Assert.That(GetBaseColor(_fixture.CaveShell.Renderer).r, Is.EqualTo(0.6f));
                Assert.That(GetCaveOverview(_fixture.CaveShell.Renderer), Is.EqualTo(1f));
                Assert.That(_fixture.CaveGrid.Renderer.forceRenderingOff, Is.False);
                Assert.That(_fixture.CaveGrid.Root.localPosition.y, Is.EqualTo(3f));
            }
        }

        [Test]
        public void FullMapCaveViewHidesSolidSurfaceVisualization()
        {
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(-1, true, true)))
            {
                Assert.That(GetCaveOverview(_fixture.CaveShell.Renderer), Is.Zero);
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
                Assert.That(GetBaseColor(_fixture.Surface[0].Renderer), Is.EqualTo(Color.cyan));

                using (_fixture.Renderer.PrepareForCamera(new MapRenderView(-1, false, false)))
                {
                    Assert.That(_fixture.Surface[0].Renderer.forceRenderingOff, Is.True);
                    Assert.That(_fixture.Cave[0].Renderer.forceRenderingOff, Is.False);
                }

                Assert.That(_fixture.Surface[0].Renderer.forceRenderingOff, Is.False);
                Assert.That(GetBaseColor(_fixture.Surface[0].Renderer), Is.EqualTo(Color.cyan));
                Assert.That(_fixture.Surface[0].Collider.enabled, Is.False);
            }

            Assert.That(_fixture.Surface[0].Renderer.forceRenderingOff, Is.False);
            Assert.That(GetBaseColor(_fixture.Surface[0].Renderer), Is.EqualTo(Color.cyan));
            Assert.That(_fixture.Surface[0].Collider.enabled, Is.False);
        }

        [Test]
        public void FadedRendererPreservesExistingColorAndAlpha()
        {
            Color originalColor = new Color(0.8f, 0.4f, 0.2f, 0.25f);
            Vector4 shear = new Vector4(0.1f, -0.2f, 0, 0);
            MaterialPropertyBlock originalBlock = new MaterialPropertyBlock();
            originalBlock.SetColor(ShaderPropertyIds.BaseColor, originalColor);
            originalBlock.SetVector(ShaderPropertyIds.ShearY, shear);
            _fixture.Surface[0].Renderer.SetPropertyBlock(originalBlock);

            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(1, false, true)))
            {
                Color fadedColor = GetBaseColor(_fixture.Surface[0].Renderer);
                Assert.That(fadedColor.r, Is.EqualTo(0.48f).Within(0.001f));
                Assert.That(fadedColor.g, Is.EqualTo(0.24f).Within(0.001f));
                Assert.That(fadedColor.b, Is.EqualTo(0.12f).Within(0.001f));
                Assert.That(fadedColor.a, Is.EqualTo(0.25f));
                _fixture.Surface[0].Renderer.GetPropertyBlock(originalBlock);
                Assert.That(originalBlock.GetVector(ShaderPropertyIds.ShearY), Is.EqualTo(shear));
            }
            _fixture.Surface[0].Renderer.GetPropertyBlock(originalBlock);
            Assert.That(originalBlock.GetColor(ShaderPropertyIds.BaseColor), Is.EqualTo(originalColor));
            Assert.That(originalBlock.GetVector(ShaderPropertyIds.ShearY), Is.EqualTo(shear));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-2)]
        public void CaveShellRestoresOverridesAcrossNestedViews(int outerLevel)
        {
            Renderer renderer = _fixture.CaveShell.Renderer;
            Color originalColor = new Color(0.8f, 0.4f, 0.2f, 0.25f);
            Vector4 shear = new Vector4(0.1f, -0.2f, 0, 0);
            var block = new MaterialPropertyBlock();
            block.SetColor(ShaderPropertyIds.BaseColor, originalColor);
            block.SetVector(ShaderPropertyIds.ShearY, shear);
            block.SetFloat(ShaderPropertyIds.CaveOverview, 0.7f);
            renderer.SetPropertyBlock(block);

            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(outerLevel, false, false)))
            {
                Color outerColor = GetBaseColor(renderer);
                Assert.That(GetCaveOverview(renderer), Is.EqualTo(outerLevel < 0 ? 1f : 0f));
                using (_fixture.Renderer.PrepareForCamera(new MapRenderView(-1, true, false)))
                {
                    Assert.That(GetBaseColor(renderer), Is.EqualTo(originalColor));
                    Assert.That(GetCaveOverview(renderer), Is.Zero);
                }
                Assert.That(GetBaseColor(renderer), Is.EqualTo(outerColor));
                Assert.That(GetCaveOverview(renderer), Is.EqualTo(outerLevel < 0 ? 1f : 0f));
            }
            renderer.GetPropertyBlock(block);
            Assert.That(block.GetColor(ShaderPropertyIds.BaseColor), Is.EqualTo(originalColor));
            Assert.That(block.GetVector(ShaderPropertyIds.ShearY), Is.EqualTo(shear));
            Assert.That(block.GetFloat(ShaderPropertyIds.CaveOverview), Is.EqualTo(0.7f));
        }

        [TestCase(0, false)]
        [TestCase(-1, false)]
        [TestCase(0, true)]
        [TestCase(-1, true)]
        public void NestedFadeRestoresOpaqueOrHiddenOuterView(int outerLevel, bool originallyHidden)
        {
            Renderer renderer = _fixture.Surface[0].Renderer;
            renderer.forceRenderingOff = originallyHidden;
            var block = new MaterialPropertyBlock();
            block.SetColor(ShaderPropertyIds.BaseColor, Color.cyan);
            renderer.SetPropertyBlock(block);

            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(outerLevel, true, false)))
            {
                using (_fixture.Renderer.PrepareForCamera(new MapRenderView(1, false, false)))
                {
                    Assert.That(GetBaseColor(renderer).g, Is.EqualTo(0.6f).Within(0.001f));
                    Assert.That(renderer.forceRenderingOff, Is.EqualTo(originallyHidden));
                }
                Assert.That(GetBaseColor(renderer), Is.EqualTo(Color.cyan));
                Assert.That(renderer.forceRenderingOff, Is.EqualTo(originallyHidden || outerLevel < 0));
            }
            Assert.That(GetBaseColor(renderer), Is.EqualTo(Color.cyan));
            Assert.That(renderer.forceRenderingOff, Is.EqualTo(originallyHidden));
        }

        [Test]
        public void MaterialColorFallbackDoesNotLeaveAnOverrideAfterRestoration()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            try
            {
                Renderer renderer = _fixture.Surface[0].Renderer;
                var originalColor = new Color(0.8f, 0.4f, 0.2f, 0.25f);
                material.SetColor(ShaderPropertyIds.BaseColor, originalColor);
                renderer.sharedMaterial = material;

                using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false)))
                {
                    using (_fixture.Renderer.PrepareForCamera(new MapRenderView(1, false, false)))
                    {
                        Assert.That(GetBaseColor(renderer).r, Is.EqualTo(0.48f).Within(0.001f));
                        Assert.That(GetBaseColor(renderer).a, Is.EqualTo(originalColor.a));
                    }
                    var block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block);
                    Assert.That(block.HasColor(ShaderPropertyIds.BaseColor), Is.False);
                }
                var restoredBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(restoredBlock);
                Assert.That(restoredBlock.HasColor(ShaderPropertyIds.BaseColor), Is.False);
                Assert.That(GetEffectiveBaseColor(renderer), Is.EqualTo(originalColor));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void NextCameraPassUsesColorChangedBetweenPasses()
        {
            Renderer renderer = _fixture.Surface[0].Renderer;
            var block = new MaterialPropertyBlock();
            block.SetColor(ShaderPropertyIds.BaseColor, Color.cyan);
            renderer.SetPropertyBlock(block);
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(1, false, false))) { }

            block.SetColor(ShaderPropertyIds.BaseColor, Color.magenta);
            renderer.SetPropertyBlock(block);
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(1, false, false)))
            {
                Assert.That(GetBaseColor(renderer).r, Is.EqualTo(0.6f).Within(0.001f));
                Assert.That(GetBaseColor(renderer).g, Is.Zero);
            }
            Assert.That(GetBaseColor(renderer), Is.EqualTo(Color.magenta));
        }

        [Test]
        public void NestedViewsUseOriginalColorRatherThanFadingTwice()
        {
            var original = new Color(0.8f, 0.4f, 0.2f, 0.25f);
            var block = new MaterialPropertyBlock();
            block.SetColor(ShaderPropertyIds.BaseColor, original);
            block.SetFloat(ShaderPropertyIds.CaveOverview, 0.7f);
            Renderer renderer = _fixture.Surface[0].Renderer;
            renderer.SetPropertyBlock(block);

            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(1, false, false)))
            {
                using (_fixture.Renderer.PrepareForCamera(new MapRenderView(2, false, false)))
                {
                    Assert.That(GetBaseColor(renderer).r, Is.EqualTo(0.2f).Within(0.001f));
                    Assert.That(GetBaseColor(renderer).a, Is.EqualTo(original.a));
                    Assert.That(GetCaveOverview(renderer), Is.EqualTo(0.7f));
                }
                using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false)))
                {
                    Assert.That(GetBaseColor(renderer), Is.EqualTo(original));
                }
                Assert.That(GetBaseColor(renderer).r, Is.EqualTo(0.48f).Within(0.001f));
            }
            Assert.That(GetBaseColor(renderer), Is.EqualTo(original));
        }

        [Test]
        public void ActiveViewKeepsOriginalStateBetweenCameraPasses()
        {
            _fixture.Renderer.SetActiveView(new MapRenderView(1, false, false));
            Assert.That(GetEffectiveBaseColor(_fixture.Surface[0].Renderer), Is.EqualTo(Color.white));
            Assert.That(_fixture.Surface[2].Renderer.forceRenderingOff, Is.False);
        }

        [Test]
        public void NextCameraPassIncludesModelsLoadedAfterThePreviousPass()
        {
            var view = new MapRenderView(1, false, false);
            using (_fixture.Renderer.PrepareForCamera(view)) { }
            var model = new GameObject("Late model", typeof(MeshRenderer));
            model.transform.SetParent(_fixture.Surface[0].Root);
            Renderer renderer = model.GetComponent<Renderer>();
            var block = new MaterialPropertyBlock();
            block.SetColor(ShaderPropertyIds.BaseColor, Color.cyan);
            renderer.SetPropertyBlock(block);

            using (_fixture.Renderer.PrepareForCamera(view))
            {
                Assert.That(GetBaseColor(renderer).g, Is.EqualTo(0.6f).Within(0.001f));
                Assert.That(renderer.forceRenderingOff, Is.False);
            }
            Assert.That(GetBaseColor(renderer), Is.EqualTo(Color.cyan));
            renderer.gameObject.SetActive(false);
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false)))
            {
                Assert.That(renderer.gameObject.activeSelf, Is.False);
            }
        }

        [Test]
        public void AddEntityToMapAssignsCaveRaycastLayer()
        {
            GameObject entity = new GameObject("Cave floor", typeof(DynamicModelBehaviour), typeof(BoxCollider));
            entity.layer = LayerMasks.FloorRoofLayer;
            GameObject modelCollider = new GameObject("Model collider", typeof(BoxCollider));
            modelCollider.layer = LayerMasks.FloorRoofLayer;
            modelCollider.transform.SetParent(entity.transform);

            _fixture.Renderer.AddEntityToMap(entity, -1);

            Assert.That(entity.layer, Is.EqualTo(LayerMasks.CaveFloorRoofLayer));
            Assert.That(modelCollider.layer, Is.EqualTo(LayerMasks.CaveFloorRoofLayer));
        }

        [Test]
        public void CaveEditMasksExcludeSurfaceEntityLayers()
        {
            int mask = LayerMasks.GetMaskForTab(Editing.Tab.Objects, -1);

            Assert.That(mask & LayerMasks.CaveFloorRoofMask, Is.Not.Zero);
            Assert.That(mask & LayerMasks.CaveWallMask, Is.Not.Zero);
            Assert.That(mask & LayerMasks.CaveDecorationMask, Is.Not.Zero);
            Assert.That(mask & LayerMasks.CaveBridgeMask, Is.Not.Zero);
            Assert.That(mask & LayerMasks.FloorRoofMask, Is.Zero);
            Assert.That(mask & LayerMasks.WallMask, Is.Zero);
            Assert.That(mask & LayerMasks.DecorationMask, Is.Zero);
            Assert.That(mask & LayerMasks.BridgeMask, Is.Zero);
        }

        [Test]
        public void MirrorEditMasksUseTerrainForCurrentLevel()
        {
            Assert.That(LayerMasks.GetMaskForTab(Editing.Tab.Mirror, 0), Is.EqualTo(LayerMasks.GroundMask));
            Assert.That(LayerMasks.GetMaskForTab(Editing.Tab.Mirror, -1), Is.EqualTo(LayerMasks.CaveMask));
        }

        [Test]
        public void PreserveOpaquePartialScopeRestoresOwnedAbsence()
        {
            foreach (RenderTarget surface in _fixture.Surface) WarmOwnedModel(surface.Renderer);
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, false, false)))
            {
                AssertEmptyPropertyBlock(_fixture.Surface[0].Renderer);
                AssertAbsentPropertyBlock(_fixture.Surface[1].Renderer);
                using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false))) { }
                AssertEmptyPropertyBlock(_fixture.Surface[0].Renderer);
            }
            foreach (RenderTarget surface in _fixture.Surface) AssertAbsentPropertyBlock(surface.Renderer);
            Assert.That(OwnedNullBlocks().Count, Is.EqualTo(3));
        }

        [Test]
        public void PreserveFullMapDoesNotAdmitNewOwnership()
        {
            Renderer renderer = _fixture.Surface[0].Renderer;
            Material material = _fixture.CreateMaterial("Warlander/ModelShader");
            renderer.sharedMaterial = material;
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false))) { }
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false)))
                AssertAbsentPropertyBlock(renderer);
            AssertEmptyPropertyBlock(renderer);
            Assert.That(OwnedNullBlocks().Count, Is.Zero);
            WarmOwnedModel(renderer);
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false))) AssertAbsentPropertyBlock(renderer);
            AssertAbsentPropertyBlock(renderer);
        }

        [TestCase(0, false)]
        [TestCase(2, false)]
        [TestCase(-1, false)]
        [TestCase(-1, true)]
        public void DisabledViewsFlushOwnedBlocksAndUseLegacyStateRepeatedly(int level, bool full)
        {
            Renderer renderer = _fixture.Surface[0].Renderer;
            WarmOwnedModel(renderer);
            for (int i = 0; i < 3; i++)
            {
                using (_fixture.Renderer.PrepareForCamera(new MapRenderView(level, full, false, ModelPropertyBlockRetention.Disabled))) { }
                AssertEmptyPropertyBlock(renderer);
                Assert.That(OwnedNullBlocks().Count, Is.Zero);
            }
        }

        [Test]
        public void EnabledUndergroundCannotRetainHiddenSurfaceOwnership()
        {
            Renderer renderer = _fixture.Surface[0].Renderer;
            WarmOwnedModel(renderer);
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(-1, true, false, ModelPropertyBlockRetention.Enabled))) { }
            AssertEmptyPropertyBlock(renderer);
            Assert.That(OwnedNullBlocks().Count, Is.Zero);
        }

        [Test]
        public void SingleSplitSingleContextsRestoreAdmissionWithoutALatch()
        {
            Renderer renderer = _fixture.Surface[0].Renderer;
            WarmOwnedModel(renderer);
            var splitFull = new MapRenderView(0, true, false, ModelPropertyBlockRetention.Disabled);
            var splitTop = new MapRenderView(0, false, false, ModelPropertyBlockRetention.Disabled);
            for (int i = 0; i < 3; i++)
            {
                using (_fixture.Renderer.PrepareForCamera(splitFull)) { }
                using (_fixture.Renderer.PrepareForCamera(splitTop)) { }
                AssertEmptyPropertyBlock(renderer);
                Assert.That(OwnedNullBlocks().Count, Is.Zero);
            }
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false, ModelPropertyBlockRetention.Enabled))) { }
            AssertAbsentPropertyBlock(renderer);
            Assert.That(OwnedNullBlocks().Count, Is.EqualTo(1));
        }

        [Test]
        public void NestedDisabledViewPreservesOuterOwnership()
        {
            Renderer renderer = _fixture.Surface[0].Renderer;
            WarmOwnedModel(renderer);
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false, ModelPropertyBlockRetention.Enabled)))
            {
                using (_fixture.Renderer.PrepareForCamera(new MapRenderView(1, false, false, ModelPropertyBlockRetention.Disabled)))
                    Assert.That(GetEffectiveBaseColor(renderer).r, Is.EqualTo(0.6f).Within(0.001f));
                AssertAbsentPropertyBlock(renderer);
            }
            AssertAbsentPropertyBlock(renderer);
            Assert.That(OwnedNullBlocks().Count, Is.EqualTo(1));
        }

        [Test]
        public void OwnedAbsenceSurvivesRepeatedCompatibleFullMapScopes()
        {
            Renderer renderer = _fixture.Surface[0].Renderer;
            WarmOwnedModel(renderer);
            for (int i = 0; i < 3; i++)
            {
                using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false, ModelPropertyBlockRetention.Enabled)))
                    AssertAbsentPropertyBlock(renderer);
                AssertAbsentPropertyBlock(renderer);
            }
        }

        [Test]
        public void DisabledPartialScopesReleaseAllOwnershipBeforeRepeatedViews()
        {
            foreach (RenderTarget surface in _fixture.Surface) WarmOwnedModel(surface.Renderer);
            for (int i = 0; i < 3; i++)
            {
                using (_fixture.Renderer.PrepareForCamera(new MapRenderView(1, false, false, ModelPropertyBlockRetention.Disabled)))
                {
                    Assert.That(GetEffectiveBaseColor(_fixture.Surface[0].Renderer).r, Is.EqualTo(0.6f).Within(0.001f));
                    AssertEmptyPropertyBlock(_fixture.Surface[1].Renderer);
                    AssertEmptyPropertyBlock(_fixture.Surface[2].Renderer);
                    Assert.That(_fixture.Surface[2].Renderer.forceRenderingOff, Is.True);
                }
                AssertEmptyPropertyBlock(_fixture.Surface[0].Renderer);
                AssertEmptyPropertyBlock(_fixture.Surface[1].Renderer);
                AssertEmptyPropertyBlock(_fixture.Surface[2].Renderer);
                Assert.That(OwnedNullBlocks().Count, Is.Zero);
            }
        }

        [Test]
        public void NestedFadedAndHiddenViewsRestoreOuterOwnedAbsence()
        {
            Renderer renderer = _fixture.Surface[0].Renderer;
            WarmOwnedModel(renderer);
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false, ModelPropertyBlockRetention.Enabled)))
            {
                using (_fixture.Renderer.PrepareForCamera(new MapRenderView(1, false, false)))
                    Assert.That(GetEffectiveBaseColor(renderer).r, Is.EqualTo(0.6f).Within(0.001f));
                AssertAbsentPropertyBlock(renderer);
                using (_fixture.Renderer.PrepareForCamera(new MapRenderView(-1, true, false, ModelPropertyBlockRetention.Enabled)))
                {
                    Assert.That(renderer.forceRenderingOff, Is.True);
                    AssertAbsentPropertyBlock(renderer);
                }
                Assert.That(renderer.forceRenderingOff, Is.False);
                AssertAbsentPropertyBlock(renderer);
            }
            AssertAbsentPropertyBlock(renderer);
        }

        [Test]
        public void InactiveIncompatibleMaterialRehydratesOtherOwnedModels()
        {
            Renderer ordinary = _fixture.Surface[0].Renderer;
            Renderer incompatible = _fixture.Surface[1].Renderer;
            WarmOwnedModel(ordinary);
            Material material = WarmOwnedModel(incompatible);
            incompatible.gameObject.SetActive(false);
            material.SetVector(ShaderPropertyIds.ShearY, new Vector4(0.1f, 0, 0, 0));
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false, ModelPropertyBlockRetention.Enabled)))
            {
                AssertEmptyPropertyBlock(ordinary);
                AssertEmptyPropertyBlock(incompatible);
            }
            AssertEmptyPropertyBlock(ordinary);
            Assert.That(OwnedNullBlocks().Count, Is.Zero);
            material.SetVector(ShaderPropertyIds.ShearY, Vector4.zero);
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false, ModelPropertyBlockRetention.Enabled))) { }
            AssertAbsentPropertyBlock(ordinary);
        }

        [Test]
        public void NestedScopesReleaseTemporaryMarkersBeforeLateOverrides()
        {
            Renderer renderer = _fixture.Surface[0].Renderer;
            WarmOwnedModel(renderer);
            var block = new MaterialPropertyBlock();
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false, ModelPropertyBlockRetention.Enabled)))
            {
                using (_fixture.Renderer.PrepareForCamera(new MapRenderView(1, false, false)))
                {
                    using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false, ModelPropertyBlockRetention.Enabled)))
                        Assert.That(GetEffectiveBaseColor(renderer), Is.EqualTo(Color.white));
                    Assert.That(GetEffectiveBaseColor(renderer).r, Is.EqualTo(0.6f).Within(0.001f));
                }
                AssertAbsentPropertyBlock(renderer);
                block.SetColor(ShaderPropertyIds.BaseColor, Color.cyan);
                renderer.SetPropertyBlock(block);
                using (_fixture.Renderer.PrepareForCamera(new MapRenderView(1, false, false))) { }
                Assert.That(GetBaseColor(renderer), Is.EqualTo(Color.cyan));
            }
            Assert.That(GetBaseColor(renderer), Is.EqualTo(Color.cyan));
            var scoped = (HashSet<Renderer>)typeof(MapLevelRenderer).GetField("_scopedPropertyBlocks",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(_fixture.Renderer);
            Assert.That(scoped.Count, Is.Zero);
            Assert.That(OwnedNullBlocks().Count, Is.Zero);
        }

        [Test]
        public void LateAttachedEmptyBlockRelinquishesOwnership()
        {
            Renderer renderer = _fixture.Surface[0].Renderer;
            WarmOwnedModel(renderer);
            renderer.SetPropertyBlock(new MaterialPropertyBlock());
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(1, false, false))) { }
            AssertEmptyPropertyBlock(renderer);
            Assert.That(OwnedNullBlocks().Count, Is.Zero);
        }

        [Test]
        public void GlobalAndIndexedOverridesRelinquishOwnedAbsence()
        {
            Renderer global = _fixture.Surface[0].Renderer;
            Renderer indexed = _fixture.Surface[1].Renderer;
            WarmOwnedModel(global);
            WarmOwnedModel(indexed);
            int valueId = Shader.PropertyToID("_OwnedNullExternalValue");
            var block = new MaterialPropertyBlock();
            block.SetColor(ShaderPropertyIds.BaseColor, Color.cyan);
            block.SetFloat(valueId, 31);
            global.SetPropertyBlock(block);
            block.SetColor(ShaderPropertyIds.BaseColor, Color.magenta);
            block.SetFloat(valueId, 47);
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false, ModelPropertyBlockRetention.Enabled)))
                indexed.SetPropertyBlock(block, 0);
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false, ModelPropertyBlockRetention.Enabled))) { }
            global.GetPropertyBlock(block);
            Assert.That(block.GetColor(ShaderPropertyIds.BaseColor), Is.EqualTo(Color.cyan));
            Assert.That(block.GetFloat(valueId), Is.EqualTo(31));
            indexed.GetPropertyBlock(block, 0);
            Assert.That(block.GetColor(ShaderPropertyIds.BaseColor), Is.EqualTo(Color.magenta));
            Assert.That(block.GetFloat(valueId), Is.EqualTo(47));
            Assert.That(OwnedNullBlocks().Count, Is.Zero);
            indexed.SetPropertyBlock(null, 0);
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false, ModelPropertyBlockRetention.Enabled))) { }
            AssertEmptyPropertyBlock(indexed);
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false, ModelPropertyBlockRetention.Enabled))) { }
            AssertAbsentPropertyBlock(indexed);
        }

        [Test]
        public void OwnedShaderAndMaterialSlotFallbackRehydratesOnce()
        {
            Renderer renderer = _fixture.Surface[0].Renderer;
            Material material = WarmOwnedModel(renderer);
            Material other = _fixture.CreateMaterial("Universal Render Pipeline/Unlit");
            Shader shader = material.shader;
            material.shader = other.shader;
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false, ModelPropertyBlockRetention.Enabled))) AssertEmptyPropertyBlock(renderer);
            AssertEmptyPropertyBlock(renderer);
            Assert.That(OwnedNullBlocks().Count, Is.Zero);
            material.shader = shader;
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false, ModelPropertyBlockRetention.Enabled))) { }
            AssertAbsentPropertyBlock(renderer);
            renderer.sharedMaterials = new[] { material, other };
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false, ModelPropertyBlockRetention.Enabled))) AssertEmptyPropertyBlock(renderer);
            AssertEmptyPropertyBlock(renderer);
            Assert.That(OwnedNullBlocks().Count, Is.Zero);
        }

        [Test]
        public void DestroyedOwnershipDoesNotTransferToAReplacementRenderer()
        {
            Renderer renderer = _fixture.Surface[0].Renderer;
            Material material = WarmOwnedModel(renderer);
            UnityEngine.Object.DestroyImmediate(renderer.gameObject);
            var replacement = new GameObject("Owned-null replacement", typeof(MeshRenderer));
            replacement.transform.SetParent(_fixture.Surface[0].Root);
            renderer = replacement.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false, ModelPropertyBlockRetention.Enabled))) { }
            AssertEmptyPropertyBlock(renderer);
            Assert.That(OwnedNullBlocks().Count, Is.Zero);
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false, ModelPropertyBlockRetention.Enabled))) { }
            AssertAbsentPropertyBlock(renderer);
            Assert.That(OwnedNullBlocks().Count, Is.EqualTo(1));
        }

        [Test]
        public void RenderingCameraPredicateTracksSingleSplitSingleAndCameraEnablement()
        {
            var coordinatorObject = new GameObject("Retention camera coordinator");
            var firstObject = new GameObject("First retention camera");
            var secondObject = new GameObject("Second retention camera");
            coordinatorObject.SetActive(false);
            firstObject.SetActive(false);
            secondObject.SetActive(false);
            try
            {
                var coordinator = coordinatorObject.AddComponent<Cameras.CameraCoordinator>();
                var first = firstObject.AddComponent<Cameras.MultiCamera>();
                var second = secondObject.AddComponent<Cameras.MultiCamera>();
                var attached = typeof(Cameras.MultiCamera).GetProperty("AttachedCamera");
                attached.GetSetMethod(true).Invoke(first, new object[] { firstObject.GetComponent<Camera>() });
                attached.GetSetMethod(true).Invoke(second, new object[] { secondObject.GetComponent<Camera>() });
                typeof(Cameras.CameraCoordinator).GetField("_cameras",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(coordinator, new[] { first, second });
                firstObject.SetActive(true);
                Assert.That(coordinator.IsOnlyRenderingCamera(first), Is.True);
                secondObject.SetActive(true);
                second.enabled = false;
                Assert.That(coordinator.IsOnlyRenderingCamera(first), Is.False);
                second.AttachedCamera.enabled = false;
                Assert.That(coordinator.IsOnlyRenderingCamera(first), Is.True);
                second.AttachedCamera.enabled = true;
                secondObject.SetActive(false);
                Assert.That(coordinator.IsOnlyRenderingCamera(first), Is.True);
                first.AttachedCamera.enabled = false;
                Assert.That(coordinator.IsOnlyRenderingCamera(first), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(coordinatorObject);
                UnityEngine.Object.DestroyImmediate(firstObject);
                UnityEngine.Object.DestroyImmediate(secondObject);
            }
        }

        private Material WarmOwnedModel(Renderer renderer)
        {
            Material material = _fixture.CreateMaterial("Warlander/ModelShader");
            renderer.sharedMaterial = material;
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false, ModelPropertyBlockRetention.Enabled))) { }
            using (_fixture.Renderer.PrepareForCamera(new MapRenderView(0, true, false, ModelPropertyBlockRetention.Enabled))) { }
            AssertAbsentPropertyBlock(renderer);
            return material;
        }

        private HashSet<Renderer> OwnedNullBlocks()
        {
            return (HashSet<Renderer>)typeof(MapLevelRenderer).GetField("_ownedNullBlocks",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(_fixture.Renderer);
        }

        private static void AssertAbsentPropertyBlock(Renderer renderer)
        {
            Assert.That(renderer.HasPropertyBlock(), Is.False);
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            Assert.That(block.isEmpty, Is.True);
        }

        private static void AssertEmptyPropertyBlock(Renderer renderer)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            Assert.That(renderer.HasPropertyBlock(), Is.True);
            Assert.That(block.isEmpty, Is.True);
        }

        private static Color GetBaseColor(Renderer renderer)
        {
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            return propertyBlock.GetColor(ShaderPropertyIds.BaseColor);
        }

        private static Color GetEffectiveBaseColor(Renderer renderer)
        {
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            if (propertyBlock.HasColor(ShaderPropertyIds.BaseColor))
                return propertyBlock.GetColor(ShaderPropertyIds.BaseColor);

            Material material = renderer.sharedMaterial;
            return material && material.HasProperty(ShaderPropertyIds.BaseColor)
                ? material.GetColor(ShaderPropertyIds.BaseColor)
                : Color.white;
        }

        private static float GetCaveOverview(Renderer renderer)
        {
            var propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            return propertyBlock.GetFloat(ShaderPropertyIds.CaveOverview);
        }

        private sealed class RenderFixture : IDisposable
        {
            private readonly GameObject _container = new GameObject("Map rendering test");
            private readonly List<Material> _materials = new List<Material>();

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
                foreach (Material material in _materials)
                    UnityEngine.Object.DestroyImmediate(material);
            }

            public Material CreateMaterial(string shaderName)
            {
                Shader shader = Shader.Find(shaderName);
                Assert.That(shader, Is.Not.Null);
                var material = new Material(shader);
                _materials.Add(material);
                return material;
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
