using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Persistence;
using Warlander.Deedplanner.Rendering.Outline;
using Warlander.Deedplanner.Logging;
using VContainer;

namespace Warlander.Deedplanner.Domain
{
    public class TileFactory
    {
        private readonly IOutlineCoordinator _outlineCoordinator;
        private readonly IDataCatalog _dataCatalog;
        private readonly ICategoryLogger _logger;

        [Inject]
        public TileFactory(IOutlineCoordinator outlineCoordinator, IDataCatalog dataCatalog, MapHandler mapHandler)
        {
            _outlineCoordinator = outlineCoordinator;
            _dataCatalog = dataCatalog;
            _logger = mapHandler.Logger;
        }

        public Tile CreateTile(Map map, int x, int y)
        {
            return new Tile(map, x, y, _outlineCoordinator, _dataCatalog, _logger);
        }
    }
}
