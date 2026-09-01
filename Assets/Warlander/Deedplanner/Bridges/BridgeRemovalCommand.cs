using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Editing;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Bridges
{
    public class BridgeRemovalCommand : IReversibleCommand
    {
        private readonly Map _map;
        private readonly Bridge _bridge;

        public BridgeRemovalCommand(Map map, Bridge bridge)
        {
            _map = map;
            _bridge = bridge;
        }

        public void Execute()
        {
            _map.RemoveBridge(_bridge);
            _map.RefreshBridgesRendering();
        }

        public void Undo()
        {
            _map.AddBridge(_bridge);
            _map.RefreshBridgesRendering();
        }

        public void DisposeUndo()
        {
            _bridge.Destroy();
        }

        public void DisposeRedo()
        {
        }
    }
}
