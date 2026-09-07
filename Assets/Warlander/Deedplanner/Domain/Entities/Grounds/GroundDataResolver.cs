using System;
using Warlander.Deedplanner.Logging;

namespace Warlander.Deedplanner.Domain.Entities.Grounds
{
    public sealed class GroundDataResolver : IGroundDataResolver
    {
        private readonly IDataCatalog _dataCatalog;
        private readonly ICategoryLogger _logger;

        public GroundDataResolver(IDataCatalog dataCatalog, ICategoryLogger logger)
        {
            _dataCatalog = dataCatalog ?? throw new ArgumentNullException(nameof(dataCatalog));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public GroundData Resolve(string shortName)
        {
            GroundData data = string.IsNullOrEmpty(shortName) ? null : _dataCatalog.GetGround(shortName);
            if (data != null)
            {
                return data;
            }

            if (!string.IsNullOrEmpty(shortName))
            {
                _logger.Warning("Unable to load ground " + shortName + ", using default instead");
            }

            return _dataCatalog.DefaultGroundData;
        }
    }
}
