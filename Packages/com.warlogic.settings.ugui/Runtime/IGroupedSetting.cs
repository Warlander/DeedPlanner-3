namespace Warlogic.Settings.Ugui
{
    /// <summary>
    /// Optional setting marker: settings with the same consecutive Group value get a group header
    /// inserted before them when tab content is built.
    /// </summary>
    public interface IGroupedSetting
    {
        string Group { get; }
    }
}
