using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Warlander.Deedplanner.Caves;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Domain.Entities.Caves;
using Warlander.Deedplanner.Domain.Entities.Grounds;
using Warlander.Deedplanner.Logging;
using Warlander.Render;
using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Rendering.Assets.Tests
{
    public class NormalMapTests
    {
        private const string TextureXml = "<objects><model><tex location='first.dds' normal='shared_n.dds'/><override mesh='body' texture='second.dds' normal='shared_n.dds'/></model></objects>";
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
        public async Task PairedReferencesSharePendingNormalLoadAndKeepDiffuseSeparate()
        {
            Texture2D diffuse = Track(new Texture2D(2, 2));
            Texture2D normal = Track(new Texture2D(2, 2));
            var completion = new TaskCompletionSource<Texture2D>();
            var loader = new RecordingTextureLoader(diffuse) { NormalTask = completion.Task };
            var factory = new TextureReferenceFactory(loader, new SilentLogger());
            var document = new XmlDocument();
            document.LoadXml(TextureXml);
            factory.LoadTextureDefinitions(document);
            TextureReference first = factory.GetTextureReference("first.dds");
            TextureReference second = factory.GetTextureReference("second.dds");

            Assert.That(factory.GetTextureReference(Application.streamingAssetsPath + "/first.dds"), Is.SameAs(first));
            Assert.That(await first.LoadOrGetTextureAsync(), Is.SameAs(diffuse));
            Assert.That(loader.NormalLoads, Is.Zero);
            Task<Texture2D> firstNormal = first.LoadOrGetNormalAsync();
            Task<Texture2D> secondNormal = second.LoadOrGetNormalAsync();

            Assert.That(secondNormal, Is.SameAs(firstNormal));
            Assert.That(loader.NormalLoads, Is.EqualTo(1));
            Assert.That(loader.LastNormalLocation, Is.EqualTo(Application.streamingAssetsPath + "/shared_n.dds"));
            Assert.That(loader.NormalReadable, Is.False);
            completion.SetResult(normal);
            Assert.That(await firstNormal, Is.SameAs(normal));
            Assert.That(await secondNormal, Is.SameAs(normal));
            Assert.That(await first.LoadOrGetNormalAsync(), Is.SameAs(normal));
            Assert.That(loader.NormalLoads, Is.EqualTo(1));
            Assert.That(loader.DiffuseLoads, Is.EqualTo(1));
        }

        [Test]
        public async Task UnmappedTextureHasNoNormalAndDoesNotRequestOne()
        {
            var loader = new RecordingTextureLoader(null);
            var factory = new TextureReferenceFactory(loader, new SilentLogger());
            var document = new XmlDocument();
            document.LoadXml(TextureXml);
            factory.LoadTextureDefinitions(document);

            Assert.That(await factory.GetTextureReference("unmapped.dds").LoadOrGetNormalAsync(), Is.Null);
            Assert.That(loader.NormalLoads, Is.Zero);
            Assert.That(loader.DiffuseLoads, Is.Zero);
        }

        [TestCase("<ground><tex location='surface.dds' normal='detail_n.dds'/></ground>")]
        [TestCase("<model><tex location='surface.dds' normal='detail_n.dds'/></model>")]
        [TestCase("<model><override mesh='body' texture='surface.dds' normal='detail_n.dds'/></model>")]
        [TestCase("<rock tex='surface.dds' normal='detail_n.dds'/>")]
        [TestCase("<roof tex='surface.dds' normal='detail_n.dds'/>")]
        public async Task XmlTextureDefinitionsSupplyNormals(string definition)
        {
            Texture2D normal = Track(new Texture2D(2, 2));
            var loader = new RecordingTextureLoader(null) { NormalTask = Task.FromResult(normal) };
            var factory = new TextureReferenceFactory(loader, new SilentLogger());
            var document = new XmlDocument();
            document.LoadXml("<entities>" + definition + "</entities>");
            factory.LoadTextureDefinitions(document);

            Assert.That(await factory.GetTextureReference("surface.dds").LoadOrGetNormalAsync(), Is.SameAs(normal));
            Assert.That(loader.NormalLoads, Is.EqualTo(1));
            Assert.That(loader.DiffuseLoads, Is.Zero);
        }

        [Test]
        public void ObjectsXmlNormalDefinitionsAreConsistentAndFilesExist()
        {
            var document = new XmlDocument();
            document.Load(Path.Combine(Application.streamingAssetsPath, "objects.xml"));
            var locations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (XmlElement element in document.SelectNodes("//*[@normal]"))
            {
                string location = element.Name == "tex" ? element.GetAttribute("location")
                    : element.Name == "override" ? element.GetAttribute("texture") : element.GetAttribute("tex");
                string normal = element.GetAttribute("normal");
                Assert.That(File.Exists(Path.Combine(Application.streamingAssetsPath, location)), Is.True, location);
                Assert.That(File.Exists(Path.Combine(Application.streamingAssetsPath, normal)), Is.True, normal);
                if (locations.TryGetValue(location, out string previous))
                    Assert.That(normal, Is.EqualTo(previous), location);
                locations[location] = normal;
            }
            Assert.That(locations.Count, Is.GreaterThan(0));
        }

        [Test]
        public async Task UnmappedOverrideClearsPreviousNormalBeforeDiffuseCompletes()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Materials/ModelShader.shadergraph");
            Assert.That(shader, Is.Not.Null);
            Material material = Track(new Material(shader));
            Texture2D staleNormal = Track(new Texture2D(2, 2));
            Texture2D diffuse = Track(new Texture2D(2, 2));
            material.SetTexture(ShaderPropertyIds.NormalMap, staleNormal);
            material.SetFloat(ShaderPropertyIds.NormalStrength, 1);
            var completion = new TaskCompletionSource<Texture2D>();
            var loader = new RecordingTextureLoader(null) { DiffuseTask = completion.Task };
            var reference = new TextureReference(loader, "unmapped.dds");

            Task apply = ModelMaterialTextures.ApplyAsync(material, reference);

            Assert.That(apply.IsCompleted, Is.False);
            Assert.That(material.IsKeywordEnabled("_SPECULAR_SETUP"), Is.True);
            Assert.That(material.GetTexture(ShaderPropertyIds.NormalMap), Is.Null);
            Assert.That(material.GetFloat(ShaderPropertyIds.NormalStrength), Is.Zero);
            completion.SetResult(diffuse);
            await apply;
            Assert.That(material.GetTexture(ShaderPropertyIds.BaseMap), Is.SameAs(diffuse));
            Assert.That(material.GetTexture(ShaderPropertyIds.NormalMap), Is.Null);
            Assert.That(material.GetFloat(ShaderPropertyIds.NormalStrength), Is.Zero);
            Assert.That(loader.NormalLoads, Is.Zero);
        }

        [TestCase(-2f)]
        [TestCase(2f)]
        public void MeshTangentsMatchFlippedUvsAndNonuniformScale(float scaleX)
        {
            Vector3 scale = new Vector3(scaleX, 3, 4);
            using var stream = new MemoryStream(CreateMeshBytes());
            using var reader = new BinaryReader(stream);
            Mesh mesh = Track(new WurmMeshLoader().LoadMesh(reader, scale));
            Vector3 expectedNormal = new Vector3(-1 / scale.x, -1 / scale.y, 1 / scale.z).normalized;
            Vector3 expectedTangent = Vector3.Scale(new Vector3(1, 0, 1), scale).normalized;
            Vector3 expectedBitangent = -Vector3.Scale(new Vector3(0, 1, 1), scale);
            expectedBitangent = (expectedBitangent - Vector3.Dot(expectedBitangent, expectedTangent) * expectedTangent).normalized;

            Assert.That(stream.Position, Is.EqualTo(stream.Length));
            Assert.That(mesh.uv[0], Is.EqualTo(new Vector2(0, 1)));
            Assert.That(mesh.tangents.Length, Is.EqualTo(mesh.vertexCount));
            for (int i = 0; i < mesh.vertexCount; i++)
            {
                Vector3 normal = mesh.normals[i];
                Vector4 tangent = mesh.tangents[i];
                Vector3 direction = new Vector3(tangent.x, tangent.y, tangent.z);
                Assert.That(normal.magnitude, Is.EqualTo(1).Within(0.0001f));
                Assert.That(Vector3.Dot(normal, expectedNormal), Is.GreaterThan(0.9999f));
                Assert.That(direction.magnitude, Is.EqualTo(1).Within(0.0001f));
                Assert.That(Vector3.Dot(direction, expectedTangent), Is.GreaterThan(0.9999f));
                Assert.That(Mathf.Abs(tangent.w), Is.EqualTo(1).Within(0.0001f));
                Assert.That(Vector3.Dot(Vector3.Cross(normal, direction) * tangent.w, expectedBitangent), Is.GreaterThan(0.9999f));
            }
            int[] triangles = mesh.triangles;
            Vector3[] vertices = mesh.vertices;
            Vector3 faceNormal = Vector3.Cross(vertices[triangles[1]] - vertices[triangles[0]], vertices[triangles[2]] - vertices[triangles[0]]).normalized;
            Assert.That(Vector3.Dot(faceNormal, expectedNormal), Is.GreaterThan(0.9999f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DdsNormalDecodingPreservesRgbAndCorrectsGreenAfterVerticalFlip(bool normalMap)
        {
            MethodInfo decode = typeof(DDSTextureLoader).GetMethod("LoadTextureDxt", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(decode, Is.Not.Null);
            Texture2D texture = Track((Texture2D)decode.Invoke(new DDSTextureLoader(new SilentLogger()),
                new object[] { CreateDdsBytes(), true, normalMap }));
            Color32[] pixels = texture.GetPixels32();

            Assert.That(texture.isDataSRGB, Is.EqualTo(!normalMap));
            for (int i = 0; i < pixels.Length; i++)
            {
                int expectedGreen = i < 8 ? 255 : 0;
                if (normalMap) expectedGreen = 255 - expectedGreen;
                Assert.That(pixels[i].r, Is.EqualTo(132).Within(8));
                Assert.That(pixels[i].g, Is.EqualTo(expectedGreen).Within(8));
                Assert.That(pixels[i].b, Is.EqualTo(255).Within(8));
                Assert.That(pixels[i].a, Is.EqualTo(255));
            }
        }

        [Test]
        public void GroundArraysKeepPairedIndicesAndUseFlatNormalForUnmappedTexture()
        {
            Texture2D firstDiffuse = CreateSolidTexture(Color.red);
            Texture2D secondDiffuse = CreateSolidTexture(Color.green);
            Color normalColor = new Color(1, 0.5f, 0.5f, 1);
            Texture2D normal = CreateSolidTexture(normalColor);
            var first = new TextureReference(new RecordingTextureLoader(firstDiffuse), "mapped.dds");
            var second = new TextureReference(new RecordingTextureLoader(secondDiffuse), "unmapped.dds");
            var catalog = new Database();
            catalog.AddGround(new GroundData("Test", "gr", Array.Empty<string[]>(), first, second, false));
            using var arrays = new GroundTextureArray(catalog);

            Assert.That(arrays.TryGetOrAdd(first, firstDiffuse, normal, out int firstIndex), Is.True);
            Assert.That(arrays.TryGetOrAdd(second, secondDiffuse, null, out int secondIndex), Is.True);
            Assert.That(arrays.TryGetOrAdd(first, firstDiffuse, normal, out int repeatedIndex), Is.True);
            Assert.That(repeatedIndex, Is.EqualTo(firstIndex));
            Assert.That(secondIndex, Is.Not.EqualTo(firstIndex));
            AssertPairedSlices(arrays, first, firstIndex, Color.red, normalColor);
            AssertPairedSlices(arrays, second, secondIndex, Color.green, new Color(0.5f, 0.5f, 1, 0));
        }

        [Test]
        public async Task CaveArraysKeepPairedIndicesAndDefaultFallback()
        {
            Texture2D defaultDiffuse = CreateSolidTexture(Color.red);
            Texture2D mappedDiffuse = CreateSolidTexture(Color.green);
            Color normalColor = new Color(1, 0.5f, 0.5f, 1);
            Texture2D normal = CreateSolidTexture(normalColor);
            var defaultReference = new TextureReference(new RecordingTextureLoader(defaultDiffuse), "stone.dds");
            var mappedLoader = new RecordingTextureLoader(mappedDiffuse) { NormalTask = Task.FromResult(normal) };
            var normalReference = new TextureReference(mappedLoader, "mapped_n.dds", normalMap: true);
            var mappedReference = new TextureReference(mappedLoader, "mapped.dds", normalReference: normalReference);
            var missingReference = new TextureReference(new RecordingTextureLoader(null), "missing.dds");
            var defaultTerrain = new CaveData(defaultReference, "Stone", "sw", Array.Empty<string[]>(), true, true, false);
            var mappedTerrain = new CaveData(mappedReference, "Mapped", "mapped", Array.Empty<string[]>(), true, true, false);
            var missingTerrain = new CaveData(missingReference, "Missing", "missing", Array.Empty<string[]>(), true, true, false);
            var catalog = new Database();
            catalog.AddCave(defaultTerrain);
            catalog.AddCave(mappedTerrain);
            catalog.AddCave(missingTerrain);
            using var arrays = new CaveTextureArray(catalog);

            await arrays.LoadAsync();

            int defaultIndex = arrays.GetIndex(defaultTerrain);
            int mappedIndex = arrays.GetIndex(mappedTerrain);
            Assert.That(mappedIndex, Is.Not.EqualTo(defaultIndex));
            Assert.That(arrays.GetIndex(missingTerrain), Is.EqualTo(defaultIndex));
            Assert.That(arrays.GetIndex(null), Is.EqualTo(defaultIndex));
            AssertPairedSlices(arrays, defaultReference, defaultIndex, Color.red, new Color(0.5f, 0.5f, 1, 0));
            AssertPairedSlices(arrays, mappedReference, mappedIndex, Color.green, normalColor);
        }

        private Texture2D CreateSolidTexture(Color color)
        {
            Texture2D texture = Track(new Texture2D(2, 2, TextureFormat.RGBA32, false, true));
            texture.SetPixels(new[] { color, color, color, color });
            texture.Apply();
            return texture;
        }

        private static void AssertPairedSlices(object arrays, TextureReference reference, int index, Color diffuse, Color normal)
        {
            var textures = (IndexedTextureArray<TextureReference>)arrays.GetType()
                .GetField("_textures", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(arrays);
            var normals = (IndexedTextureArray<TextureReference>)arrays.GetType()
                .GetField("_normals", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(arrays);
            Assert.That(textures.GetTextureIndex(reference), Is.EqualTo(index));
            Assert.That(normals.GetTextureIndex(reference), Is.EqualTo(index));
            Assert.That(normals.TextureArray.isDataSRGB, Is.False);
            Color actualDiffuse = textures.TextureArray.GetPixels(index)[0];
            Color actualNormal = normals.TextureArray.GetPixels(index)[0];
            Assert.That(actualDiffuse.r, Is.EqualTo(diffuse.r).Within(0.04f));
            Assert.That(actualDiffuse.g, Is.EqualTo(diffuse.g).Within(0.04f));
            Assert.That(actualDiffuse.b, Is.EqualTo(diffuse.b).Within(0.04f));
            Assert.That(actualNormal.r, Is.EqualTo(normal.r).Within(0.04f));
            Assert.That(actualNormal.g, Is.EqualTo(normal.g).Within(0.04f));
            Assert.That(actualNormal.b, Is.EqualTo(normal.b).Within(0.04f));
            Assert.That(actualNormal.a, Is.EqualTo(normal.a).Within(0.04f));
        }

        private static byte[] CreateMeshBytes()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(true);
            writer.Write(true);
            writer.Write(false);
            writer.Write(0);
            writer.Write(4);
            Vector3[] vertices = { Vector3.zero, new Vector3(1, 0, 1), new Vector3(1, 1, 2), new Vector3(0, 1, 1) };
            Vector2[] uvs = { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            Vector3 normal = new Vector3(-1, -1, 1).normalized;
            for (int i = 0; i < vertices.Length; i++)
            {
                writer.Write(vertices[i].x);
                writer.Write(vertices[i].y);
                writer.Write(vertices[i].z);
                writer.Write(normal.x);
                writer.Write(normal.y);
                writer.Write(normal.z);
                writer.Write(uvs[i].x);
                writer.Write(uvs[i].y);
                for (int j = 0; j < 6; j++) writer.Write(0f);
            }
            writer.Write(6);
            foreach (short index in new short[] { 0, 1, 2, 0, 2, 3 }) writer.Write(index);
            return stream.ToArray();
        }

        private static byte[] CreateDdsBytes()
        {
            var bytes = new byte[136];
            using var stream = new MemoryStream(bytes);
            using var writer = new BinaryWriter(stream);
            writer.Write(0x20534444);
            writer.Write(124);
            writer.Write(0x81007);
            writer.Write(4);
            writer.Write(4);
            writer.Write(8);
            stream.Position = 76;
            writer.Write(32);
            writer.Write(4);
            writer.Write(0x31545844);
            stream.Position = 108;
            writer.Write(0x1000);
            stream.Position = 128;
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

        private sealed class RecordingTextureLoader : ITextureLoader
        {
            public Task<Texture2D> DiffuseTask { get; set; }
            public Task<Texture2D> NormalTask { get; set; } = Task.FromResult<Texture2D>(null);
            public int DiffuseLoads { get; private set; }
            public int NormalLoads { get; private set; }
            public string LastNormalLocation { get; private set; }
            public bool NormalReadable { get; private set; }

            public RecordingTextureLoader(Texture2D diffuse) => DiffuseTask = Task.FromResult(diffuse);

            public Task<Texture2D> LoadTextureAsync(string location, bool readable, bool normalMap = false)
            {
                if (!normalMap)
                {
                    DiffuseLoads++;
                    return DiffuseTask;
                }
                NormalLoads++;
                LastNormalLocation = location;
                NormalReadable = readable;
                return NormalTask;
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
