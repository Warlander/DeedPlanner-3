using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Editing;
using UnityEngine;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Docks
{
    public class DockPlacementCommand : IReversibleCommand
    {
        private readonly Map _map;
        private readonly Dock _dock;
        private readonly Dock _replacedDock;

        public DockPlacementCommand(Map map, Dock dock, Dock replacedDock)
        {
            _map = map;
            _dock = dock;
            _replacedDock = replacedDock;
        }

        public void Execute()
        {
            if (_replacedDock)
            {
                _replacedDock.Tile.UnregisterDock();
                _map.RemoveDock(_replacedDock);
                _replacedDock.gameObject.SetActive(false);
            }

            _dock.Tile.RegisterDock(_dock);
            _map.AddDock(_dock);
            _dock.gameObject.SetActive(true);
        }

        public void Undo()
        {
            _dock.Tile.UnregisterDock();
            _map.RemoveDock(_dock);
            _dock.gameObject.SetActive(false);

            if (_replacedDock)
            {
                _replacedDock.Tile.RegisterDock(_replacedDock);
                _map.AddDock(_replacedDock);
                _replacedDock.gameObject.SetActive(true);
            }
        }

        public void DisposeUndo()
        {
            if (_replacedDock)
            {
                Object.Destroy(_replacedDock.gameObject);
            }
        }

        public void DisposeRedo()
        {
            Object.Destroy(_dock.gameObject);
        }
    }
}
