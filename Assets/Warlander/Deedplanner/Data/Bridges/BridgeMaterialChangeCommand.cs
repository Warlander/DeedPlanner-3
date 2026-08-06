using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data.Bridges
{
    public class BridgeMaterialChangeCommand : IReversibleCommand
    {
        private readonly Map _map;
        private readonly Bridge _bridge;
        private readonly BridgeData _oldMaterial;
        private readonly BridgeData _newMaterial;
        private readonly string _oldSegments;
        private readonly string _newSegments;

        public BridgeMaterialChangeCommand(Map map, Bridge bridge, BridgeData oldMaterial,
            BridgeData newMaterial, string oldSegments, string newSegments)
        {
            _map = map;
            _bridge = bridge;
            _oldMaterial = oldMaterial;
            _newMaterial = newMaterial;
            _oldSegments = oldSegments;
            _newSegments = newSegments;
        }

        public void Execute()
        {
            _bridge.Rebuild(_map, _newMaterial, _newSegments);
            _map.RefreshBridgesRendering();
        }

        public void Undo()
        {
            _bridge.Rebuild(_map, _oldMaterial, _oldSegments);
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
