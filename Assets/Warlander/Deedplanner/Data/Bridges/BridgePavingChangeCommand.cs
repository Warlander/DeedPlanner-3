using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data.Bridges
{
    public class BridgePavingChangeCommand : IReversibleCommand
    {
        private readonly Bridge _bridge;
        private readonly BridgePavementData[] _oldPavements;
        private readonly BridgePavementData[] _newPavements;

        public BridgePavingChangeCommand(Bridge bridge, BridgePavementData[] oldPavements,
            BridgePavementData[] newPavements)
        {
            _bridge = bridge;
            _oldPavements = oldPavements;
            _newPavements = newPavements;
        }

        public void Execute()
        {
            _bridge.SetPavements((BridgePavementData[])_newPavements.Clone());
        }

        public void Undo()
        {
            _bridge.SetPavements((BridgePavementData[])_oldPavements.Clone());
        }

        public void DisposeUndo()
        {
        }

        public void DisposeRedo()
        {
        }
    }
}
