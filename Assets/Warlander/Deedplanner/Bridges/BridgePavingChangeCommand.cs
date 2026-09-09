using System;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Editing;

namespace Warlander.Deedplanner.Bridges
{
    public class BridgePavingChangeCommand : IReversibleCommand
    {
        private readonly Bridge[] _bridges;
        private readonly int[] _segmentIndices;
        private readonly int[] _laneIndices;
        private readonly BridgePavementData[] _oldPavements;
        private readonly BridgePavementData[] _newPavements;

        public BridgePavingChangeCommand(BridgePart[] parts, BridgePavementData[] oldPavements,
            BridgePavementData[] newPavements)
        {
            _bridges = new Bridge[parts.Length];
            _segmentIndices = new int[parts.Length];
            _laneIndices = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                _bridges[i] = parts[i].ParentBridge;
                _segmentIndices[i] = parts[i].SegmentIndex;
                _laneIndices[i] = parts[i].LaneIndex;
            }

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
            for (int i = 0; i < _bridges.Length; i++)
            {
                BridgePart part = _bridges[i].GetPart(_segmentIndices[i], _laneIndices[i]);
                if (part == null)
                {
                    throw new InvalidOperationException(
                        $"Bridge part {_segmentIndices[i]}:{_laneIndices[i]} no longer exists");
                }

                part.SetPavement(pavements[i]);
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
