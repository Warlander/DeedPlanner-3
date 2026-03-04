using UnityEngine;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Graphics.Projectors
{
    public interface IMapProjector
    {
        ProjectorColor Color { get; }
        void MarkRenderWithAllCameras();
        void SetRenderCameraId(int id);
        void ProjectTile(Vector2Int tileCoord, TileSelectionTarget target = TileSelectionTarget.Tile);
        void ProjectLine(Vector2Int tileCoord, PlaneAlignment alignment);
    }
}
