using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Editor.UiHtmlExport
{
    public static class UiHtmlExporter
    {
        private const string Format = "ugui-html-export/v2";
        private const float VisibleAlpha = 0.0001f;

        public static UiHtmlExportResult Export(RectTransform root, string outputDirectory)
        {
            Validate(root, outputDirectory);

            string fullOutputPath = Path.GetFullPath(outputDirectory);
            Directory.CreateDirectory(fullOutputPath);
            string assetsPath = Path.Combine(fullOutputPath, "assets");
            Directory.CreateDirectory(assetsPath);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            Canvas.ForceUpdateCanvases();

            Rect rootRect = root.rect;
            var warnings = new List<string>();
            var warningSet = new HashSet<string>();
            RectTransform[] transforms = root.GetComponentsInChildren<RectTransform>(false);
            foreach (RectTransform rectTransform in transforms)
            {
                if (rectTransform.gameObject.activeInHierarchy)
                    AddUnsupportedWarnings(root, rectTransform, BuildPath(rectTransform, root.parent), warnings,
                        warningSet);
            }

            HashSet<RectTransform> retained = FindRetainedTransforms(root, transforms);
            Dictionary<RectTransform, List<RectTransform>> children = BuildRetainedChildren(root, transforms,
                retained);
            var manifest = new UiHtmlManifest
            {
                format = Format,
                sourcePath = BuildPath(root, root.parent),
                width = rootRect.width,
                height = rootRect.height,
                rects = new List<UiHtmlManifestRect>(),
                elements = new List<UiHtmlManifestElement>(),
                assets = new List<UiHtmlManifestAsset>()
            };
            var html = new StringBuilder(4096);
            AppendHeader(html, rootRect.width, rootRect.height, manifest.sourcePath);

            var context = new ExportContext
            {
                Root = root,
                RootWidth = rootRect.width,
                AssetsPath = assetsPath,
                AssetsByHash = new Dictionary<string, UiHtmlManifestAsset>(),
                Children = children,
                Manifest = manifest,
                Html = html,
                Warnings = warnings,
                WarningSet = warningSet
            };
            UiHtmlGeometry rootGeometry = CreateRootGeometry(rootRect);
            manifest.rects.Add(CreateManifestRect(manifest.sourcePath, string.Empty, rootGeometry));
            AppendVisuals(context, root, rootGeometry, 1);
            if (children.TryGetValue(root, out List<RectTransform> rootChildren))
            {
                foreach (RectTransform child in rootChildren)
                    AppendRect(context, child, root, 1);
            }

            if (context.TextCount > 0)
                AddWarning("TMP font assets are not embedded; exported text uses the browser's Arial fallback.",
                    warnings, warningSet);

            html.AppendLine("</div>");
            manifest.warnings = warnings.ToArray();

            File.WriteAllText(Path.Combine(fullOutputPath, "index.html"), html.ToString(), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(fullOutputPath, "manifest.json"), JsonUtility.ToJson(manifest, true),
                new UTF8Encoding(false));

            return new UiHtmlExportResult
            {
                Success = true,
                Path = fullOutputPath,
                Width = rootRect.width,
                Height = rootRect.height,
                RectTransformCount = retained.Count,
                ElementCount = context.ImageCount + context.RawImageCount + context.TextCount,
                ImageCount = context.ImageCount,
                RawImageCount = context.RawImageCount,
                TextCount = context.TextCount,
                Warnings = warnings.ToArray()
            };
        }

        private static HashSet<RectTransform> FindRetainedTransforms(RectTransform root,
            RectTransform[] transforms)
        {
            var retained = new HashSet<RectTransform> { root };
            foreach (RectTransform rectTransform in transforms)
            {
                if (!rectTransform.gameObject.activeInHierarchy || !HasSupportedVisual(rectTransform))
                    continue;

                Transform current = rectTransform;
                while (current != null)
                {
                    if (current is RectTransform currentRect)
                        retained.Add(currentRect);
                    if (current == root)
                        break;
                    current = current.parent;
                }
            }
            return retained;
        }

        private static bool HasSupportedVisual(RectTransform rectTransform)
        {
            if (IsVisible(rectTransform.GetComponent<Image>()) ||
                IsVisible(rectTransform.GetComponent<RawImage>()))
                return true;

            TextMeshProUGUI text = rectTransform.GetComponent<TextMeshProUGUI>();
            return IsVisible(text) && !string.IsNullOrEmpty(text.text);
        }

        private static Dictionary<RectTransform, List<RectTransform>> BuildRetainedChildren(RectTransform root,
            RectTransform[] transforms, HashSet<RectTransform> retained)
        {
            var children = new Dictionary<RectTransform, List<RectTransform>>();
            foreach (RectTransform rectTransform in transforms)
            {
                if (rectTransform == root || !retained.Contains(rectTransform))
                    continue;

                RectTransform parent = FindRetainedParent(rectTransform, root, retained);
                if (!children.TryGetValue(parent, out List<RectTransform> siblings))
                {
                    siblings = new List<RectTransform>();
                    children.Add(parent, siblings);
                }
                siblings.Add(rectTransform);
            }
            return children;
        }

        private static RectTransform FindRetainedParent(RectTransform rectTransform, RectTransform root,
            HashSet<RectTransform> retained)
        {
            Transform current = rectTransform.parent;
            while (current != null && current != root)
            {
                if (current is RectTransform parent && retained.Contains(parent))
                    return parent;
                current = current.parent;
            }
            return root;
        }

        private static void AppendRect(ExportContext context, RectTransform rectTransform, RectTransform parent,
            int depth)
        {
            string path = BuildPath(rectTransform, context.Root.parent);
            string parentPath = BuildPath(parent, context.Root.parent);
            UiHtmlGeometry geometry = ResolveGeometry(parent, rectTransform);
            context.Manifest.rects.Add(CreateManifestRect(path, parentPath, geometry));

            AppendIndent(context.Html, depth);
            context.Html.Append("<div class=\"ugui-export__rect\" ");
            AppendGeometryAttributes(context.Html, path, geometry);
            context.Html.AppendLine(">");
            AppendVisuals(context, rectTransform, geometry, depth + 1);
            if (context.Children.TryGetValue(rectTransform, out List<RectTransform> children))
            {
                foreach (RectTransform child in children)
                    AppendRect(context, child, rectTransform, depth + 1);
            }
            AppendIndent(context.Html, depth);
            context.Html.AppendLine("</div>");
        }

        private static void AppendVisuals(ExportContext context, RectTransform rectTransform,
            UiHtmlGeometry geometry, int depth)
        {
            string path = BuildPath(rectTransform, context.Root.parent);
            Image image = rectTransform.GetComponent<Image>();
            if (IsVisible(image))
            {
                string assetPath = WriteImageAsset(image, geometry, context.AssetsPath, context.AssetsByHash,
                    context.Manifest.assets, path, context.Warnings, context.WarningSet);
                AppendImage(context.Html, path, assetPath, "image", depth);
                context.Manifest.elements.Add(CreateManifestElement("image", path, geometry, assetPath, null));
                context.ImageCount++;
            }

            RawImage rawImage = rectTransform.GetComponent<RawImage>();
            if (IsVisible(rawImage))
            {
                string assetPath = WriteRawImageAsset(rawImage, geometry, context.AssetsPath, context.AssetsByHash,
                    context.Manifest.assets, path, context.Warnings, context.WarningSet);
                AppendImage(context.Html, path, assetPath, "raw-image", depth);
                context.Manifest.elements.Add(CreateManifestElement("raw-image", path, geometry, assetPath, null));
                context.RawImageCount++;
            }

            TextMeshProUGUI text = rectTransform.GetComponent<TextMeshProUGUI>();
            if (IsVisible(text) && !string.IsNullOrEmpty(text.text))
            {
                string parsedText = GetParsedText(text);
                AppendText(context.Html, path, text, parsedText, context.RootWidth, depth);
                context.Manifest.elements.Add(CreateManifestElement("text", path, geometry, null, parsedText));
                context.TextCount++;
            }
        }

        internal static UiHtmlGeometry ResolveGeometry(RectTransform root, RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            for (int i = 0; i < corners.Length; i++)
            {
                corners[i] = root.InverseTransformPoint(corners[i]);
            }

            Rect rootRect = root.rect;
            float width = target.rect.width;
            float height = target.rect.height;
            Vector3 topLeft = corners[1];
            Vector3 topRight = corners[2];
            Vector3 bottomLeft = corners[0];

            return new UiHtmlGeometry
            {
                X = topLeft.x - rootRect.xMin,
                Y = rootRect.yMax - topLeft.y,
                Width = width,
                Height = height,
                A = width == 0 ? 1 : (topRight.x - topLeft.x) / width,
                B = width == 0 ? 0 : -(topRight.y - topLeft.y) / width,
                C = height == 0 ? 0 : (bottomLeft.x - topLeft.x) / height,
                D = height == 0 ? 1 : -(bottomLeft.y - topLeft.y) / height,
                RootWidth = rootRect.width,
                RootHeight = rootRect.height
            };
        }

        private static void Validate(RectTransform root, string outputDirectory)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (!root.gameObject.activeInHierarchy)
                throw new InvalidOperationException("The export root must be active in the hierarchy.");
            if (root.rect.width <= 0 || root.rect.height <= 0)
                throw new InvalidOperationException("The export root must have a positive resolved width and height.");
            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new ArgumentException("An output directory is required.", nameof(outputDirectory));

            string fullPath = Path.GetFullPath(outputDirectory);
            if (Directory.Exists(fullPath) && Directory.GetFileSystemEntries(fullPath).Length > 0)
                throw new IOException($"The output directory must be empty: {fullPath}");
        }

        private static bool IsVisible(Graphic graphic)
        {
            return graphic != null && graphic.enabled && graphic.gameObject.activeInHierarchy &&
                   graphic.color.a * graphic.canvasRenderer.GetInheritedAlpha() > VisibleAlpha;
        }

        private static void AddUnsupportedWarnings(RectTransform root, RectTransform rectTransform, string path,
            List<string> warnings, HashSet<string> warningSet)
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            float minimumZ = float.MaxValue;
            float maximumZ = float.MinValue;
            foreach (Vector3 corner in corners)
            {
                float z = root.InverseTransformPoint(corner).z;
                minimumZ = Mathf.Min(minimumZ, z);
                maximumZ = Mathf.Max(maximumZ, z);
            }
            if (maximumZ - minimumZ > 0.001f)
                AddWarning($"Out-of-plane RectTransform rotation is not supported: {path}", warnings, warningSet);

            Mask mask = rectTransform.GetComponent<Mask>();
            if (mask != null && mask.enabled)
                AddWarning($"Mask clipping is not supported: {path}", warnings, warningSet);

            RectMask2D rectMask = rectTransform.GetComponent<RectMask2D>();
            if (rectMask != null && rectMask.enabled)
                AddWarning($"RectMask2D clipping is not supported: {path}", warnings, warningSet);

            BaseMeshEffect[] effects = rectTransform.GetComponents<BaseMeshEffect>();
            foreach (BaseMeshEffect effect in effects)
            {
                if (effect != null && effect.enabled)
                    AddWarning($"{effect.GetType().Name} is not supported: {path}", warnings, warningSet);
            }

            Graphic[] graphics = rectTransform.GetComponents<Graphic>();
            foreach (Graphic graphic in graphics)
            {
                if (graphic == null || graphic is Image || graphic is TextMeshProUGUI || graphic is RawImage ||
                    graphic.GetType().Name == "NonDrawingGraphic" || !IsVisible(graphic))
                    continue;
                AddWarning($"{graphic.GetType().Name} is not supported: {path}", warnings, warningSet);
            }
        }

        private static string WriteImageAsset(Image image, UiHtmlGeometry geometry, string assetsPath,
            Dictionary<string, UiHtmlManifestAsset> assetsByHash, List<UiHtmlManifestAsset> assets, string path,
            List<string> warnings, HashSet<string> warningSet)
        {
            if (image.material != null && image.material != image.defaultMaterial)
                AddWarning($"Custom Image material is not supported and was omitted: {path}", warnings, warningSet);

            Sprite sprite = image.overrideSprite != null ? image.overrideSprite : image.sprite;
            return WriteGraphicAsset(image, geometry, assetsPath, assetsByHash, assets,
                sprite != null ? sprite.name : string.Empty);
        }

        private static string WriteRawImageAsset(RawImage rawImage, UiHtmlGeometry geometry, string assetsPath,
            Dictionary<string, UiHtmlManifestAsset> assetsByHash, List<UiHtmlManifestAsset> assets, string path,
            List<string> warnings, HashSet<string> warningSet)
        {
            if (rawImage.material != null && rawImage.material != rawImage.defaultMaterial)
                AddWarning($"Custom RawImage material is not supported and was omitted: {path}", warnings,
                    warningSet);

            return WriteGraphicAsset(rawImage, geometry, assetsPath, assetsByHash, assets,
                rawImage.texture != null ? rawImage.texture.name : string.Empty);
        }

        private static string WriteGraphicAsset(Graphic graphic, UiHtmlGeometry geometry, string assetsPath,
            Dictionary<string, UiHtmlManifestAsset> assetsByHash, List<UiHtmlManifestAsset> assets,
            string sourceName)
        {
            int width = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(geometry.Width)));
            int height = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(geometry.Height)));
            byte[] png = UiImageRasterizer.Render(graphic, width, height);
            string hash = ComputeHash(png);
            if (assetsByHash.TryGetValue(hash, out UiHtmlManifestAsset existing))
                return existing.file;

            string fileName = $"{SanitizeFileName(graphic.gameObject.name)}-{hash.Substring(0, 12)}.png";
            string relativePath = "assets/" + fileName;
            File.WriteAllBytes(Path.Combine(assetsPath, fileName), png);

            var asset = new UiHtmlManifestAsset
            {
                file = relativePath,
                sha256 = hash,
                sourceName = sourceName,
                width = width,
                height = height
            };
            assetsByHash.Add(hash, asset);
            assets.Add(asset);
            return relativePath;
        }

        private static void AppendHeader(StringBuilder html, float width, float height, string sourcePath)
        {
            html.AppendLine("<style>");
            html.AppendLine(".ugui-export{position:relative;width:100%;max-width:none;aspect-ratio:" +
                            Number(width) + "/" + Number(height) +
                            ";overflow:visible;isolation:isolate;container-type:inline-size;background:transparent}");
            html.AppendLine(".ugui-export,.ugui-export *{box-sizing:border-box}");
            html.AppendLine(".ugui-export__rect{position:absolute;transform-origin:0 0;margin:0;padding:0}");
            html.AppendLine(".ugui-export__visual{position:absolute;inset:0;margin:0;padding:0}");
            html.AppendLine(".ugui-export__image{display:block;width:100%;height:100%;max-width:none}");
            html.AppendLine(".ugui-export__text{display:flex;overflow:hidden;font-family:Arial,sans-serif;white-space:pre-wrap}");
            html.AppendLine("</style>");
            html.Append("<div class=\"ugui-export\" data-format=\"").Append(Format)
                .Append("\" data-source-path=\"").Append(EscapeAttribute(sourcePath))
                .Append("\" data-width=\"").Append(Number(width)).Append("\" data-height=\"")
                .Append(Number(height)).AppendLine("\">");
        }

        private static void AppendImage(StringBuilder html, string path, string assetPath, string type, int depth)
        {
            AppendIndent(html, depth);
            html.Append("<img class=\"ugui-export__visual ugui-export__image\" data-component=\"")
                .Append(type).Append("\" data-path=\"").Append(EscapeAttribute(path)).Append("\" src=\"")
                .Append(EscapeAttribute(assetPath)).AppendLine("\" alt=\"\">");
        }

        private static void AppendText(StringBuilder html, string path, TextMeshProUGUI text, string content,
            float rootWidth, int depth)
        {
            Color color = text.color;
            color.a *= text.canvasRenderer.GetInheritedAlpha();
            string horizontal = "flex-start";
            if ((text.horizontalAlignment & HorizontalAlignmentOptions.Right) != 0)
                horizontal = "flex-end";
            else if ((text.horizontalAlignment & (HorizontalAlignmentOptions.Center | HorizontalAlignmentOptions.Geometry)) != 0)
                horizontal = "center";

            string vertical = "flex-start";
            if ((text.verticalAlignment & VerticalAlignmentOptions.Bottom) != 0)
                vertical = "flex-end";
            else if ((text.verticalAlignment & (VerticalAlignmentOptions.Middle | VerticalAlignmentOptions.Geometry |
                                                VerticalAlignmentOptions.Baseline | VerticalAlignmentOptions.Capline)) != 0)
                vertical = "center";

            string fontWeight = (text.fontStyle & FontStyles.Bold) != 0 ? "700" : "400";
            string fontStyle = (text.fontStyle & FontStyles.Italic) != 0 ? "italic" : "normal";
            string decoration = TextDecoration(text.fontStyle);
            float fontSize = rootWidth > 0 ? text.fontSize / rootWidth * 100 : 0;

            string textStyle = "color:#" + ColorUtility.ToHtmlStringRGBA(color) + ";font-size:" +
                               Number(fontSize) + "cqw;font-weight:" + fontWeight + ";font-style:" + fontStyle +
                               ";text-decoration:" + decoration + ";justify-content:" + horizontal +
                               ";text-align:" +
                               (horizontal == "flex-end" ? "right" : horizontal == "center" ? "center" : "left") +
                               ";align-items:" + vertical;

            AppendIndent(html, depth);
            html.Append("<div class=\"ugui-export__visual ugui-export__text\" data-component=\"text\" data-path=\"")
                .Append(EscapeAttribute(path)).Append("\" style=\"").Append(textStyle)
                .Append("\" data-unity-font=\"")
                .Append(EscapeAttribute(text.font != null ? text.font.name : string.Empty)).Append("\"")
                .Append(">")
                .Append(EscapeText(content)).AppendLine("</div>");
        }

        private static void AppendGeometryAttributes(StringBuilder html, string path, UiHtmlGeometry geometry)
        {
            html.Append("data-path=\"").Append(EscapeAttribute(path)).Append("\" data-x=\"")
                .Append(Number(geometry.X)).Append("\" data-y=\"").Append(Number(geometry.Y))
                .Append("\" data-width=\"").Append(Number(geometry.Width)).Append("\" data-height=\"")
                .Append(Number(geometry.Height)).Append("\" style=\"").Append(GeometryStyle(geometry)).Append("\"");
        }

        private static string GeometryStyle(UiHtmlGeometry geometry)
        {
            return "left:" + Number(geometry.XPercent) + "%;top:" + Number(geometry.YPercent) +
                   "%;width:" + Number(geometry.WidthPercent) + "%;height:" + Number(geometry.HeightPercent) +
                   "%;transform:matrix(" + Number(geometry.A) + "," + Number(geometry.B) + "," +
                   Number(geometry.C) + "," + Number(geometry.D) + ",0,0)";
        }

        private static UiHtmlGeometry CreateRootGeometry(Rect rootRect)
        {
            return new UiHtmlGeometry
            {
                X = 0,
                Y = 0,
                Width = rootRect.width,
                Height = rootRect.height,
                A = 1,
                D = 1,
                RootWidth = rootRect.width,
                RootHeight = rootRect.height
            };
        }

        private static UiHtmlManifestRect CreateManifestRect(string path, string parentPath,
            UiHtmlGeometry geometry)
        {
            return new UiHtmlManifestRect
            {
                path = path,
                parentPath = parentPath,
                x = geometry.X,
                y = geometry.Y,
                width = geometry.Width,
                height = geometry.Height,
                a = geometry.A,
                b = geometry.B,
                c = geometry.C,
                d = geometry.D
            };
        }

        private static UiHtmlManifestElement CreateManifestElement(string type, string path, UiHtmlGeometry geometry,
            string asset, string text)
        {
            return new UiHtmlManifestElement
            {
                type = type,
                path = path,
                x = geometry.X,
                y = geometry.Y,
                width = geometry.Width,
                height = geometry.Height,
                a = geometry.A,
                b = geometry.B,
                c = geometry.C,
                d = geometry.D,
                asset = asset,
                text = text
            };
        }

        private static string GetParsedText(TextMeshProUGUI text)
        {
            if (text.font == null)
                return text.text;
            text.ForceMeshUpdate(false, true);
            string parsed = text.GetParsedText();
            return string.IsNullOrEmpty(parsed) ? text.text : parsed;
        }

        private static string TextDecoration(FontStyles style)
        {
            bool underline = (style & FontStyles.Underline) != 0;
            bool strike = (style & FontStyles.Strikethrough) != 0;
            if (underline && strike)
                return "underline line-through";
            if (underline)
                return "underline";
            return strike ? "line-through" : "none";
        }

        private static void AppendIndent(StringBuilder html, int depth)
        {
            html.Append(' ', depth * 2);
        }

        private static string BuildPath(RectTransform transform, Transform stopBefore)
        {
            var names = new List<string>();
            Transform current = transform;
            while (current != null && current != stopBefore)
            {
                names.Add(current.name);
                current = current.parent;
            }
            names.Reverse();
            return "/" + string.Join("/", names);
        }

        private static void AddWarning(string warning, List<string> warnings, HashSet<string> warningSet)
        {
            if (warningSet.Add(warning))
                warnings.Add(warning);
        }

        private static string ComputeHash(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(bytes);
                var result = new StringBuilder(hash.Length * 2);
                foreach (byte value in hash)
                    result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }

        private static string SanitizeFileName(string value)
        {
            var result = new StringBuilder();
            foreach (char character in value.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(character))
                    result.Append(character);
                else if (result.Length > 0 && result[result.Length - 1] != '-')
                    result.Append('-');
            }
            string sanitized = result.ToString().Trim('-');
            return string.IsNullOrEmpty(sanitized) ? "image" : sanitized;
        }

        private static string Number(float value)
        {
            if (Mathf.Abs(value) < 0.0000005f)
                value = 0;
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string EscapeAttribute(string value)
        {
            return EscapeText(value).Replace("'", "&#39;");
        }

        private static string EscapeText(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }
    }

    public sealed class UiHtmlExportResult
    {
        public bool Success { get; set; }
        public string Path { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public int RectTransformCount { get; set; }
        public int ElementCount { get; set; }
        public int ImageCount { get; set; }
        public int RawImageCount { get; set; }
        public int TextCount { get; set; }
        public string[] Warnings { get; set; }
    }

    internal struct UiHtmlGeometry
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;
        public float A;
        public float B;
        public float C;
        public float D;
        public float RootWidth;
        public float RootHeight;

        public float XPercent => RootWidth == 0 ? 0 : X / RootWidth * 100;
        public float YPercent => RootHeight == 0 ? 0 : Y / RootHeight * 100;
        public float WidthPercent => RootWidth == 0 ? 0 : Width / RootWidth * 100;
        public float HeightPercent => RootHeight == 0 ? 0 : Height / RootHeight * 100;
    }

    internal sealed class ExportContext
    {
        public RectTransform Root;
        public float RootWidth;
        public string AssetsPath;
        public Dictionary<string, UiHtmlManifestAsset> AssetsByHash;
        public Dictionary<RectTransform, List<RectTransform>> Children;
        public UiHtmlManifest Manifest;
        public StringBuilder Html;
        public List<string> Warnings;
        public HashSet<string> WarningSet;
        public int ImageCount;
        public int RawImageCount;
        public int TextCount;
    }

    [Serializable]
    internal sealed class UiHtmlManifest
    {
        public string format;
        public string sourcePath;
        public float width;
        public float height;
        public List<UiHtmlManifestRect> rects;
        public List<UiHtmlManifestElement> elements;
        public List<UiHtmlManifestAsset> assets;
        public string[] warnings;
    }

    [Serializable]
    internal sealed class UiHtmlManifestRect
    {
        public string path;
        public string parentPath;
        public float x;
        public float y;
        public float width;
        public float height;
        public float a;
        public float b;
        public float c;
        public float d;
    }

    [Serializable]
    internal sealed class UiHtmlManifestElement
    {
        public string type;
        public string path;
        public float x;
        public float y;
        public float width;
        public float height;
        public float a;
        public float b;
        public float c;
        public float d;
        public string asset;
        public string text;
    }

    [Serializable]
    internal sealed class UiHtmlManifestAsset
    {
        public string file;
        public string sha256;
        public string sourceName;
        public int width;
        public int height;
    }
}
