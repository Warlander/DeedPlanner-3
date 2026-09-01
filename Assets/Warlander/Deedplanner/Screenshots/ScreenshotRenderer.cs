using Warlander.Deedplanner.Persistence;
using UnityEngine;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Rendering.Water;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Rendering.Outline;
using Warlander.Deedplanner.Settings;

namespace Warlander.Deedplanner.Screenshots
{
    public class ScreenshotRenderer : IScreenshotRenderer
    {
        private readonly MapHandler _mapHandler;
        private readonly IWaterFacade _waterFacade;
        private readonly IOutlineCoordinator _outlineCoordinator;
        private readonly DPSettings _settings;

        private Camera _screenshotCamera;

        public ScreenshotRenderer(MapHandler mapHandler, IWaterFacade waterFacade,
            IOutlineCoordinator outlineCoordinator, DPSettings settings)
        {
            _mapHandler = mapHandler;
            _waterFacade = waterFacade;
            _outlineCoordinator = outlineCoordinator;
            _settings = settings;
        }

        public Texture2D TakeScreenshot(ScreenshotRequest request)
        {
            Map map = _mapHandler.Map;
            if (map == null)
            {
                return null;
            }

            WaterQuality previousWaterQuality = _settings.WaterQuality;
            bool previousRenderGrid = map.RenderGrid;

            _outlineCoordinator.RenderingSuspended = true;
            map.RenderGrid = false;
            _settings.Modify(settings => settings.WaterQuality = WaterQuality.Ultra, autoSave: false);

            try
            {
                map.RenderedLevel = request.Level;
                map.RenderEntireMap = request.RenderEntireMap;

                Camera camera = GetOrCreateCamera();
                // Shaders read camera position from the transform (_WorldSpaceCameraPos),
                // not from the view matrix — both must be set or view-dependent lighting breaks.
                Matrix4x4 cameraToWorld = request.WorldToCamera.inverse;
                camera.transform.SetPositionAndRotation(cameraToWorld.GetColumn(3), cameraToWorld.rotation);
                // Clip planes and aspect must be set before the matrices: assigning any of them
                // resets a custom projection. URP culling reads the clip plane properties, not
                // the custom matrix — leaving them at defaults clips the render early.
                camera.nearClipPlane = request.NearClip;
                camera.farClipPlane = request.FarClip;
                camera.aspect = request.Width / (float) request.Height;
                camera.worldToCameraMatrix = request.WorldToCamera;
                camera.projectionMatrix = request.Projection;
                camera.clearFlags = request.ClearFlags;
                camera.backgroundColor = request.BackgroundColor;

                RenderTexture renderTexture = new RenderTexture(request.Width, request.Height, 24);
                camera.targetTexture = renderTexture;

                bool renderWater = request.RenderEntireMap || request.Level == 0 || request.Level == -1;
                _waterFacade.PrepareForCamera(camera, request.CameraController, renderWater);

                camera.Render();

                RenderTexture.active = renderTexture;
                Texture2D result = new Texture2D(request.Width, request.Height, TextureFormat.RGBA32, false);
                result.ReadPixels(new Rect(0, 0, request.Width, request.Height), 0, 0);
                result.Apply();
                RenderTexture.active = null;

                camera.targetTexture = null;
                renderTexture.Release();

                return result;
            }
            finally
            {
                _settings.Modify(settings => settings.WaterQuality = previousWaterQuality, autoSave: false);
                map.RenderGrid = previousRenderGrid;
                _outlineCoordinator.RenderingSuspended = false;
            }
        }

        private Camera GetOrCreateCamera()
        {
            if (_screenshotCamera == null)
            {
                GameObject cameraObject = new GameObject("ScreenshotCamera");
                cameraObject.hideFlags = HideFlags.HideInHierarchy;
                _screenshotCamera = cameraObject.AddComponent<Camera>();
                _screenshotCamera.enabled = false;
            }

            return _screenshotCamera;
        }
    }
}
