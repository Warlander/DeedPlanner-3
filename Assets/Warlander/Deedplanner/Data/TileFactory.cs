using Warlander.Deedplanner.Logic;
using Zenject;

namespace Warlander.Deedplanner.Data
{
    public class TileFactory
    {
        [Inject] private OutlineCoordinator _outlineCoordinator;
        
        public Tile CreateTile(Map map, int x, int y)
        {
            return new Tile(map, x, y, _outlineCoordinator);
        }
    }
}