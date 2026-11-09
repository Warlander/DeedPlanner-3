using System;
using Warlander.Deedplanner.Ui;
using Warlogic.Settings;

namespace Warlander.Deedplanner.Settings
{
    public sealed class GraphicsOptions
    {
        private readonly ISetting<WaterQuality> _waterQuality;
        private readonly ISetting<QualityLevel> _qualityLevel;
        private readonly ISetting<bool> _surfaceHeightDetail;

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

        public event Action SurfaceHeightDetailChanged
        {
            add => _surfaceHeightDetail.Changed += value;
            remove => _surfaceHeightDetail.Changed -= value;
        }

        public WaterQuality WaterQuality
        {
            get => _waterQuality.Value;
            set => _waterQuality.Value = value;
        }

        public QualityLevel QualityLevel
        {
            get => _qualityLevel.Value;
            set => _qualityLevel.Value = value;
        }

        public bool SurfaceHeightDetail
        {
            get => _surfaceHeightDetail.Value;
            set => _surfaceHeightDetail.Value = value;
        }

        internal GraphicsOptions(ISetting<WaterQuality> waterQuality, ISetting<QualityLevel> qualityLevel,
            ISetting<bool> surfaceHeightDetail)
        {
            _waterQuality = waterQuality;
            _qualityLevel = qualityLevel;
            _surfaceHeightDetail = surfaceHeightDetail;
        }
    }
}
