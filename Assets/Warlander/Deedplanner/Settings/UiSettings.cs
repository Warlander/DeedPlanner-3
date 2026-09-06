using System;
using Warlogic.Settings;

namespace Warlander.Deedplanner.Settings
{
    public sealed class UiSettings
    {
        private readonly ISetting<int> _guiScale;
        private readonly ISetting<bool> _compassVisibility;

        public event Action GuiScaleChanged
        {
            add => _guiScale.Changed += value;
            remove => _guiScale.Changed -= value;
        }

        public int GuiScale
        {
            get => _guiScale.Value;
            set => _guiScale.Value = value;
        }

        public bool CompassVisibility
        {
            get => _compassVisibility.Value;
            set => _compassVisibility.Value = value;
        }

        internal UiSettings(ISetting<int> guiScale, ISetting<bool> compassVisibility)
        {
            _guiScale = guiScale;
            _compassVisibility = compassVisibility;
        }
    }
}
