using System;
using Warlander.Deedplanner.Ui;
using Warlogic.Settings;

namespace Warlander.Deedplanner.Settings
{
    public sealed class GraphicsOptions
    {
        private readonly ISetting<WaterQuality> _waterQuality;
        private readonly ISetting<QualityLevel> _qualityLevel;

        public event Action WaterQualityChanged
        {
            add => _waterQuality.Changed += value;
            remove => _waterQuality.Changed -= value;
        }

        public event Action QualityLevelChanged
        {
            add => _qualityLevel.Changed += value;
            remove => _qualityLevel.Changed -= value;
        }

        public WaterQuality WaterQuality
        {
            get => _waterQuality.Value;
            set => _waterQuality.Value = value;
        }

        public QualityLevel QualityLevel
        {
            get => _qualityLevel.Value;
            set
            {
                // quality level is staged (ApplyMode.OnSave); translator commits so callers see immediate semantics
                _qualityLevel.Value = value;
                _qualityLevel.Commit();
            }
        }

        internal GraphicsOptions(ISetting<WaterQuality> waterQuality, ISetting<QualityLevel> qualityLevel)
        {
            _waterQuality = waterQuality;
            _qualityLevel = qualityLevel;
        }
    }
}
