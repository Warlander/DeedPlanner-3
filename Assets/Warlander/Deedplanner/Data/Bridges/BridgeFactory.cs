using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Logic.Outlines;
using VContainer;

namespace Warlander.Deedplanner.Data.Bridges
{
    public class BridgeFactory
    {
        private readonly IOutlineCoordinator _outlineCoordinator;

        [Inject]
        public BridgeFactory(IOutlineCoordinator outlineCoordinator)
        {
            _outlineCoordinator = outlineCoordinator;
        }

        public Bridge CreateBridge(Map map, XmlElement element)
        {
            return new Bridge(map, element, _outlineCoordinator);
        }

        /// <summary>
        /// Used for moving (previously) existing bridges around the map.
        /// </summary>
        public Bridge CreateBridge(Map map, Bridge originalBridge, Vector2Int tileShift)
        {
            return new Bridge(map, originalBridge, tileShift, _outlineCoordinator);
        }

        public Bridge CreateBridge(Map map, TileCoords start, TileCoords end, BridgeData data,
            BridgeType type, int additionalData, string segments)
        {
            return new Bridge(map, start, end, data, type, additionalData, segments, _outlineCoordinator);
        }
    }
}