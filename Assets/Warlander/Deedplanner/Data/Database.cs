using System.Collections.Generic;
using Warlander.Deedplanner.Data.Bridges;
using Warlander.Deedplanner.Data.Caves;
using Warlander.Deedplanner.Data.Decorations;
using Warlander.Deedplanner.Data.Docks;
using Warlander.Deedplanner.Data.Floors;
using Warlander.Deedplanner.Data.Grounds;
using Warlander.Deedplanner.Data.Roofs;
using Warlander.Deedplanner.Data.Walls;

namespace Warlander.Deedplanner.Data
{
    public class Database : IDataCatalog
    {
        private readonly Dictionary<string, GroundData> _grounds = new Dictionary<string, GroundData>();
        private readonly Dictionary<string, CaveData> _caves = new Dictionary<string, CaveData>();
        private readonly Dictionary<string, FloorData> _floors = new Dictionary<string, FloorData>();
        private readonly Dictionary<string, WallData> _walls = new Dictionary<string, WallData>();
        private readonly Dictionary<string, RoofData> _roofs = new Dictionary<string, RoofData>();
        private readonly Dictionary<string, DecorationData> _decorations = new Dictionary<string, DecorationData>();
        private readonly Dictionary<string, BridgeData> _bridges = new Dictionary<string, BridgeData>();
        private readonly Dictionary<string, BridgePavementData> _bridgePavements = new Dictionary<string, BridgePavementData>();
        private readonly Dictionary<string, DockSupportData> _dockSupports = new Dictionary<string, DockSupportData>();

        public void AddGround(GroundData data) => _grounds[data.ShortName] = data;
        public GroundData GetGround(string shortName) => _grounds.GetValueOrDefault(shortName);
        public IReadOnlyCollection<GroundData> GetAllGrounds() => _grounds.Values;

        public void AddCave(CaveData data) => _caves[data.ShortName] = data;
        public CaveData GetCave(string shortName) => _caves.GetValueOrDefault(shortName);
        public IReadOnlyCollection<CaveData> GetAllCaves() => _caves.Values;

        public void AddFloor(FloorData data) => _floors[data.ShortName] = data;
        public FloorData GetFloor(string shortName) => _floors.GetValueOrDefault(shortName);
        public IReadOnlyCollection<FloorData> GetAllFloors() => _floors.Values;

        public void AddWall(WallData data) => _walls[data.ShortName] = data;
        public WallData GetWall(string shortName) => _walls.GetValueOrDefault(shortName);
        public IReadOnlyCollection<WallData> GetAllWalls() => _walls.Values;

        public void AddRoof(RoofData data) => _roofs[data.ShortName] = data;
        public RoofData GetRoof(string shortName) => _roofs.GetValueOrDefault(shortName);
        public IReadOnlyCollection<RoofData> GetAllRoofs() => _roofs.Values;

        public void AddDecoration(DecorationData data) => _decorations[data.ShortName] = data;
        public DecorationData GetDecoration(string shortName) => _decorations.GetValueOrDefault(shortName);
        public IReadOnlyCollection<DecorationData> GetAllDecorations() => _decorations.Values;

        public void AddBridge(BridgeData data) => _bridges[data.Name] = data;
        public BridgeData GetBridge(string name) => _bridges.GetValueOrDefault(name);
        public IReadOnlyCollection<BridgeData> GetAllBridges() => _bridges.Values;

        public void AddBridgePavement(BridgePavementData data) => _bridgePavements[data.ShortName] = data;
        public BridgePavementData GetBridgePavement(string shortName) => _bridgePavements.GetValueOrDefault(shortName);
        public IReadOnlyCollection<BridgePavementData> GetAllBridgePavements() => _bridgePavements.Values;

        public void AddDockSupport(DockSupportData data) => _dockSupports[data.ShortName] = data;
        public DockSupportData GetDockSupport(string shortName) => _dockSupports.GetValueOrDefault(shortName);
        public IReadOnlyCollection<DockSupportData> GetAllDockSupports() => _dockSupports.Values;

        public GroundData DefaultGroundData => _grounds["gr"];
        public GroundData DefaultSecondaryGroundData => _grounds["di"];

        public CaveData DefaultCaveData => _caves["sw"];
    }
}
