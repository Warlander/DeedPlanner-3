using Warlogic.Settings;

namespace Warlander.Deedplanner.Settings
{
    public sealed class CameraSettings
    {
        private readonly ISetting<float> _fppMouseSensitivity;
        private readonly ISetting<float> _fppKeyboardRotationSensitivity;
        private readonly ISetting<float> _fppMovementSpeed;
        private readonly ISetting<float> _topMovementSpeed;
        private readonly ISetting<float> _isoMovementSpeed;
        private readonly ISetting<float> _shiftSpeedModifier;
        private readonly ISetting<float> _controlSpeedModifier;

        public float FppMouseSensitivity => _fppMouseSensitivity.Value;
        public float FppKeyboardRotationSensitivity => _fppKeyboardRotationSensitivity.Value;
        public float FppMovementSpeed => _fppMovementSpeed.Value;
        public float TopMovementSpeed => _topMovementSpeed.Value;
        public float IsoMovementSpeed => _isoMovementSpeed.Value;
        public float ShiftSpeedModifier => _shiftSpeedModifier.Value;
        public float ControlSpeedModifier => _controlSpeedModifier.Value;

        internal CameraSettings(
            ISetting<float> fppMouseSensitivity, ISetting<float> fppKeyboardRotationSensitivity,
            ISetting<float> fppMovementSpeed, ISetting<float> topMovementSpeed, ISetting<float> isoMovementSpeed,
            ISetting<float> shiftSpeedModifier, ISetting<float> controlSpeedModifier)
        {
            _fppMouseSensitivity = fppMouseSensitivity;
            _fppKeyboardRotationSensitivity = fppKeyboardRotationSensitivity;
            _fppMovementSpeed = fppMovementSpeed;
            _topMovementSpeed = topMovementSpeed;
            _isoMovementSpeed = isoMovementSpeed;
            _shiftSpeedModifier = shiftSpeedModifier;
            _controlSpeedModifier = controlSpeedModifier;
        }
    }
}
