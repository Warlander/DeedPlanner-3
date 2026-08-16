using UnityEngine;
using Warlander.Deedplanner.Logic.Cameras;

namespace Warlander.Deedplanner.Graphics.Screenshots
{
    public readonly struct ScreenshotRequest
    {
        public readonly Matrix4x4 WorldToCamera;
        public readonly Matrix4x4 Projection;
        public readonly CameraClearFlags ClearFlags;
        public readonly Color BackgroundColor;
        public readonly int Level;
        public readonly bool RenderEntireMap;
        public readonly ICameraController CameraController;
        public readonly int Width;
        public readonly int Height;
        public readonly float NearClip;
        public readonly float FarClip;

        public ScreenshotRequest(Matrix4x4 worldToCamera, Matrix4x4 projection, CameraClearFlags clearFlags,
            Color backgroundColor, int level, bool renderEntireMap, ICameraController cameraController,
            int width, int height, float nearClip, float farClip)
        {
            WorldToCamera = worldToCamera;
            Projection = projection;
            ClearFlags = clearFlags;
            BackgroundColor = backgroundColor;
            Level = level;
            RenderEntireMap = renderEntireMap;
            CameraController = cameraController;
            Width = width;
            Height = height;
            NearClip = nearClip;
            FarClip = farClip;
        }
    }

    public interface IScreenshotRenderer
    {
        /// Returns null when no map is loaded. Caller owns the returned texture and must Destroy it.
        Texture2D TakeScreenshot(ScreenshotRequest request);
    }
}
