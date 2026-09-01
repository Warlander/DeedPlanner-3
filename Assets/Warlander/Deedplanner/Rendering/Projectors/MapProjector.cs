using Warlander.Deedplanner.Editing;
using Warlander.Deedplanner.Persistence;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Cameras;
using VContainer;

namespace Warlander.Deedplanner.Rendering.Projectors
{
    public class MapProjector : MonoBehaviour, IToggleableMapProjector
    {
        private const float RenderDistance = 20000f;

        [Inject] private MapHandler _mapHandler;

        [SerializeField] private ProjectorColor color = default;
        [SerializeField] private DecalProjector attachedProjector = null;

        public ProjectorColor Color => color;

        private int _renderCameraId = -1;

        private void Awake()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            MultiCamera multiCamera = camera.GetComponent<MultiCamera>();
            bool shouldProjectorBeEnabled = multiCamera != null
                && (_renderCameraId == -1 || multiCamera.ScreenId == _renderCameraId);
            attachedProjector.enabled = shouldProjectorBeEnabled;
        }

        public void Activate() => gameObject.SetActive(true);

        public void Deactivate() => gameObject.SetActive(false);

        public void MarkRenderWithAllCameras()
        {
            _renderCameraId = -1;
        }

        public void SetRenderCameraId(int id)
        {
            _renderCameraId = id;
        }

        public void ProjectTile(Vector2Int tileCoord, TileSelectionTarget target = TileSelectionTarget.Tile)
        {
            int tileX = tileCoord.x;
            int tileY = tileCoord.y;
            const float borderThickness = TileSelection.BorderThickness;
            const float borderThicknessWorld = borderThickness * 4f;

            switch (target)
            {
                case TileSelectionTarget.Tile:
                    attachedProjector.transform.position = new Vector3(tileX * 4 + 2, 500, tileY * 4 + 2);
                    attachedProjector.size = new Vector3(4f, 4f, RenderDistance);
                    break;
                case TileSelectionTarget.InnerTile:
                    float innerTileSize = (2f - borderThicknessWorld) * 2f;
                    attachedProjector.transform.position = new Vector3(tileX * 4 + 2, 500, tileY * 4 + 2);
                    attachedProjector.size = new Vector3(innerTileSize, innerTileSize, RenderDistance);
                    break;
                case TileSelectionTarget.BottomBorder:
                    float bottomBorderWidth = 4f - 12f * borderThicknessWorld * borderThickness;
                    float bottomBorderHeight = borderThicknessWorld * 2f;
                    attachedProjector.transform.position = new Vector3(tileX * 4 + 2, 500, tileY * 4);
                    attachedProjector.size = new Vector3(bottomBorderWidth, bottomBorderHeight, RenderDistance);
                    break;
                case TileSelectionTarget.LeftBorder:
                    float leftBorderHalfLength = 2f - borderThicknessWorld;
                    float leftBorderWidth = 2f * leftBorderHalfLength * (borderThicknessWorld / 1.5f);
                    float leftBorderLength = 2f * leftBorderHalfLength;
                    attachedProjector.transform.position = new Vector3(tileX * 4, 500, tileY * 4 + 2);
                    attachedProjector.size = new Vector3(leftBorderWidth, leftBorderLength, RenderDistance);
                    break;
                case TileSelectionTarget.Corner:
                    float cornerSize = borderThicknessWorld * 2f;
                    attachedProjector.transform.position = new Vector3(tileX * 4, 500, tileY * 4);
                    attachedProjector.size = new Vector3(cornerSize, cornerSize, RenderDistance);
                    break;
            }
        }

        public void ProjectArea(Vector2Int min, Vector2Int max)
        {
            int minX = Mathf.Min(min.x, max.x);
            int maxX = Mathf.Max(min.x, max.x);
            int minY = Mathf.Min(min.y, max.y);
            int maxY = Mathf.Max(min.y, max.y);

            float centerX = (minX + maxX) * 2f + 2f;
            float centerY = (minY + maxY) * 2f + 2f;
            float sizeX = (maxX - minX + 1) * 4f;
            float sizeY = (maxY - minY + 1) * 4f;

            attachedProjector.transform.position = new Vector3(centerX, 500, centerY);
            attachedProjector.size = new Vector3(sizeX, sizeY, RenderDistance);
        }

        public void ProjectLine(Vector2Int tileCoord, PlaneAlignment alignment)
        {
            Map map = _mapHandler.Map;

            switch (alignment)
            {
                case PlaneAlignment.Horizontal:
                    attachedProjector.transform.position = new Vector3(map.Width * 2f, 500, tileCoord.y * 4);
                    attachedProjector.size = new Vector3(map.Width * 4f, 0.5f, RenderDistance);
                    break;
                case PlaneAlignment.Vertical:
                    attachedProjector.transform.position = new Vector3(tileCoord.x * 4, 500, map.Height * 2f);
                    attachedProjector.size = new Vector3(0.5f, map.Height * 4f, RenderDistance);
                    break;
            }
        }

        private void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }
    }
}
