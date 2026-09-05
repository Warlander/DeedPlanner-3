using System;

namespace Warlogic.Settings
{
    public sealed class EnumSetting<T> : Setting<T> where T : struct, Enum
    {
        public EnumSetting(string key, string label, T defaultValue, string description = null, ApplyMode applyMode = ApplyMode.Immediate)
            : base(key, label, defaultValue, description, applyMode)
        {
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
