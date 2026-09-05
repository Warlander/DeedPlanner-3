namespace Warlogic.Settings
{
    public sealed class StringSetting : Setting<string>
    {
        public StringSetting(string key, string label, string defaultValue, string description = null, ApplyMode applyMode = ApplyMode.Immediate)
            : base(key, label, defaultValue, description, applyMode)
        {
        }

        protected override string Serialize(string value)
        {
            return value;
        }

        protected override string Deserialize(string raw)
        {
            return raw;
        }
    }
}
