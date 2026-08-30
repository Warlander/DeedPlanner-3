using System.Threading.Tasks;
using UnityEngine;
using Warlander.Deedplanner.Graphics.Screenshots;
using Warlander.Deedplanner.Logic.Cameras;

namespace Warlander.Deedplanner.Logic
{
    public class CurrentViewScreenshotCapture : IScreenshotCapture
    {
        public const int ScreenshotHeight = 2160;

        private readonly CameraCoordinator _cameraCoordinator;
        private readonly IScreenshotRenderer _screenshotRenderer;

        public CurrentViewScreenshotCapture(CameraCoordinator cameraCoordinator, IScreenshotRenderer screenshotRenderer)
        {
            _cameraCoordinator = cameraCoordinator;
            _screenshotRenderer = screenshotRenderer;
        }

        public Task<Texture2D> CaptureAsync()
        {
            return Task.FromResult(CaptureCurrentView());
        }

        public Texture2D CaptureCurrentView()
        {
            MultiCamera multiCamera = _cameraCoordinator.Current;
            Camera camera = multiCamera.AttachedCamera;

            int height = ScreenshotHeight;
            int width = Mathf.RoundToInt(height * camera.aspect);

            var request = new ScreenshotRequest(
                camera.worldToCameraMatrix,
                camera.projectionMatrix,
                camera.clearFlags,
                camera.backgroundColor,
                multiCamera.Level,
                multiCamera.RenderEntireMap,
                multiCamera.CameraController,
                width,
                height,
                camera.nearClipPlane,
                camera.farClipPlane);

            return _screenshotRenderer.TakeScreenshot(request);
        }
    }
}
