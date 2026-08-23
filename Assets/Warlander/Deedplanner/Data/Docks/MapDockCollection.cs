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

            RevalidateAll();
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

            RevalidateAll();
        }

        public Dock GetDock(Tile tile)
        {
            _docksByTile.TryGetValue(tile, out Dock dock);
            return dock;
        }

        public void RefreshDocksForSurfaceHeight(int x, int y)
        {
            foreach (Dock dock in _docks)
            {
                int dx = x - dock.Tile.X;
                int dy = y - dock.Tile.Y;
                if (dx >= 0 && dx <= 1 && dy >= 0 && dy <= 1)
                {
                    dock.RefreshSupportExtensions();
                    dock.Revalidate();
                }
            }
        }

        public void AddDock(Dock dock)
        {
            Register(dock);
            RevalidateArea(dock.Tile);
            DocksChanged?.Invoke();
        }

        public void RemoveDock(Dock dock)
        {
            Tile tile = dock.Tile;
            _docks.Remove(dock);
            _docksByTile.Remove(dock.Tile);
            RevalidateArea(tile);
            DocksChanged?.Invoke();
        }

        public void RevalidateAll()
        {
            foreach (Dock dock in _docks)
            {
                dock.Revalidate();
            }
        }

        public void RevalidateForWallChange(int x, int y, bool vertical)
        {
            RevalidateDockAt(x, y);
            if (vertical)
            {
                RevalidateDockAt(x, y + 1);
            }
            else
            {
                RevalidateDockAt(x + 1, y);
            }
        }

        private void RevalidateArea(Tile tile)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx != 0 && dy != 0)
                    {
                        continue;
                    }

                    RevalidateDockAt(tile.X + dx, tile.Y + dy);
                }
            }
        }

        private void RevalidateDockAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _map.Width || y >= _map.Height)
            {
                return;
            }

            Dock dock = GetDock(_map[x, y]);
            if (dock != null)
            {
                dock.Revalidate();
            }
        }

        private void Register(Dock dock)
        {
            _docks.Add(dock);
            _docksByTile[dock.Tile] = dock;
        }
    }
}
