using System.Collections.Generic;
using Warlander.Deedplanner.Bridges;
using Warlander.Deedplanner.Domain.Entities.Caves;
using Warlander.Deedplanner.Domain.Entities.Decorations;
using Warlander.Deedplanner.Docks;
using Warlander.Deedplanner.Domain.Entities.Floors;
using Warlander.Deedplanner.Domain.Entities.Grounds;
using Warlander.Deedplanner.Domain.Entities.Roofs;
using Warlander.Deedplanner.Domain.Entities.Walls;

namespace Warlander.Deedplanner.Domain
{
    public interface IDataCatalog
    {
        GroundData GetGround(string shortName);
        IReadOnlyCollection<GroundData> GetAllGrounds();

        CaveData GetCave(string shortName);
        IReadOnlyCollection<CaveData> GetAllCaves();

        FloorData GetFloor(string shortName);
        IReadOnlyCollection<FloorData> GetAllFloors();

        WallData GetWall(string shortName);
        IReadOnlyCollection<WallData> GetAllWalls();

        RoofData GetRoof(string shortName);
        IReadOnlyCollection<RoofData> GetAllRoofs();

        DecorationData GetDecoration(string shortName);
        IReadOnlyCollection<DecorationData> GetAllDecorations();

        BridgeData GetBridge(string name);
        IReadOnlyCollection<BridgeData> GetAllBridges();

        BridgePavementData GetBridgePavement(string shortName);
        IReadOnlyCollection<BridgePavementData> GetAllBridgePavements();

        DockSupportData GetDockSupport(string shortName);
        IReadOnlyCollection<DockSupportData> GetAllDockSupports();

        GroundData DefaultGroundData { get; }
        GroundData DefaultSecondaryGroundData { get; }
        CaveData DefaultCaveData { get; }
    }
}
