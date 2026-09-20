using Warlogic.Settings;

namespace Warlander.Deedplanner.Settings
{
    public sealed class CropVisibilitySettings
    {
        private readonly ISetting<bool> _showIn3D;
        private readonly ISetting<bool> _showIn2D;
        private readonly ISetting<bool> _showInIsometric;

        public bool ShowIn3D
        {
            get => _showIn3D.Value;
            set => _showIn3D.Value = value;
        }

        public bool ShowIn2D
        {
            get => _showIn2D.Value;
            set => _showIn2D.Value = value;
        }

        public bool ShowInIsometric
        {
            get => _showInIsometric.Value;
            set => _showInIsometric.Value = value;
        }

        internal CropVisibilitySettings(ISetting<bool> showIn3D, ISetting<bool> showIn2D,
            ISetting<bool> showInIsometric)
        {
            _showIn3D = showIn3D;
            _showIn2D = showIn2D;
            _showInIsometric = showInIsometric;
        }
    }
}
