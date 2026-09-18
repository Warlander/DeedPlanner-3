using UnityEngine;
using Warlander.Deedplanner.Caves;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Domain.Entities.Caves;
using Warlander.Deedplanner.Domain.Entities.Decorations;
using Warlander.Deedplanner.Domain.Entities.Floors;
using Warlander.Deedplanner.Domain.Entities.Grounds;
using Warlander.Deedplanner.Domain.Entities.Roofs;
using Warlander.Deedplanner.Domain.Entities.Walls;
using Warlander.Deedplanner.Docks;
using Warlander.Deedplanner.Bridges;
using System;

namespace Warlander.Deedplanner.Editing
{
    public interface IMapEditFacade
    {
        void PaintGround(int x, int y, GroundData data, RoadDirection direction);
        void ReplaceGroundData(int x, int y, GroundData data);
        ICaveEditStroke BeginCaveTerrainStroke(CaveData data, CaveOccupiedCellPolicy occupiedCellPolicy);
        IMapHeightEdit BeginHeightEdit(bool cave, CaveHeightMode caveMode, bool preserveCeilingHeight,
            HeightEditBehavior behavior);
        void SetFloor(int x, int y, FloorData data, EntityOrientation orientation, int level);
        void SetRoof(int x, int y, RoofData data, int level);
        void SetHorizontalWall(int x, int y, WallData data, bool reversed, int level);
        void SetVerticalWall(int x, int y, WallData data, bool reversed, int level);
        Decoration SetDecoration(int x, int y, DecorationData data, Vector2 localPosition, float rotation,
            int level, bool floatOnWater = false);
        IMapDockStroke BeginDockStroke(int anchorX, int anchorY);
        Bridge PlaceBridge(BridgePlacementRequest request);
        void RemoveBridge(Bridge bridge);
        void ChangeBridgeMaterial(Bridge bridge, BridgeData material, string newSegments);
        void ChangeBridgeExtraArgument(Bridge bridge, int value);
        void ChangeBridgeSegments(Bridge bridge, string newSegments);
        IBridgePavingStroke BeginBridgePavingStroke();
        void RecordBridgePaving(BridgePart[] parts, BridgePavementData[] oldPavements,
            BridgePavementData[] newPavements);
        void FinishAction();
        void CancelAction();
    }
}
