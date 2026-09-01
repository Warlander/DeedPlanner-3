using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Editing;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Bridges
{
    public class BridgeExtraArgumentChangeCommand : IReversibleCommand
    {
        private readonly Map _map;
        private readonly Bridge _bridge;
        private readonly int _oldValue;
        private readonly int _newValue;

        public BridgeExtraArgumentChangeCommand(Map map, Bridge bridge, int oldValue, int newValue)
        {
            _map = map;
            _bridge = bridge;
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public void Execute()
        {
            _bridge.Rebuild(_map, _bridge.Data, _bridge.GetSegmentsString(), _newValue);
            _map.RefreshBridgesRendering();
        }

        public void Undo()
        {
            _bridge.Rebuild(_map, _bridge.Data, _bridge.GetSegmentsString(), _oldValue);
            _map.RefreshBridgesRendering();
        }

        public void DisposeUndo()
        {
        }

        public void DisposeRedo()
        {
        }
    }
}
