using Warlander.Deedplanner.Data;
using System;
using System.Collections.Generic;
using System.Xml;

namespace Warlander.Deedplanner.Docks
{
    public class MapDockCollection
    {
        private readonly Map _map;
        private readonly DockFactory _dockFactory;
        private readonly List<Dock> _docks = new List<Dock>();
        private readonly Dictionary<Tile, Dock> _docksByTile = new Dictionary<Tile, Dock>();

        public IReadOnlyList<Dock> Docks => _docks;

        public event Action DocksChanged;

        public MapDockCollection(Map map, DockFactory dockFactory)
        {
            _map = map;
            _dockFactory = dockFactory;
        }

        public void InitializeDocks(XmlElement mapRoot)
        {
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
            foreach (Dock originalDock in originalMap.Docks)
            {
                int shiftedX = originalDock.Tile.X + addLeft;
                int shiftedY = originalDock.Tile.Y + addBottom;

                if (shiftedX >= 0 && shiftedX < _map.Width && shiftedY >= 0 && shiftedY < _map.Height)
                {
                    Dock dock = _dockFactory.CreateDock(_map, shiftedX, shiftedY, originalDock.Height,
                        originalDock.Floor, originalDock.Support, originalDock.BraceRotation, originalDock.AnchorLevel);
                    Register(dock);
                }
            }

            RevalidateAll();
        }

        public Dock GetDock(Tile tile)
        {
            if (tile == null)
            {
                return null;
            }

            _docksByTile.TryGetValue(tile, out Dock dock);
            return dock;
        }

        public Dock GetDockSharingCorner(Tile corner)
        {
            Dock dock = GetDock(corner);
            if (dock != null)
            {
                return dock;
            }

            dock = GetDock(_map[corner.X - 1, corner.Y]);
            if (dock != null)
            {
                return dock;
            }

            dock = GetDock(_map[corner.X, corner.Y - 1]);
            return dock ?? GetDock(_map[corner.X - 1, corner.Y - 1]);
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
                }

                // Brace validity also depends on neighbor floor heights and wall tops, whose
                // surface heights derive from corners one step further out than the dock's own.
                if (dx >= -1 && dx <= 1 && dy >= -1 && dy <= 1)
                {
                    RevalidateArea(dock.Tile);
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

        public void RevalidateForFloorChange(int x, int y)
        {
            RevalidateArea(_map[x, y]);
        }

        private void RevalidateArea(Tile tile)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
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
