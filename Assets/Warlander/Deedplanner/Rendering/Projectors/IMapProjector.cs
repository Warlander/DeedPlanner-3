using Warlander.Deedplanner.Editing;
using UnityEngine;

namespace Warlander.Deedplanner.Rendering.Projectors
{
    public interface IMapProjector
    {
        ProjectorColor Color { get; }
        void MarkRenderWithAllCameras();
        void SetRenderCameraId(int id);
        void ProjectTile(Vector2Int tileCoord, TileSelectionTarget target = TileSelectionTarget.Tile);
        void ProjectArea(Vector2Int min, Vector2Int max);
        void ProjectLine(Vector2Int tileCoord, PlaneAlignment alignment);
    }
}
