using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Data.Bridges;
using Warlander.Deedplanner.Platform.Features;
using Warlogic.Features;

namespace Warlander.Deedplanner.Data
{
    public class MapBridgesController
    {
        private readonly Map _map;
        private readonly BridgeFactory _bridgeFactory;
        private readonly IFeatureStateRetriever<Feature> _featureStateRetriever;
        private readonly List<Bridge> _bridges = new List<Bridge>();

        public IReadOnlyList<Bridge> Bridges => _bridges;

        public event Action BridgesChanged;

        public MapBridgesController(Map map, BridgeFactory bridgeFactory, IFeatureStateRetriever<Feature> featureStateRetriever)
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
                if (bridge != null)
                {
                    _bridges.Add(bridge);
                }
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

        public void AddBridge(Bridge bridge)
        {
            _bridges.Add(bridge);
            BridgesChanged?.Invoke();
        }

        public void RemoveBridge(Bridge bridge)
        {
            _bridges.Remove(bridge);
            BridgesChanged?.Invoke();
        }

        public void RefreshBridgesForSurfaceHeight(int x, int y)
        {
            foreach (Bridge bridge in _bridges)
            {
                if (bridge.HasSurfaceAnchor(x, y))
                {
                    bridge.RefreshHeights(_map);
                    continue;
                }

                foreach (BridgePart part in bridge.Parts)
                {
                    if (part.PartType != BridgePartType.Support || part.Tile == null)
                    {
                        continue;
                    }

                    int dx = x - part.Tile.X;
                    int dy = y - part.Tile.Y;
                    if (dx >= 0 && dx <= 1 && dy >= 0 && dy <= 1)
                    {
                        part.RefreshExtensions();
                    }
                }
            }
        }

        public void RefreshBridgesForCaveHeight(int x, int y)
        {
            foreach (Bridge bridge in _bridges)
            {
                if (bridge.HasCaveAnchor(x, y))
                {
                    bridge.RefreshHeights(_map);
                }
            }
        }

        private bool IsWithinBounds(Vector2Int tile)
        {
            return tile.x >= 0 && tile.x < _map.Width && tile.y >= 0 && tile.y < _map.Height;
        }
    }
}
