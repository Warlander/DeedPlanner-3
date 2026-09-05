using System.Globalization;

namespace Warlogic.Settings
{
    public sealed class IntSetting : Setting<int>
    {
        public int Min { get; }
        public int Max { get; }

        public IntSetting(string key, string label, int defaultValue, int min = int.MinValue, int max = int.MaxValue, string description = null, ApplyMode applyMode = ApplyMode.Immediate)
            : base(key, label, defaultValue, description, applyMode)
        {
            Min = min;
            Max = max;
        }

        protected override int PrepareValue(int value)
        {
            return value < Min ? Min : value > Max ? Max : value;
        }

        protected override string Serialize(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        protected override int Deserialize(string raw)
        {
            return int.Parse(raw, CultureInfo.InvariantCulture);
        }
    }
}
