using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Screenshots
{
    public class BackbufferScreenshotCapture : IScreenshotCapture
    {
        public const int ScreenshotHeight = 2160;

        public async Task<Texture2D> CaptureAsync()
        {
            int superSize = Mathf.Max(1, Mathf.CeilToInt((float) ScreenshotHeight / Screen.height));
            Texture2D raw = ScreenCapture.CaptureScreenshotAsTexture(superSize);
            await Awaitable.EndOfFrameAsync();

            int targetWidth = Mathf.RoundToInt((float) ScreenshotHeight * Screen.width / Screen.height);
            if (raw.width == targetWidth && raw.height == ScreenshotHeight)
            {
                return raw;
            }

            RenderTexture renderTexture = RenderTexture.GetTemporary(targetWidth, ScreenshotHeight);
            UnityEngine.Graphics.Blit(raw, renderTexture);
            UnityEngine.Object.Destroy(raw);

            RenderTexture.active = renderTexture;
            Texture2D result = new Texture2D(targetWidth, ScreenshotHeight, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, targetWidth, ScreenshotHeight), 0, 0);
            result.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(renderTexture);

            return result;
        }
    }
}
