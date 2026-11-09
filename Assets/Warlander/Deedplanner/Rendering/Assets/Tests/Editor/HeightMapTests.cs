using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Warlander.Deedplanner.Logging;
using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Rendering.Assets.Tests
{
    public class HeightMapTests
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
        public async Task SharedPackedDataKeepsIndependentHeightScalesUnderCommaDecimalCultureAsync()
        {
            Texture2D packed = Track(new Texture2D(2, 2, TextureFormat.RGBA32, false, true));
            var completion = new TaskCompletionSource<Texture2D>();
            var loader = new RecordingTextureLoader { LinearTask = completion.Task };
            var factory = new TextureReferenceFactory(loader, new SilentLogger());
            XmlDocument document = Document("<tex location='first.dds' occlusion='shared_d.dds'/>"
                + "<override mesh='deck' texture='first.dds' heightScale='0.035'/>"
                + "<rock tex='second.dds' occlusion='shared_d.dds' heightScale='0.02'/>"
                + "<roof tex='third.dds' occlusion='shared_d.dds' heightScale='0.0075'/>");
            CultureInfo previousCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                factory.LoadTextureDefinitions(document);
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }

            TextureReference first = factory.GetTextureReference("first.dds");
            TextureReference second = factory.GetTextureReference("second.dds");
            TextureReference third = factory.GetTextureReference("third.dds");
            Assert.That(first.HeightScale, Is.EqualTo(0.035f));
            Assert.That(second.HeightScale, Is.EqualTo(0.02f));
            Assert.That(third.HeightScale, Is.EqualTo(0.0075f));
            Task<Texture2D> firstLoad = first.LoadOrGetOcclusionAsync();
            Assert.That(second.LoadOrGetOcclusionAsync(), Is.SameAs(firstLoad));
            Assert.That(third.LoadOrGetOcclusionAsync(), Is.SameAs(firstLoad));
            Assert.That(loader.LinearLoads, Is.EqualTo(1));
            Assert.That(loader.DiffuseLoads, Is.Zero);
            completion.SetResult(packed);
            Assert.That(await firstLoad, Is.SameAs(packed));
            Assert.That(await second.LoadOrGetOcclusionAsync(), Is.SameAs(packed));
            Assert.That(loader.LinearLoads, Is.EqualTo(1));
        }

        [Test]
        public async Task DefaultAndExplicitZeroHeightNeedNoPackedTextureAsync()
        {
            var loader = new RecordingTextureLoader();
            var factory = new TextureReferenceFactory(loader, new SilentLogger());
            factory.LoadTextureDefinitions(Document("<tex location='zero.dds' heightScale='0'/>"));
            TextureReference zero = factory.GetTextureReference("zero.dds");
            TextureReference unmapped = factory.GetTextureReference("unmapped.dds");

            Assert.That(zero.HeightScale, Is.Zero);
            Assert.That(unmapped.HeightScale, Is.Zero);
            Assert.That(await zero.LoadOrGetOcclusionAsync(), Is.Null);
            Assert.That(await unmapped.LoadOrGetOcclusionAsync(), Is.Null);
            Assert.That(loader.LinearLoads, Is.Zero);
            Assert.That(loader.DiffuseLoads, Is.Zero);
        }

        [TestCase("-0.01", true)]
        [TestCase("NaN", true)]
        [TestCase("Infinity", true)]
        [TestCase("-Infinity", true)]
        [TestCase("0.035", false)]
        public void InvalidHeightOrMissingPackedSourceIsRejected(string scale, bool hasPackedSource)
        {
            var factory = new TextureReferenceFactory(new RecordingTextureLoader(), new SilentLogger());
            string source = hasPackedSource ? " occlusion='detail.dds'" : "";
            XmlDocument document = Document("<tex location='surface.dds' heightScale='" + scale + "'" + source + "/>");

            Assert.Throws<FormatException>(() => factory.LoadTextureDefinitions(document));
        }

        [Test]
        public void TerrainHeightRequiresLoadedDataAndResetsWithoutDisturbingSurfaceProperties()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Materials/Terrain Shader.shadergraph");
            Assert.That(shader, Is.Not.Null);
            Material material = Track(new Material(shader));
            var reference = new TextureReference(new RecordingTextureLoader(), "surface.dds",
                specularRange: new Vector2(0.1f, 0.7f), heightScale: 0.035f);
            using var properties = new TerrainSurfaceProperties(2);
            properties.Set(0, reference, true, true);
            properties.ApplyTo(material);
            var heights = material.GetTexture("_HeightProperties") as Texture2DArray;
            var surfaces = material.GetTexture("_SurfaceProperties") as Texture2DArray;

            Assert.That(heights, Is.Not.Null);
            Assert.That(surfaces, Is.Not.Null);
            Assert.That(heights.format, Is.EqualTo(TextureFormat.RGBAHalf));
            Assert.That(heights.isDataSRGB, Is.False);
            Assert.That(heights.GetPixels(0)[0].r, Is.EqualTo(0.035f).Within(0.00003f));
            Color previousSurface = surfaces.GetPixels(0)[0];
            properties.Set(0, reference, true);
            Assert.That(heights.GetPixels(0)[0].r, Is.Zero);
            Assert.That(surfaces.GetPixels(0)[0], Is.EqualTo(previousSurface));
            properties.Set(1, new TextureReference(new RecordingTextureLoader(), "unmapped.dds"), false, true);
            Assert.That(heights.GetPixels(1)[0].r, Is.Zero);
        }

        [TestCase(0f, true, 0f, true)]
        [TestCase(0.035f, false, 0f, true)]
        [TestCase(0.035f, true, 0.035f, true)]
        [TestCase(0.035f, true, 0f, false)]
        public async Task ModelOverrideClearsStaleHeightBeforeLoadingAndRequiresPackedDataAsync(float scale,
            bool packedAvailable, float expectedScale, bool heightDetail)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Materials/ModelShader.shadergraph");
            Assert.That(shader, Is.Not.Null);
            Material material = Track(new Material(shader));
            material.SetFloat("_HeightScale", 0.07f);
            Texture2D diffuse = Track(new Texture2D(2, 2));
            Texture2D packed = packedAvailable ? Track(new Texture2D(2, 2, TextureFormat.RGBA32, false, true)) : null;
            var completion = new TaskCompletionSource<Texture2D>();
            var loader = new RecordingTextureLoader
            {
                DiffuseTask = completion.Task,
                LinearTask = Task.FromResult(packed)
            };
            var packedReference = new TextureReference(loader, "detail.dds", linearData: true);
            var reference = new TextureReference(loader, "surface.dds", occlusionReference: packedReference,
                heightScale: scale);

            Task apply = ModelMaterialTextures.ApplyAsync(material, reference, heightDetail);

            Assert.That(apply.IsCompleted, Is.False);
            Assert.That(material.GetFloat("_HeightScale"), Is.Zero);
            completion.SetResult(diffuse);
            await apply;
            Assert.That(material.GetFloat("_HeightScale"), Is.EqualTo(expectedScale));
            Assert.That(loader.DiffuseLoads, Is.EqualTo(1));
            Assert.That(loader.LinearLoads, Is.EqualTo(1));
        }

        [Test]
        public void AuthoredHeightScalesAreConsistentAndReferenceExistingPackedTextures()
        {
            var document = new XmlDocument();
            document.Load(Path.Combine(Application.streamingAssetsPath, "objects.xml"));
            var packedSources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (XmlElement element in document.SelectNodes("//*[@occlusion]"))
                packedSources[TextureLocation(element)] = element.GetAttribute("occlusion");
            var scales = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            int positiveDefinitions = 0;
            foreach (XmlElement element in document.SelectNodes("//*[@heightScale]"))
            {
                string location = TextureLocation(element);
                float scale = float.Parse(element.GetAttribute("heightScale"), CultureInfo.InvariantCulture);
                Assert.That(float.IsNaN(scale) || float.IsInfinity(scale) || scale < 0, Is.False, location);
                if (scales.TryGetValue(location, out float previous))
                    Assert.That(scale, Is.EqualTo(previous), location);
                scales[location] = scale;
                if (scale == 0) continue;
                positiveDefinitions++;
                Assert.That(packedSources.TryGetValue(location, out string packed), Is.True, location);
                Assert.That(File.Exists(Path.Combine(Application.streamingAssetsPath, packed)), Is.True, packed);
                Assert.That(File.Exists(Path.Combine(Application.streamingAssetsPath, location)), Is.True, location);
            }
            Assert.That(positiveDefinitions, Is.GreaterThan(0));
        }

        private static string TextureLocation(XmlElement element)
        {
            return element.Name == "tex" ? element.GetAttribute("location")
                : element.Name == "override" ? element.GetAttribute("texture") : element.GetAttribute("tex");
        }

        private static XmlDocument Document(string definitions)
        {
            var document = new XmlDocument();
            document.LoadXml("<objects>" + definitions + "</objects>");
            return document;
        }

        private T Track<T>(T value) where T : Object
        {
            _createdObjects.Add(value);
            return value;
        }

        private sealed class RecordingTextureLoader : ITextureLoader
        {
            public Task<Texture2D> DiffuseTask { get; set; } = Task.FromResult<Texture2D>(null);
            public Task<Texture2D> LinearTask { get; set; } = Task.FromResult<Texture2D>(null);
            public int DiffuseLoads { get; private set; }
            public int LinearLoads { get; private set; }

            public Task<Texture2D> LoadTextureAsync(string location, bool readable, bool normalMap = false,
                bool linearData = false, Func<bool> canDiscardReadableData = null)
            {
                if (normalMap) return Task.FromResult<Texture2D>(null);
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
