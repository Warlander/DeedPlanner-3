namespace Warlogic.Settings
{
    public sealed class BoolSetting : Setting<bool>
    {
        public BoolSetting(string key, string label, bool defaultValue, string description = null, ApplyMode applyMode = ApplyMode.Immediate)
            : base(key, label, defaultValue, description, applyMode)
        {
        }

        protected override string Serialize(bool value)
        {
            return value ? "true" : "false";
        }

        protected override bool Deserialize(string raw)
        {
            return bool.Parse(raw);
        }
    }
}
