using System;

namespace Warlogic.Settings
{
    public sealed class EnumSetting<T> : Setting<T>, IEnumOptionLabels where T : struct, Enum
    {
        public Func<T, string> OptionLabel { get; set; }

        public EnumSetting(string key, string label, T defaultValue, string description = null, ApplyMode applyMode = ApplyMode.Immediate)
            : base(key, label, defaultValue, description, applyMode)
        {
        }

        public string GetOptionLabel(object value)
        {
            return OptionLabel?.Invoke((T) value) ?? value.ToString();
        }

        protected override string Serialize(T value)
        {
            return value.ToString();
        }

        protected override T Deserialize(string raw)
        {
            return Enum.Parse<T>(raw, true);
        }
    }
}
