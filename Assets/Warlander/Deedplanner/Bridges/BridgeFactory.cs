using System;
using Warlander.Deedplanner.Data;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Rendering.Outline;
using Warlander.Deedplanner.Logging;
using VContainer;

namespace Warlander.Deedplanner.Bridges
{
    public class BridgeFactory
    {
        public static readonly LogCategory Category = new LogCategory("Bridges");

        private readonly IOutlineCoordinator _outlineCoordinator;
        private readonly IDataCatalog _dataCatalog;
        private readonly ICategoryLogger _logger;

        [Inject]
        public BridgeFactory(IOutlineCoordinator outlineCoordinator, IDataCatalog dataCatalog, ILoggerSource loggerSource)
        {
            _outlineCoordinator = outlineCoordinator;
            _dataCatalog = dataCatalog;
            _logger = loggerSource.Create(Category);
        }

        public Bridge CreateBridge(Map map, XmlElement element)
        {
            try
            {
                return new Bridge(map, element, _dataCatalog, _outlineCoordinator, _logger);
            }
            catch (Exception e)
            {
                _logger.Warning("Unable to load bridge: " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// Used for moving (previously) existing bridges around the map.
        /// </summary>
        public Bridge CreateBridge(Map map, Bridge originalBridge, Vector2Int tileShift)
        {
            return new Bridge(map, originalBridge, tileShift, _dataCatalog, _outlineCoordinator, _logger);
        }

        public Bridge CreateBridge(Map map, TileCoords start, TileCoords end, BridgeData data,
            BridgeType type, int additionalData, string segments)
        {
            return new Bridge(map, start, end, data, type, additionalData, segments, _dataCatalog, _outlineCoordinator, _logger);
        }
    }
}
