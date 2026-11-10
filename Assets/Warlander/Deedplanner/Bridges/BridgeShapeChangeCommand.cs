using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Editing;

namespace Warlander.Deedplanner.Bridges
{
    public sealed class BridgeShapeChangeCommand : IReversibleCommand
    {
        private readonly Map _map;
        private readonly Bridge _bridge;
        private readonly BridgeShapeState _oldState;
        private readonly BridgeShapeState _newState;

        public BridgeShapeChangeCommand(Map map, Bridge bridge, BridgeShapeState newState)
        {
            _map = map;
            _bridge = bridge;
            _oldState = bridge.CaptureShapeState();
            _newState = newState;
        }

        public void Execute() => _bridge.ApplyShapeState(_map, _newState);
        public void Undo() => _bridge.ApplyShapeState(_map, _oldState);
        public void DisposeUndo() { }
        public void DisposeRedo() { }
    }
}
