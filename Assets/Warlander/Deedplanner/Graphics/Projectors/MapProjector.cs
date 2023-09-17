using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Logic.Cameras;
using Zenject;

namespace Warlander.Deedplanner.Graphics.Projectors
{
    public class MapProjector : MonoBehaviour
    {
        [Inject] private GameManager _gameManager;
        
        [SerializeField] private ProjectorColor color = default;
        [SerializeField] private Projector attachedProjector = null;
        
        public ProjectorColor Color => color;

        private int renderCameraId = -1;
        
        public void MarkRenderWithAllCameras()
        {
            renderCameraId = -1;
        }
        
        public void SetRenderCameraId(int id)
        {
            renderCameraId = id;
        }

        public void ProjectTile(Vector2Int tileCoord, TileSelectionTarget target = TileSelectionTarget.Tile)
        {
            int tileX = tileCoord.x;
            int tileY = tileCoord.y;
            const float borderThickness = TileSelection.BorderThickness;
            
            switch (target)
            {
                case TileSelectionTarget.Tile:
                    attachedProjector.transform.position = new Vector3(tileX * 4 + 2, 10000, tileY * 4 + 2);
                    attachedProjector.orthographicSize = 2;
                    attachedProjector.aspectRatio = 1;
                    break;
                case TileSelectionTarget.InnerTile:
                    attachedProjector.transform.position = new Vector3(tileX * 4 + 2, 10000, tileY * 4 + 2);
                    attachedProjector.orthographicSize = 2 - borderThickness * 4;
                    attachedProjector.aspectRatio = 1;
                    break;
                case TileSelectionTarget.BottomBorder:
                    attachedProjector.transform.position = new Vector3(tileX * 4 + 2, 10000, tileY * 4);
                    attachedProjector.orthographicSize = borderThickness * 4;
                    attachedProjector.aspectRatio = 2f / (borderThickness * 4) - (borderThickness * 6);
                    break;
                case TileSelectionTarget.LeftBorder:
                    attachedProjector.transform.position = new Vector3(tileX * 4, 10000, tileY * 4 + 2);
                    attachedProjector.orthographicSize = 2 - (borderThickness * 4);
                    attachedProjector.aspectRatio = (borderThickness * 4) / 1.5f;
                    break;
                case TileSelectionTarget.Corner:
                    attachedProjector.transform.position = new Vector3(tileX * 4, 10000, tileY * 4);
                    attachedProjector.orthographicSize = borderThickness * 4;
                    attachedProjector.aspectRatio = 1;
                    break;
            }
        }

        public void ProjectLine(Vector2Int tileCoord, PlaneAlignment alignment)
        {
            Map map = _gameManager.Map;
            
            switch (alignment)
            {
                case PlaneAlignment.Horizontal:
                    attachedProjector.transform.localPosition = new Vector3(map.Width * 2f, 10000, tileCoord.y * 4);
                    attachedProjector.orthographicSize = 0.25f;
                    attachedProjector.aspectRatio = map.Width * 2 / attachedProjector.orthographicSize;
                    break;
                case PlaneAlignment.Vertical:
                    attachedProjector.transform.localPosition = new Vector3(tileCoord.x * 4, 10000, map.Height * 2f);
                    attachedProjector.orthographicSize = map.Height * 2;
                    attachedProjector.aspectRatio = 0.25f / attachedProjector.orthographicSize;
                    break;
            }
        }

        private void OnWillRenderObject()
        {
            Camera currentCamera = Camera.current;
            MultiCamera currentMultiCamera = currentCamera.GetComponent<MultiCamera>();
            
            bool shouldProjectorBeEnabled = currentMultiCamera != null && (renderCameraId == -1 || currentMultiCamera.ScreenId == renderCameraId);
            attachedProjector.enabled = shouldProjectorBeEnabled;
        }
    }
}