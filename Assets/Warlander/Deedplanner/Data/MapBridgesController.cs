using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Data.Bridges;
using Warlander.Deedplanner.Features;

namespace Warlander.Deedplanner.Data
{
    public class MapBridgesController
    {
        private readonly Map _map;
        private readonly BridgeFactory _bridgeFactory;
        private readonly IFeatureStateRetriever _featureStateRetriever;
        private readonly List<Bridge> _bridges = new List<Bridge>();

        public IReadOnlyList<Bridge> Bridges => _bridges;

        public MapBridgesController(Map map, BridgeFactory bridgeFactory, IFeatureStateRetriever featureStateRetriever)
        {
            _map = map;
            _bridgeFactory = bridgeFactory;
            _featureStateRetriever = featureStateRetriever;
        }

        public void InitializeBridges(XmlElement mapRoot)
        {
            if (!_featureStateRetriever.IsFeatureEnabled(Feature.Bridges))
                return;

            XmlNodeList bridgesList = mapRoot.GetElementsByTagName("bridge");
            foreach (XmlElement bridgeElement in bridgesList)
            {
                Bridge bridge = _bridgeFactory.CreateBridge(_map, bridgeElement);
                _bridges.Add(bridge);
            }
        }

        public void InitializeBridgesAfterResize(Map originalMap, int addLeft, int addBottom)
        {
            if (!_featureStateRetriever.IsFeatureEnabled(Feature.Bridges))
                return;

            Vector2Int bridgeShift = new Vector2Int(addLeft, addBottom);

            foreach (Bridge originalMapBridge in originalMap.Bridges)
            {
                Vector2Int firstTileAfterShift = originalMapBridge.FirstTile + bridgeShift;
                Vector2Int secondTileAfterShift = originalMapBridge.SecondTile + bridgeShift;

                if (IsWithinBounds(firstTileAfterShift) && IsWithinBounds(secondTileAfterShift))
                {
                    Bridge movedBridge = _bridgeFactory.CreateBridge(_map, originalMapBridge, bridgeShift);
                    _bridges.Add(movedBridge);
                }
            }
        }

        private bool IsWithinBounds(Vector2Int tile)
        {
            return tile.x >= 0 && tile.x < _map.Width && tile.y >= 0 && tile.y < _map.Height;
        }
    }
}
