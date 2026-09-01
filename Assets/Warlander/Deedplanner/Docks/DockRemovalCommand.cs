using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Editing;
using UnityEngine;

namespace Warlander.Deedplanner.Docks
{
    public class DockRemovalCommand : IReversibleCommand
    {
        private readonly Map _map;
        private readonly Dock _dock;

        public DockRemovalCommand(Map map, Dock dock)
        {
            _map = map;
            _dock = dock;
        }

        public void Execute()
        {
            _dock.Tile.UnregisterDock();
            _map.RemoveDock(_dock);
            _dock.gameObject.SetActive(false);
        }

        public void Undo()
        {
            _dock.Tile.RegisterDock(_dock);
            _map.AddDock(_dock);
            _dock.gameObject.SetActive(true);
        }

        public void DisposeUndo()
        {
            Object.Destroy(_dock.gameObject);
        }

        public void DisposeRedo()
        {
        }
    }
}
