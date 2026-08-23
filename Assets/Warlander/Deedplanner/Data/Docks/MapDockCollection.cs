using System;
using System.Collections.Generic;
using System.Xml;
using Warlander.Deedplanner.Features;
using Warlogic.Features;

namespace Warlander.Deedplanner.Data.Docks
{
    public class MapDockCollection
    {
        private readonly Map _map;
        private readonly DockFactory _dockFactory;
        private readonly IFeatureStateRetriever<Feature> _featureStateRetriever;
        private readonly List<Dock> _docks = new List<Dock>();
        private readonly Dictionary<Tile, Dock> _docksByTile = new Dictionary<Tile, Dock>();

        public IReadOnlyList<Dock> Docks => _docks;

        public event Action DocksChanged;

        public MapDockCollection(Map map, DockFactory dockFactory, IFeatureStateRetriever<Feature> featureStateRetriever)
        {
            _map = map;
            _dockFactory = dockFactory;
            _featureStateRetriever = featureStateRetriever;
        }

        public void InitializeDocks(XmlElement mapRoot)
        {
            if (!_featureStateRetriever.IsFeatureEnabled(Feature.Docks))
                return;

            XmlNodeList docksList = mapRoot.GetElementsByTagName("dock");
            foreach (XmlElement dockElement in docksList)
            {
                Dock dock = _dockFactory.CreateDock(_map, dockElement);
                if (dock != null)
                {
                    Register(dock);
                }
            }
        }

        public void InitializeDocksAfterResize(Map originalMap, int addLeft, int addBottom)
        {
            if (!_featureStateRetriever.IsFeatureEnabled(Feature.Docks))
                return;

            foreach (Dock originalDock in originalMap.Docks)
            {
                int shiftedX = originalDock.Tile.X + addLeft;
                int shiftedY = originalDock.Tile.Y + addBottom;

                if (shiftedX >= 0 && shiftedX < _map.Width && shiftedY >= 0 && shiftedY < _map.Height)
                {
                    Dock dock = _dockFactory.CreateDock(_map, shiftedX, shiftedY, originalDock.Height,
                        originalDock.Floor, originalDock.Support, originalDock.BraceRotation);
                    Register(dock);
                }
            }
        }

        public Dock GetDock(Tile tile)
        {
            _docksByTile.TryGetValue(tile, out Dock dock);
            return dock;
        }

        public void AddDock(Dock dock)
        {
            Register(dock);
            DocksChanged?.Invoke();
        }

        public void RemoveDock(Dock dock)
        {
            _docks.Remove(dock);
            _docksByTile.Remove(dock.Tile);
            DocksChanged?.Invoke();
        }

        private void Register(Dock dock)
        {
            _docks.Add(dock);
            _docksByTile[dock.Tile] = dock;
        }
    }
}
