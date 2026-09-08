using Warlogic.Settings;
using Warlander.Deedplanner.Caves;

namespace Warlander.Deedplanner.Settings
{
    public sealed class EditingSettings
    {
        private readonly ISetting<float> _heightDragSensitivity;
        private readonly ISetting<bool> _heightRespectOriginalSlopes;
        private readonly ISetting<bool> _wallAutomaticReverse;
        private readonly ISetting<bool> _wallReverse;
        private readonly ISetting<bool> _decorationSnapToGrid;
        private readonly ISetting<bool> _decorationRotationSnapping;
        private readonly ISetting<float> _decorationRotationSensitivity;
        private readonly ISetting<CaveOccupiedCellPolicy> _caveOccupiedCellPolicy;

        public float HeightDragSensitivity
        {
            get => _heightDragSensitivity.Value;
            set => _heightDragSensitivity.Value = value;
        }

        public bool HeightRespectOriginalSlopes
        {
            get => _heightRespectOriginalSlopes.Value;
            set => _heightRespectOriginalSlopes.Value = value;
        }

        public bool WallAutomaticReverse
        {
            get => _wallAutomaticReverse.Value;
            set => _wallAutomaticReverse.Value = value;
        }

        public bool WallReverse
        {
            get => _wallReverse.Value;
            set => _wallReverse.Value = value;
        }

        public bool DecorationSnapToGrid
        {
            get => _decorationSnapToGrid.Value;
            set => _decorationSnapToGrid.Value = value;
        }

        public bool DecorationRotationSnapping
        {
            get => _decorationRotationSnapping.Value;
            set => _decorationRotationSnapping.Value = value;
        }

        public float DecorationRotationSensitivity
        {
            get => _decorationRotationSensitivity.Value;
            set => _decorationRotationSensitivity.Value = value;
        }

        public CaveOccupiedCellPolicy CaveOccupiedCellPolicy
        {
            get => _caveOccupiedCellPolicy.Value;
            set => _caveOccupiedCellPolicy.Value = value;
        }

        internal EditingSettings(
            ISetting<float> heightDragSensitivity, ISetting<bool> heightRespectOriginalSlopes,
            ISetting<bool> wallAutomaticReverse, ISetting<bool> wallReverse,
            ISetting<bool> decorationSnapToGrid, ISetting<bool> decorationRotationSnapping,
            ISetting<float> decorationRotationSensitivity,
            ISetting<CaveOccupiedCellPolicy> caveOccupiedCellPolicy)
        {
            _heightDragSensitivity = heightDragSensitivity;
            _heightRespectOriginalSlopes = heightRespectOriginalSlopes;
            _wallAutomaticReverse = wallAutomaticReverse;
            _wallReverse = wallReverse;
            _decorationSnapToGrid = decorationSnapToGrid;
            _decorationRotationSnapping = decorationRotationSnapping;
            _decorationRotationSensitivity = decorationRotationSensitivity;
            _caveOccupiedCellPolicy = caveOccupiedCellPolicy;
        }
    }
}
