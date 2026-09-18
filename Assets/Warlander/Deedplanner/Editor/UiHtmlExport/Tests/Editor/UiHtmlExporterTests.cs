using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TMPro;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Editor.UiHtmlExport.Tests
{
    public class UiHtmlExporterTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly List<string> _directories = new List<string>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject gameObject in _objects)
            {
                if (gameObject != null)
                    Object.DestroyImmediate(gameObject);
            }

            foreach (string directory in _directories)
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        [Test]
        public void ResolveGeometry_UsesRootRelativeTopLeftCoordinates()
        {
            RectTransform root = CreateRect("Root", null, new Vector2(200, 100));
            RectTransform child = CreateRect("Child", root, new Vector2(50, 20));
            child.anchorMin = Vector2.zero;
            child.anchorMax = Vector2.zero;
            child.pivot = Vector2.zero;
            child.anchoredPosition = new Vector2(20, 30);

            UiHtmlGeometry geometry = UiHtmlExporter.ResolveGeometry(root, child);

            Assert.That(geometry.X, Is.EqualTo(20).Within(0.001f));
            Assert.That(geometry.Y, Is.EqualTo(50).Within(0.001f));
            Assert.That(geometry.Width, Is.EqualTo(50).Within(0.001f));
            Assert.That(geometry.Height, Is.EqualTo(20).Within(0.001f));
            Assert.That(geometry.XPercent, Is.EqualTo(10).Within(0.001f));
            Assert.That(geometry.YPercent, Is.EqualTo(50).Within(0.001f));
            Assert.That(geometry.A, Is.EqualTo(1).Within(0.001f));
            Assert.That(geometry.D, Is.EqualTo(1).Within(0.001f));
        }

        [Test]
        public void ResolveGeometry_PreservesTwoDimensionalRotation()
        {
            RectTransform root = CreateRect("Root", null, new Vector2(200, 100));
            RectTransform child = CreateRect("Child", root, new Vector2(50, 20));
            child.localRotation = Quaternion.Euler(0, 0, 90);

            UiHtmlGeometry geometry = UiHtmlExporter.ResolveGeometry(root, child);

            Assert.That(geometry.A, Is.EqualTo(0).Within(0.001f));
            Assert.That(geometry.B, Is.EqualTo(-1).Within(0.001f));
            Assert.That(geometry.C, Is.EqualTo(1).Within(0.001f));
            Assert.That(geometry.D, Is.EqualTo(0).Within(0.001f));
        }

        [Test]
        public void Export_WritesComposableHtmlManifestAndDeduplicatedImages()
        {
            RectTransform root = CreateRect("Panel Root", null, new Vector2(200, 100));
            CreateImage("Background", root, new Vector2(80, 40), new Color(0.2f, 0.4f, 0.8f, 1));
            CreateImage("Repeated", root, new Vector2(80, 40), new Color(0.2f, 0.4f, 0.8f, 1));
            TextMeshProUGUI text = CreateRect("Label", root, new Vector2(100, 20)).gameObject
                .AddComponent<TextMeshProUGUI>();
            text.text = "A < B & C";
            text.fontSize = 16;
            string output = NewOutputPath();

            UiHtmlExportResult result = UiHtmlExporter.Export(root, output);

            Assert.That(result.Success, Is.True);
            Assert.That(result.RectTransformCount, Is.EqualTo(4));
            Assert.That(result.ImageCount, Is.EqualTo(2));
            Assert.That(result.RawImageCount, Is.Zero);
            Assert.That(result.TextCount, Is.EqualTo(1));
            Assert.That(Directory.GetFiles(Path.Combine(output, "assets"), "*.png"), Has.Length.EqualTo(1));

            string html = File.ReadAllText(Path.Combine(output, "index.html"));
            Assert.That(html, Does.StartWith("<style>"));
            Assert.That(html, Does.Contain("data-format=\"ugui-html-export/v2\""));
            Assert.That(html, Does.Contain("<img class=\"ugui-export__visual ugui-export__image\""));
            Assert.That(html, Does.Contain("A &lt; B &amp; C"));
            Assert.That(html, Does.Not.Contain("<html"));

            string manifest = File.ReadAllText(Path.Combine(output, "manifest.json"));
            Assert.That(manifest, Does.Contain("\"format\": \"ugui-html-export/v2\""));
            Assert.That(manifest, Does.Contain("\"type\": \"image\""));

            byte[] png = File.ReadAllBytes(Directory.GetFiles(Path.Combine(output, "assets"), "*.png")[0]);
            Assert.That(png[0], Is.EqualTo(0x89));
            Assert.That(png[1], Is.EqualTo(0x50));
            Assert.That(png[2], Is.EqualTo(0x4e));
            Assert.That(png[3], Is.EqualTo(0x47));
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(texture.LoadImage(png), Is.True);
                Color pixel = texture.GetPixel(texture.width / 2, texture.height / 2);
                Assert.That(pixel.a, Is.GreaterThan(0.9f));
                Assert.That(pixel.b, Is.GreaterThan(pixel.r));
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void Export_RendersRawImageUvRectAndReportsUnsupportedMask()
        {
            RectTransform root = CreateRect("Root", null, new Vector2(200, 100));
            RectTransform rawRect = CreateRect("Preview", root, new Vector2(80, 40));
            RawImage rawImage = rawRect.gameObject.AddComponent<RawImage>();
            rawRect.gameObject.AddComponent<RectMask2D>();
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.filterMode = FilterMode.Point;
            source.SetPixels(new[] { Color.red, Color.blue, Color.red, Color.blue });
            source.Apply();
            rawImage.texture = source;
            rawImage.uvRect = new Rect(0.5f, 0, 0.5f, 1);
            string output = NewOutputPath();

            try
            {
                UiHtmlExportResult result = UiHtmlExporter.Export(root, output);

                Assert.That(result.RawImageCount, Is.EqualTo(1));
                Assert.That(result.Warnings, Has.None.Contains("RawImage is not supported"));
                Assert.That(result.Warnings, Has.Some.Contains("RectMask2D clipping is not supported"));
                string manifest = File.ReadAllText(Path.Combine(output, "manifest.json"));
                Assert.That(manifest, Does.Contain("\"type\": \"raw-image\""));

                byte[] png = File.ReadAllBytes(Directory.GetFiles(Path.Combine(output, "assets"), "*.png")[0]);
                var rendered = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    Assert.That(rendered.LoadImage(png), Is.True);
                    Color pixel = rendered.GetPixel(rendered.width / 2, rendered.height / 2);
                    Assert.That(pixel.b, Is.GreaterThan(pixel.r));
                }
                finally
                {
                    Object.DestroyImmediate(rendered);
                }
            }
            finally
            {
                Object.DestroyImmediate(source);
            }
        }

        [Test]
        public void Export_PreservesStructuralRectTransformsAndOmitsEmptyLeaves()
        {
            RectTransform root = CreateRect("Root", null, new Vector2(200, 100));
            RectTransform group = CreateRect("Group", root, new Vector2(100, 50));
            CreateImage("Icon", group, new Vector2(20, 20), Color.white);
            CreateRect("Empty", root, new Vector2(10, 10));
            string output = NewOutputPath();

            UiHtmlExportResult result = UiHtmlExporter.Export(root, output);

            Assert.That(result.RectTransformCount, Is.EqualTo(3));
            string html = File.ReadAllText(Path.Combine(output, "index.html"));
            Assert.That(html, Does.Contain("data-path=\"/Root/Group\""));
            Assert.That(html, Does.Contain("data-path=\"/Root/Group/Icon\""));
            Assert.That(html, Does.Not.Contain("/Root/Empty"));
            string manifest = File.ReadAllText(Path.Combine(output, "manifest.json"));
            Assert.That(manifest, Does.Contain("\"parentPath\": \"/Root/Group\""));
        }

        [Test]
        public void Export_DeedCardPrefab_HandlesInitializedNoThumbnailState()
        {
            const string prefabPath = "Assets/Prefabs/Gui/Deed Card.prefab";
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            string output = NewOutputPath();
            try
            {
                RectTransform root = prefabRoot.GetComponent<RectTransform>();
                root.Find("Thumbnail").gameObject.SetActive(false);

                UiHtmlExportResult result = UiHtmlExporter.Export(root, output);

                Assert.That(result.ImageCount, Is.GreaterThan(0));
                Assert.That(result.RawImageCount, Is.Zero);
                Assert.That(result.TextCount, Is.GreaterThan(0));
                Assert.That(Directory.GetFiles(Path.Combine(output, "assets"), "*.png").Length,
                    Is.GreaterThan(0));
                Assert.That(File.ReadAllText(Path.Combine(output, "index.html")), Does.Not.Contain("/Thumbnail"));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        [Test]
        public void ExportCommand_AcceptsPrefabAssetPath()
        {
            const string prefabPath = "Assets/Prefabs/Gui/Deed Card.prefab";
            string output = NewOutputPath();

            UiHtmlExportResult result = UiHtmlExportCommands.Export(new ObjectRef { Path = prefabPath }, output);

            Assert.That(result.Success, Is.True);
            Assert.That(result.ImageCount, Is.GreaterThan(0));
            Assert.That(result.RawImageCount, Is.GreaterThan(0));
            Assert.That(result.TextCount, Is.GreaterThan(0));
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), Is.Not.Null);
        }

        [Test]
        public void Export_RefusesNonEmptyOutputDirectory()
        {
            RectTransform root = CreateRect("Root", null, new Vector2(200, 100));
            string output = NewOutputPath();
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "keep.txt"), "keep");

            IOException exception = Assert.Throws<IOException>(() => UiHtmlExporter.Export(root, output));

            Assert.That(exception.Message, Does.Contain("must be empty"));
            Assert.That(File.ReadAllText(Path.Combine(output, "keep.txt")), Is.EqualTo("keep"));
        }

        private RectTransform CreateRect(string name, RectTransform parent, Vector2 size)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            _objects.Add(gameObject);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            if (parent != null)
                rect.SetParent(parent, false);
            rect.sizeDelta = size;
            return rect;
        }

        private Image CreateImage(string name, RectTransform parent, Vector2 size, Color color)
        {
            RectTransform rect = CreateRect(name, parent, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private string NewOutputPath()
        {
            string path = Path.Combine(Path.GetTempPath(), "dp3-ugui-html-" + Guid.NewGuid().ToString("N"));
            _directories.Add(path);
            return path;
        }
    }
}
