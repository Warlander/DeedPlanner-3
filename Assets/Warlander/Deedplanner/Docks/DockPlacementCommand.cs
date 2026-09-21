using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Editing;
using UnityEngine;

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
                _map.RemoveDock(_replacedDock);
            }

            _map.AddDock(_dock);
        }

        public void Undo()
        {
            _map.RemoveDock(_dock);

            if (_replacedDock)
            {
                _map.AddDock(_replacedDock);
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
