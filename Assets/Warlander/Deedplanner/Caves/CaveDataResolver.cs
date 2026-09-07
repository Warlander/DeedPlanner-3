using System;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Domain.Entities.Caves;

namespace Warlander.Deedplanner.Caves
{
    public sealed class CaveDataResolver : ICaveDataResolver
    {
        private readonly IDataCatalog _dataCatalog;

        public CaveDataResolver(IDataCatalog dataCatalog)
        {
            _dataCatalog = dataCatalog ?? throw new ArgumentNullException(nameof(dataCatalog));
        }

        public CaveData Resolve(string shortName)
        {
            CaveData terrain = string.IsNullOrEmpty(shortName) ? null : _dataCatalog.GetCave(shortName);
            return terrain == null || terrain.Entrance ? _dataCatalog.DefaultCaveData : terrain;
        }
    }
}
