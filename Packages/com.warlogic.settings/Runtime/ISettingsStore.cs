namespace Warlogic.Settings
{
    public interface ISettingsStore
    {
        bool TryLoad(string key, out string value);
        void Save(string key, string value);
    }
}
