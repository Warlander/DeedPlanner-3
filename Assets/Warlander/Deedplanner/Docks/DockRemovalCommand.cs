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
            _map.RemoveDock(_dock);
        }

        public void Undo()
        {
            _map.AddDock(_dock);
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
