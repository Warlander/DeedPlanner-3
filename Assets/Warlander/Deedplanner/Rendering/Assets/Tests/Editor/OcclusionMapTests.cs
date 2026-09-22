using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Domain.Entities.Grounds;
using Warlander.Deedplanner.Logging;
using Warlander.Render;
using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Rendering.Assets.Tests
{
    public class OcclusionMapTests
    {
        private readonly List<Object> _createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _createdObjects)
            {
                if (created) Object.DestroyImmediate(created);
            }
            _createdObjects.Clear();
        }

        [Test]
        public async Task LinearDataPngKeepsGreenAndAlphaUnchanged()
        {
            Texture2D source = Track(new Texture2D(2, 2, TextureFormat.RGBA32, false, true));
            Color32[] pixels =
            {
                new Color32(20, 40, 0, 255), new Color32(80, 120, 0, 255),
                new Color32(150, 200, 0, 180), new Color32(240, 255, 0, 0)
            };
            source.SetPixels32(pixels);
            source.Apply();
            string path = Path.Combine(Path.GetTempPath(), "dp3-occlusion-" + Guid.NewGuid().ToString("N") + ".png");
            try
            {
                File.WriteAllBytes(path, source.EncodeToPNG());
                Texture2D loaded = Track(await new GenericTextureLoader(new SilentLogger())
                    .LoadTextureAsync(path, true, linearData: true));
                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded.isDataSRGB, Is.False);
                CollectionAssert.AreEqual(pixels, loaded.GetPixels32());
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void OcclusionOnlyDefinitionPreservesExistingSpecularRange()
        {
            var factory = CreateFactory(new RecordingTextureLoader(),
                "<tex location='paving.dds' normal='paving_n.dds' specularMax='0.42'/>"
                + "<rock tex='paving.dds' occlusion='paving_d.dds'/>");
            Assert.That(factory.GetTextureReference("paving.dds").SpecularRange, Is.EqualTo(new Vector2(0, 0.42f)));
        }

        [Test]
        public async Task OcclusionSharesPendingLoadButNotNormalOrDiffuseCache()
        {
            Texture2D data = Solid(new Color(0.2f, 0.4f, 0, 1));
            Texture2D normal = Solid(new Color(0.5f, 0.5f, 1, 1));
            Texture2D diffuse = Solid(Color.red);
            var completion = new TaskCompletionSource<Texture2D>();
            var loader = new RecordingTextureLoader
            {
                DiffuseTask = Task.FromResult(diffuse),
                NormalTask = Task.FromResult(normal),
                LinearTask = completion.Task
            };
            TextureReferenceFactory factory = CreateFactory(loader,
                "<tex location='first.dds' normal='shared.dds' occlusion='shared.dds'/>"
                + "<tex location='second.dds' occlusion='shared.dds'/>");
            TextureReference first = factory.GetTextureReference("first.dds");
            TextureReference second = factory.GetTextureReference("second.dds");
            Task<Texture2D> firstLoad = first.LoadOrGetOcclusionAsync();
            Task<Texture2D> secondLoad = second.LoadOrGetOcclusionAsync();

            Assert.That(secondLoad, Is.SameAs(firstLoad));
            Assert.That(loader.LinearLoads, Is.EqualTo(1));
            Assert.That(await factory.GetTextureReference("unmapped.dds").LoadOrGetOcclusionAsync(), Is.Null);
            Assert.That(loader.LinearLoads, Is.EqualTo(1));
            Assert.That(await first.LoadOrGetNormalAsync(), Is.SameAs(normal));
            Assert.That(await factory.GetTextureReference("shared.dds").LoadOrGetTextureAsync(), Is.SameAs(diffuse));
            Assert.That(loader.NormalLoads, Is.EqualTo(1));
            Assert.That(loader.DiffuseLoads, Is.EqualTo(1));
            completion.SetResult(data);
            Assert.That(await firstLoad, Is.SameAs(data));
            Assert.That(await secondLoad, Is.SameAs(data));
            Assert.That(await first.LoadOrGetOcclusionAsync(), Is.SameAs(data));
            Assert.That(loader.LinearLoads, Is.EqualTo(1));
        }

        [TestCase("<tex location='surface.dds' occlusion='detail.dds'/>")]
        [TestCase("<override mesh='body' texture='surface.dds' occlusion='detail.dds'/>")]
        [TestCase("<rock tex='surface.dds' occlusion='detail.dds'/>")]
        [TestCase("<roof tex='surface.dds' occlusion='detail.dds'/>")]
        public async Task XmlOcclusionDoesNotRequireNormalDefinition(string definition)
        {
            Texture2D data = Solid(Color.green);
            var loader = new RecordingTextureLoader { LinearTask = Task.FromResult(data) };
            TextureReference reference = CreateFactory(loader, definition).GetTextureReference("surface.dds");

            Assert.That(await reference.LoadOrGetOcclusionAsync(), Is.SameAs(data));
            Assert.That(await reference.LoadOrGetNormalAsync(), Is.Null);
            Assert.That(loader.LinearLoads, Is.EqualTo(1));
            Assert.That(loader.NormalLoads, Is.Zero);
            Assert.That(loader.DiffuseLoads, Is.Zero);
        }

        [Test]
        public void ObjectsXmlOcclusionDefinitionsAreConsistentAndFilesExist()
        {
            var document = new XmlDocument();
            document.Load(Path.Combine(Application.streamingAssetsPath, "objects.xml"));
            var locations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (XmlElement element in document.SelectNodes("//*[@occlusion]"))
            {
                string location = element.Name == "tex" ? element.GetAttribute("location")
                    : element.Name == "override" ? element.GetAttribute("texture") : element.GetAttribute("tex");
                string occlusion = element.GetAttribute("occlusion");
                Assert.That(File.Exists(Path.Combine(Application.streamingAssetsPath, location)), Is.True, location);
                Assert.That(File.Exists(Path.Combine(Application.streamingAssetsPath, occlusion)), Is.True, occlusion);
                if (locations.TryGetValue(location, out string previous))
                    Assert.That(occlusion, Is.EqualTo(previous), location);
                locations[location] = occlusion;
            }
            Assert.That(locations.Count, Is.GreaterThan(0));
        }

        [Test]
        public void GroundOcclusionSlicesMatchDiffuseIndicesAndUseWhiteFallback()
        {
            Texture2D diffuse = Solid(Color.red);
            Texture2D occlusion = Solid(new Color(0.9f, 0.25f, 0, 1));
            var loader = new RecordingTextureLoader();
            var mapped = new TextureReference(loader, "mapped.dds");
            var unmapped = new TextureReference(loader, "unmapped.dds");
            var catalog = new Database();
            catalog.AddGround(new GroundData("Test", "gr", Array.Empty<string[]>(), mapped, unmapped, false));
            using var arrays = new GroundTextureArray(catalog);

            Assert.That(arrays.TryGetOrAdd(unmapped, diffuse, null, out int unmappedIndex), Is.True);
            Assert.That(arrays.TryGetOrAdd(mapped, diffuse, null, out int mappedIndex, occlusion), Is.True);
            Assert.That(arrays.TryGetOrAdd(mapped, diffuse, null, out int repeatedIndex, occlusion), Is.True);
            Assert.That(repeatedIndex, Is.EqualTo(mappedIndex));
            Assert.That(mappedIndex, Is.Not.EqualTo(unmappedIndex));
            var data = (IndexedTextureArray<TextureReference>)typeof(GroundTextureArray)
                .GetField("_occlusions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(arrays);
            Assert.That(data.GetTextureIndex(mapped), Is.EqualTo(mappedIndex));
            Assert.That(data.GetTextureIndex(unmapped), Is.EqualTo(unmappedIndex));
            Assert.That(data.TextureArray.isDataSRGB, Is.False);
            Assert.That(data.TextureArray.GetPixels(mappedIndex)[0].g, Is.EqualTo(0.25f).Within(0.02f));
            Assert.That(data.TextureArray.GetPixels(unmappedIndex)[0].g, Is.EqualTo(1).Within(0.02f));
        }

        [Test]
        public async Task TextureOverrideClearsPreviousOcclusionWhileDiffuseIsPending()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Materials/ModelShader.shadergraph");
            Assert.That(shader, Is.Not.Null);
            Material material = Track(new Material(shader));
            Texture2D diffuse = Solid(Color.red);
            Texture2D data = Solid(new Color(0, 0.2f, 0, 1));
            var loader = new RecordingTextureLoader
            {
                DiffuseTask = Task.FromResult(diffuse), LinearTask = Task.FromResult(data)
            };
            var occlusion = new TextureReference(loader, "detail.dds", linearData: true);
            var mapped = new TextureReference(loader, "mapped.dds", occlusionReference: occlusion);
            await ModelMaterialTextures.ApplyAsync(material, mapped);
            Assert.That(material.GetTexture("_OcclusionMap"), Is.SameAs(data));
            Assert.That(material.GetFloat("_OcclusionStrength"), Is.EqualTo(0.35f).Within(0.0001f));

            var completion = new TaskCompletionSource<Texture2D>();
            loader.DiffuseTask = completion.Task;
            Task apply = ModelMaterialTextures.ApplyAsync(material, new TextureReference(loader, "unmapped.dds"));
            Assert.That(apply.IsCompleted, Is.False);
            Assert.That(material.GetTexture("_OcclusionMap"), Is.Null);
            Assert.That(material.GetFloat("_OcclusionStrength"), Is.Zero);
            completion.SetResult(diffuse);
            await apply;
            Assert.That(material.GetTexture("_OcclusionMap"), Is.Null);
            Assert.That(material.GetFloat("_OcclusionStrength"), Is.Zero);
            Assert.That(loader.LinearLoads, Is.EqualTo(1));
        }

        private static TextureReferenceFactory CreateFactory(ITextureLoader loader, string definitions)
        {
            var document = new XmlDocument();
            document.LoadXml("<objects>" + definitions + "</objects>");
            var factory = new TextureReferenceFactory(loader, new SilentLogger());
            factory.LoadTextureDefinitions(document);
            return factory;
        }

        private Texture2D Solid(Color color)
        {
            Texture2D texture = Track(new Texture2D(2, 2, TextureFormat.RGBA32, false, true));
            texture.SetPixels(new[] { color, color, color, color });
            texture.Apply();
            return texture;
        }

        private T Track<T>(T value) where T : Object
        {
            _createdObjects.Add(value);
            return value;
        }

        private sealed class RecordingTextureLoader : ITextureLoader
        {
            public Task<Texture2D> DiffuseTask { get; set; } = Task.FromResult<Texture2D>(null);
            public Task<Texture2D> NormalTask { get; set; } = Task.FromResult<Texture2D>(null);
            public Task<Texture2D> LinearTask { get; set; } = Task.FromResult<Texture2D>(null);
            public int DiffuseLoads { get; private set; }
            public int NormalLoads { get; private set; }
            public int LinearLoads { get; private set; }

            public Task<Texture2D> LoadTextureAsync(string location, bool readable, bool normalMap = false, bool linearData = false)
            {
                if (normalMap)
                {
                    NormalLoads++;
                    return NormalTask;
                }
                if (linearData)
                {
                    LinearLoads++;
                    return LinearTask;
                }
                DiffuseLoads++;
                return DiffuseTask;
            }
        }

        private sealed class SilentLogger : ICategoryLogger
        {
            public void Message(string message) { }
            public void Warning(string message) { }
            public void Error(string message) { }
            public void Exception(Exception exception) { }
            public void Write(LogType type, string message) { }
        }
    }
}
