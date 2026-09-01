using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Editing;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Bridges
{
    public class BridgePavingChangeCommand : IReversibleCommand
    {
        private readonly BridgePart[] _parts;
        private readonly BridgePavementData[] _oldPavements;
        private readonly BridgePavementData[] _newPavements;

        public BridgePavingChangeCommand(BridgePart[] parts, BridgePavementData[] oldPavements,
            BridgePavementData[] newPavements)
        {
            _parts = parts;
            _oldPavements = oldPavements;
            _newPavements = newPavements;
        }

        public void Execute()
        {
            Apply(_newPavements);
        }

        public void Undo()
        {
            Apply(_oldPavements);
        }

        private void Apply(BridgePavementData[] pavements)
        {
            for (int i = 0; i < _parts.Length; i++)
            {
                _parts[i].SetPavement(pavements[i]);
            }
        }

        public void DisposeUndo()
        {
        }

        public void DisposeRedo()
        {
        }
    }
}
