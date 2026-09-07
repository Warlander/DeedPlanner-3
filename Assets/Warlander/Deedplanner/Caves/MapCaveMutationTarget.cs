using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Domain.Entities.Caves;
using Warlander.Deedplanner.Editing;

namespace Warlander.Deedplanner.Caves
{
    internal sealed class MapCaveMutationTarget : ICaveMutationTarget
    {
        private readonly Map _map;

        public MapCaveMutationTarget(Map map)
        {
            _map = map;
        }

        public bool ContainsCell(int x, int y)
        {
            return x >= 0 && y >= 0 && x < _map.Width && y < _map.Height;
        }

        public bool ContainsVertex(int x, int y)
        {
            return x >= 0 && y >= 0 && x <= _map.Width && y <= _map.Height;
        }

        public CaveData GetTerrain(int x, int y)
        {
            return _map[x, y].Cave.Terrain;
        }

        public int GetFloorHeightAtVertex(int x, int y)
        {
            return _map[x, y].Cave.FloorHeight;
        }

        public int GetClearanceAtVertex(int x, int y)
        {
            return _map[x, y].Cave.Clearance;
        }

        public bool HasCaveContent(int x, int y)
        {
            return _map[x, y].HasCaveContent();
        }

        public ICaveContentRemoval CreateCaveContentRemoval(int x, int y)
        {
            return _map[x, y].CreateCaveContentRemoval();
        }

        public void SetTerrain(int x, int y, CaveData terrain)
        {
            _map[x, y].ApplyCaveTerrain(terrain);
        }

        public void SetFloorHeightAtVertex(int x, int y, int height)
        {
            _map[x, y].ApplyCaveFloorHeight(height);
        }

        public void SetClearanceAtVertex(int x, int y, int clearance)
        {
            _map[x, y].ApplyCaveClearance(clearance);
        }

        public void Record(IReversibleCommand command)
        {
            _map.CommandManager.AddToStack(command);
        }
    }
}
