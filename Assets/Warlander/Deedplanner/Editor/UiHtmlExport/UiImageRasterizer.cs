using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Editor.UiHtmlExport
{
    internal static class UiImageRasterizer
    {
        private const int ExportLayer = 30;

        public static byte[] Render(UnityEngine.UI.Graphic source, int width, int height)
        {
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            GameObject cameraObject = null;
            GameObject canvasObject = null;
            RenderTexture previousActive = RenderTexture.active;

            try
            {
                renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.sRGB);
                renderTexture.filterMode = FilterMode.Bilinear;

                cameraObject = new GameObject("uGUI HTML Export Camera", typeof(Camera));
                cameraObject.hideFlags = HideFlags.HideAndDontSave;
                cameraObject.layer = ExportLayer;
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.orthographic = true;
                camera.orthographicSize = height * 0.5f;
                camera.aspect = width / (float)height;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 20;
                camera.cullingMask = 1 << ExportLayer;
                camera.targetTexture = renderTexture;
                camera.transform.position = new Vector3(100000, 100000, -10);

                canvasObject = new GameObject("uGUI HTML Export Canvas", typeof(RectTransform), typeof(Canvas));
                canvasObject.hideFlags = HideFlags.HideAndDontSave;
                canvasObject.layer = ExportLayer;
                canvasObject.transform.position = new Vector3(100000, 100000, 0);
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                Canvas sourceCanvas = source.canvas;
                canvas.referencePixelsPerUnit = sourceCanvas != null ? sourceCanvas.referencePixelsPerUnit : 100;
                RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(width, height);
                canvasRect.localScale = Vector3.one;

                var graphicObject = new GameObject(source.GetType().Name, typeof(RectTransform),
                    typeof(CanvasRenderer));
                graphicObject.hideFlags = HideFlags.HideAndDontSave;
                graphicObject.layer = ExportLayer;
                RectTransform graphicRect = graphicObject.GetComponent<RectTransform>();
                graphicRect.SetParent(canvasRect, false);
                graphicRect.anchorMin = Vector2.zero;
                graphicRect.anchorMax = Vector2.one;
                graphicRect.offsetMin = Vector2.zero;
                graphicRect.offsetMax = Vector2.zero;

                UnityEngine.UI.Graphic graphic = CreateGraphicCopy(graphicObject, source);
                Color color = source.color;
                color.a *= source.canvasRenderer.GetInheritedAlpha();
                graphic.color = color;
                graphic.raycastTarget = false;
                graphic.SetAllDirty();

                Canvas.ForceUpdateCanvases();
                camera.Render();

                RenderTexture.active = renderTexture;
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                return texture.EncodeToPNG();
            }
            finally
            {
                RenderTexture.active = previousActive;
                if (cameraObject != null)
                    Object.DestroyImmediate(cameraObject);
                if (canvasObject != null)
                    Object.DestroyImmediate(canvasObject);
                if (texture != null)
                    Object.DestroyImmediate(texture);
                if (renderTexture != null)
                    RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static UnityEngine.UI.Graphic CreateGraphicCopy(GameObject target,
            UnityEngine.UI.Graphic source)
        {
            if (source is UnityEngine.UI.Image sourceImage)
            {
                UnityEngine.UI.Image image = target.AddComponent<UnityEngine.UI.Image>();
                image.sprite = sourceImage.sprite;
                image.overrideSprite = sourceImage.overrideSprite;
                image.type = sourceImage.type;
                image.preserveAspect = sourceImage.preserveAspect;
                image.fillCenter = sourceImage.fillCenter;
                image.fillMethod = sourceImage.fillMethod;
                image.fillAmount = sourceImage.fillAmount;
                image.fillClockwise = sourceImage.fillClockwise;
                image.fillOrigin = sourceImage.fillOrigin;
                image.pixelsPerUnitMultiplier = sourceImage.pixelsPerUnitMultiplier;
                image.useSpriteMesh = sourceImage.useSpriteMesh;
                return image;
            }

            if (source is UnityEngine.UI.RawImage sourceRawImage)
            {
                UnityEngine.UI.RawImage rawImage = target.AddComponent<UnityEngine.UI.RawImage>();
                rawImage.texture = sourceRawImage.texture;
                rawImage.uvRect = sourceRawImage.uvRect;
                return rawImage;
            }

            throw new ArgumentException($"Unsupported graphic type: {source.GetType().Name}", nameof(source));
        }
    }
}
