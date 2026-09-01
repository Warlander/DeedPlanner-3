using Warlander.Deedplanner.Editing;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data.Bridges
{
    public class BridgePlacementCommand : IReversibleCommand
    {
        private readonly Map _map;
        private readonly Bridge _bridge;

        public BridgePlacementCommand(Map map, Bridge bridge)
        {
            _map = map;
            _bridge = bridge;
        }

        public void Execute()
        {
            _map.AddBridge(_bridge);
            _map.RefreshBridgesRendering();
        }

        public void Undo()
        {
            _map.RemoveBridge(_bridge);
            _map.RefreshBridgesRendering();
        }

        public void DisposeUndo()
        {
        }

        public void DisposeRedo()
        {
            _bridge.Destroy();
        }
    }
}
