using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using NUnit.Framework;
using UnityEngine;
using Warlander.Deedplanner.Bridges;
using Warlander.Deedplanner.Domain.Entities.Caves;
using Warlander.Deedplanner.Logging;
using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Rendering.Assets.Tests
{
    public class SurfaceMaterialTests
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
        public void Dxt5NormalPreservesSpecularAlphaThroughVerticalFlipAndGreenCorrection()
        {
            MethodInfo decode = typeof(DDSTextureLoader).GetMethod("LoadTextureDxt", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(decode, Is.Not.Null);
            Texture2D texture = Track((Texture2D)decode.Invoke(new DDSTextureLoader(new SilentLogger()),
                new object[] { CreateDxt5Bytes(), true, true, false, null }));

            Assert.That(texture.isDataSRGB, Is.False);
            Color32[] pixels = texture.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                Assert.That(pixels[i].r, Is.EqualTo(132).Within(8));
                Assert.That(pixels[i].g, Is.EqualTo(i < 8 ? 0 : 255).Within(8));
                Assert.That(pixels[i].b, Is.EqualTo(255).Within(8));
                Assert.That(pixels[i].a, Is.EqualTo(i < 8 ? 40 : 200).Within(2));
            }
        }

        [Test]
        public async Task PngNormalKeepsLinearSpecularAlphaIncludingZero()
        {
            Texture2D source = Track(new Texture2D(2, 2, TextureFormat.RGBA32, false, true));
            Color32[] pixels =
            {
                new Color32(128, 40, 240, 0), new Color32(120, 70, 250, 48),
                new Color32(130, 90, 245, 170), new Color32(125, 110, 235, 255)
            };
            source.SetPixels32(pixels);
            source.Apply();
            string path = Path.Combine(Path.GetTempPath(), "dp3-normal-" + Guid.NewGuid().ToString("N") + ".png");
            try
            {
                File.WriteAllBytes(path, source.EncodeToPNG());
                Texture2D loaded = Track(await new GenericTextureLoader(new SilentLogger()).LoadTextureAsync(path, true, true));
                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded.isDataSRGB, Is.False);
                Color32[] actual = loaded.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                {
                    Assert.That(actual[i].r, Is.EqualTo(pixels[i].r));
                    Assert.That(actual[i].g, Is.EqualTo(255 - pixels[i].g));
                    Assert.That(actual[i].b, Is.EqualTo(pixels[i].b));
                    Assert.That(actual[i].a, Is.EqualTo(pixels[i].a));
                }
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public async Task AggregateDdsPredicateControlsOptionalCpuRelease(bool readable, bool discard)
        {
            string path = Path.Combine(Path.GetTempPath(), "dp3-callback-" + Guid.NewGuid().ToString("N") + ".dds");
            try
            {
                File.WriteAllBytes(path, CreateDxt5Bytes());
                var loader = new AggregateTextureLoader(new SilentLogger());
                Texture2D expected = Track(await loader.LoadTextureAsync(path, true, true));
                int callbacks = 0;
                Texture2D actual = Track(await loader.LoadTextureAsync(path, readable, true, false, () =>
                {
                    callbacks++;
                    return discard;
                }));
                Assert.That(callbacks, Is.EqualTo(readable ? 0 : 1));
                Assert.That(actual.format, Is.EqualTo(expected.format));
                Assert.That(actual.mipmapCount, Is.EqualTo(expected.mipmapCount));
                Assert.That(actual.isReadable, Is.EqualTo(readable || !discard));
                if (actual.isReadable) CollectionAssert.AreEqual(expected.GetPixels32(), actual.GetPixels32());
                Texture2D direct = Track(await loader.LoadTextureAsync(path, false, true));
                Assert.That(direct.isReadable, Is.False);
            }
            finally { File.Delete(path); }
        }

        [Test]
        public async Task AggregatePngIgnoresDdsDiscardPredicateAndKeepsExistingReadability()
        {
            Texture2D source = Track(new Texture2D(2, 2, TextureFormat.RGBA32, false, true));
            source.SetPixels(new[] { Color.red, Color.green, Color.blue, Color.clear }); source.Apply();
            string path = Path.Combine(Path.GetTempPath(), "dp3-callback-" + Guid.NewGuid().ToString("N") + ".png");
            try
            {
                File.WriteAllBytes(path, source.EncodeToPNG());
                Texture2D actual = Track(await new AggregateTextureLoader(new SilentLogger()).LoadTextureAsync(path, false, true, false,
                    () => throw new InvalidOperationException("PNG must not evaluate the DDS discard predicate.")));
                Assert.That(actual.isReadable, Is.False);
                Assert.That(actual.isDataSRGB, Is.False);
            }
            finally { File.Delete(path); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task CatalogSpriteRolesPreserveSharedRenderingReferenceWithoutEagerSprites(bool cave)
        {
            Texture2D texture = Track(new Texture2D(2, 2));
            var loader = new FixedTextureLoader(texture);
            var factory = new TextureReferenceFactory(loader, new SilentLogger());
            TextureReference reference = factory.GetTextureReference("shared.dds");
            if (cave) new CaveData(reference, "Cave", "c", new string[0][], true, true, false);
            else new BridgePavementData("Pavement", "p", reference, null);
            TextureReference renderingAlias = factory.GetTextureReference("shared.dds");

            Assert.That(renderingAlias, Is.SameAs(reference));
            Assert.That(await renderingAlias.LoadForRenderingAsync(), Is.SameAs(texture));
            Assert.That(loader.CanDiscardReadableData(), Is.False);
            Assert.That(Array.Exists(Resources.FindObjectsOfTypeAll<Sprite>(), sprite => sprite.texture == texture), Is.False);
        }

        [Test]
        public async Task SharedNormalRetainsSeparateAlbedoRangesUnderCommaDecimalCulture()
        {
            Texture2D normal = Track(new Texture2D(2, 2));
            var loader = new FixedTextureLoader(normal);
            var factory = new TextureReferenceFactory(loader, new SilentLogger());
            var document = new XmlDocument();
            document.LoadXml("<objects><tex location='planks.dds' normal='shared_n.dds' specularMin='0.1' specularMax='0.86'/>"
                + "<rock tex='tarred.dds' normal='shared_n.dds' specularMax='0.94'/>"
                + "<roof tex='default.dds' normal='shared_n.dds'/></objects>");
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

            TextureReference first = factory.GetTextureReference("planks.dds");
            TextureReference second = factory.GetTextureReference("tarred.dds");
            Assert.That(first.SpecularRange, Is.EqualTo(new Vector2(0.1f, 0.86f)));
            Assert.That(second.SpecularRange, Is.EqualTo(new Vector2(0, 0.94f)));
            Assert.That(factory.GetTextureReference("default.dds").SpecularRange, Is.EqualTo(new Vector2(0, 1)));
            Assert.That(await first.LoadOrGetNormalAsync(), Is.SameAs(normal));
            Assert.That(await second.LoadOrGetNormalAsync(), Is.SameAs(normal));
            Assert.That(loader.Loads, Is.EqualTo(1));
        }

        [Test]
        public async Task TerrainPresenceDoesNotTreatZeroSpecularAlphaAsMissingNormal()
        {
            Texture2D normal = Track(new Texture2D(1, 1, TextureFormat.RGBA32, false, true));
            normal.SetPixel(0, 0, new Color(0.5f, 0.5f, 1, 0));
            normal.Apply();
            var loader = new FixedTextureLoader(normal);
            var normalReference = new TextureReference(loader, "normal.dds", normalMap: true);
            var reference = new TextureReference(loader, "surface.dds", normalReference: normalReference,
                specularRange: new Vector2(0.1f, 0.8f));
            using var properties = new TerrainSurfaceProperties(3);
            properties.Set(2, reference, await reference.LoadOrGetNormalAsync() != null);
            properties.Set(0, reference, false);

            var array = (Texture2DArray)typeof(TerrainSurfaceProperties)
                .GetField("_properties", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(properties);
            Assert.That(array.isDataSRGB, Is.False);
            Assert.That(array.filterMode, Is.EqualTo(FilterMode.Point));
            Color mapped = array.GetPixels(2)[0];
            Assert.That(mapped.r, Is.EqualTo(0.1f).Within(0.004f));
            Assert.That(mapped.g, Is.EqualTo(0.8f).Within(0.004f));
            Assert.That(mapped.b, Is.EqualTo(0.35f).Within(0.004f));
            Assert.That(mapped.a, Is.EqualTo(1));
            Assert.That(array.GetPixels(0)[0].b, Is.Zero);
            Assert.That(array.GetPixels(0)[0].a, Is.Zero);

            properties.Set(2, reference, false);
            Assert.That(array.GetPixels(2)[0].b, Is.Zero);
            Assert.That(array.GetPixels(2)[0].a, Is.Zero);
        }

        [TestCase(-10f, 0f)]
        [TestCase(0f, 0f)]
        [TestCase(30f, 0.5f)]
        [TestCase(510f, 0.75f)]
        public void MaterialShininessControlsSmoothnessIndependentlyOfSpecularIntensity(float exponent, float expected)
        {
            MaterialMetadata dark = ReadMaterial(exponent, 0);
            MaterialMetadata bright = ReadMaterial(exponent, 1);
            Assert.That(dark.Glossiness, Is.EqualTo(expected).Within(0.0001f));
            Assert.That(bright.Glossiness, Is.EqualTo(dark.Glossiness));
        }

        private static MaterialMetadata ReadMaterial(float exponent, float intensity)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.ASCII, true);
            foreach (string value in new[] { "texture.dds", "material" })
            {
                byte[] bytes = Encoding.ASCII.GetBytes(value);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
            writer.Write(true);
            writer.Write(false);
            writer.Write(true);
            writer.Write(exponent);
            writer.Write(true);
            for (int i = 0; i < 3; i++) writer.Write(intensity);
            writer.Write(1f);
            writer.Write(false);
            stream.Position = 0;
            using var reader = new BinaryReader(stream);
            MaterialMetadata metadata = new WurmMaterialLoader(null).LoadMaterialMetadata(reader, "models");
            Assert.That(stream.Position, Is.EqualTo(stream.Length));
            return metadata;
        }

        private static byte[] CreateDxt5Bytes()
        {
            var bytes = new byte[144];
            using var stream = new MemoryStream(bytes);
            using var writer = new BinaryWriter(stream);
            writer.Write(0x20534444);
            writer.Write(124);
            writer.Write(0x81007);
            writer.Write(4);
            writer.Write(4);
            writer.Write(16);
            stream.Position = 76;
            writer.Write(32);
            writer.Write(4);
            writer.Write(0x35545844);
            stream.Position = 108;
            writer.Write(0x1000);
            stream.Position = 128;
            writer.Write((byte)200);
            writer.Write((byte)40);
            ulong alphaIndices = 0;
            for (int i = 8; i < 16; i++) alphaIndices |= 1UL << (i * 3);
            for (int i = 0; i < 6; i++) writer.Write((byte)(alphaIndices >> (i * 8)));
            writer.Write((ushort)0x801f);
            writer.Write((ushort)0x87ff);
            writer.Write(0x55550000);
            return bytes;
        }

        private T Track<T>(T value) where T : Object
        {
            _createdObjects.Add(value);
            return value;
        }

        private sealed class FixedTextureLoader : ITextureLoader
        {
            private readonly Texture2D _texture;
            public int Loads { get; private set; }
            public Func<bool> CanDiscardReadableData { get; private set; }

            public FixedTextureLoader(Texture2D texture) => _texture = texture;

            public Task<Texture2D> LoadTextureAsync(string location, bool readable, bool normalMap = false, bool linearData = false,
                Func<bool> canDiscardReadableData = null)
            {
                Loads++;
                CanDiscardReadableData = canDiscardReadableData;
                return Task.FromResult(_texture);
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
