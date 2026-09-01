using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Cameras;

namespace Warlander.Deedplanner.Screenshots
{
    public class DeedThumbnailCapture
    {
        public const int ThumbnailWidth = 560;
        public const int ThumbnailHeight = 400;
        public const int JpegQuality = 60;
        public const int MaxJpegBytes = 100 * 1024;

        private const float TileSize = 4f;
        private const float HeightUnitsToMeters = 0.1f;
        private const float FloorHeight = 3f;
        private const int FloorsAboveDeed = 16;
        private const float VerticalFov = 45f;

        private readonly CameraCoordinator _cameraCoordinator;
        private readonly IScreenshotRenderer _screenshotRenderer;

        public DeedThumbnailCapture(CameraCoordinator cameraCoordinator, IScreenshotRenderer screenshotRenderer)
        {
            _cameraCoordinator = cameraCoordinator;
            _screenshotRenderer = screenshotRenderer;
        }

        public byte[] CaptureJpeg(Map map)
        {
            Texture2D texture = Capture(map);
            if (texture == null)
            {
                return null;
            }

            try
            {
                byte[] jpeg = ImageConversion.EncodeToJPG(texture, JpegQuality);
                int quality = JpegQuality;
                while (jpeg.Length > MaxJpegBytes && quality > 20)
                {
                    quality -= 10;
                    jpeg = ImageConversion.EncodeToJPG(texture, quality);
                }

                return jpeg;
            }
            finally
            {
                Object.Destroy(texture);
            }
        }

        private Texture2D Capture(Map map)
        {
            float mapWidth = map.Width * TileSize;
            float mapDepth = map.Height * TileSize;
            float cameraHeight = map.HighestSurfaceHeight * HeightUnitsToMeters + FloorsAboveDeed * FloorHeight;

            float cornerDistance = Mathf.Sqrt(mapWidth * mapWidth + mapDepth * mapDepth);
            float cornerAngle = Mathf.Atan2(cameraHeight, cornerDistance) * Mathf.Rad2Deg;
            float pitch = cornerAngle + VerticalFov / 2f;
            float yaw = Mathf.Atan2(mapWidth, mapDepth) * Mathf.Rad2Deg;

            // ScreenshotRequest matrices must come from a real Camera: hand-built
            // TRS().inverse misses Unity's view-space convention and renders skybox only.
            GameObject cameraObject = new GameObject("ThumbnailCamera");
            cameraObject.hideFlags = HideFlags.HideInHierarchy;
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            try
            {
                camera.fieldOfView = VerticalFov;
                camera.aspect = (float) ThumbnailWidth / ThumbnailHeight;
                camera.nearClipPlane = 0.3f;
                camera.farClipPlane = cameraHeight + cornerDistance + 200f;
                camera.transform.SetPositionAndRotation(
                    new Vector3(0f, cameraHeight, 0f), Quaternion.Euler(pitch, yaw, 0f));

                var request = new ScreenshotRequest(
                    camera.worldToCameraMatrix, camera.projectionMatrix,
                    CameraClearFlags.Skybox, Color.black,
                    0, true, _cameraCoordinator.Current.CameraController,
                    ThumbnailWidth, ThumbnailHeight, camera.nearClipPlane, camera.farClipPlane);

                return _screenshotRenderer.TakeScreenshot(request);
            }
            finally
            {
                Object.Destroy(cameraObject);
            }
        }
    }
}
