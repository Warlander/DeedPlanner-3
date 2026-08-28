using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Logic.Outlines;
using Warlander.Deedplanner.Logging;
using VContainer;

namespace Warlander.Deedplanner.Data
{
    public class TileFactory
    {
        private readonly IOutlineCoordinator _outlineCoordinator;
        private readonly ICategoryLogger _logger;

        [Inject]
        public TileFactory(IOutlineCoordinator outlineCoordinator, MapHandler mapHandler)
        {
            _outlineCoordinator = outlineCoordinator;
            _logger = mapHandler.Logger;
        }

        public Tile CreateTile(Map map, int x, int y)
        {
            return new Tile(map, x, y, _outlineCoordinator, _logger);
        }
    }
}