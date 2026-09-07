using Warlander.Deedplanner.Caves;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Domain.Entities.Grounds;
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
        private readonly ICaveDataResolver _caveDataResolver;
        private readonly IGroundDataResolver _groundDataResolver;

        [Inject]
        public TileFactory(IOutlineCoordinator outlineCoordinator, IDataCatalog dataCatalog, MapHandler mapHandler,
            ICaveDataResolver caveDataResolver)
        {
            _outlineCoordinator = outlineCoordinator;
            _dataCatalog = dataCatalog;
            _logger = mapHandler.Logger;
            _caveDataResolver = caveDataResolver;
            _groundDataResolver = new GroundDataResolver(dataCatalog, _logger);
        }

        public Tile CreateTile(Map map, int x, int y)
        {
            return new Tile(map, x, y, _outlineCoordinator, _dataCatalog, _caveDataResolver, _groundDataResolver,
                _logger);
        }
    }
}
