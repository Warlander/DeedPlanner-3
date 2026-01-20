using Warlander.Deedplanner.Logic;
using Zenject;

namespace Warlander.Deedplanner.Data
{
    public class TileFactory
    {
        private readonly IOutlineCoordinator _outlineCoordinator;

        [Inject]
        public TileFactory(IOutlineCoordinator outlineCoordinator)
        {
            _outlineCoordinator = outlineCoordinator;
        }
        
        public Tile CreateTile(Map map, int x, int y)
        {
            return new Tile(map, x, y, _outlineCoordinator);
        }
    }
}