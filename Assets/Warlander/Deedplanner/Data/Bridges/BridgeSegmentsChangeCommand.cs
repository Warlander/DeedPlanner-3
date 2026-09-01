using Warlander.Deedplanner.Editing;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data.Bridges
{
    public class BridgeSegmentsChangeCommand : IReversibleCommand
    {
        private readonly Map _map;
        private readonly Bridge _bridge;
        private readonly string _oldSegments;
        private readonly string _newSegments;

        public BridgeSegmentsChangeCommand(Map map, Bridge bridge, string oldSegments, string newSegments)
        {
            _map = map;
            _bridge = bridge;
            _oldSegments = oldSegments;
            _newSegments = newSegments;
        }

        public void Execute()
        {
            _bridge.Rebuild(_map, _bridge.Data, _newSegments, _bridge.AdditionalData);
            _map.RefreshBridgesRendering();
        }

        public void Undo()
        {
            _bridge.Rebuild(_map, _bridge.Data, _oldSegments, _bridge.AdditionalData);
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
