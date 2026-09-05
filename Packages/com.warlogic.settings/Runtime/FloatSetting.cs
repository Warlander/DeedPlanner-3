using System.Globalization;

namespace Warlogic.Settings
{
    public sealed class FloatSetting : Setting<float>
    {
        public float Min { get; }
        public float Max { get; }

        public FloatSetting(string key, string label, float defaultValue, float min = float.NegativeInfinity, float max = float.PositiveInfinity, string description = null, ApplyMode applyMode = ApplyMode.Immediate)
            : base(key, label, defaultValue, description, applyMode)
        {
            Min = min;
            Max = max;
        }

        protected override float PrepareValue(float value)
        {
            return value < Min ? Min : value > Max ? Max : value;
        }

        protected override string Serialize(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        protected override float Deserialize(string raw)
        {
            return float.Parse(raw, CultureInfo.InvariantCulture);
        }
    }
}
