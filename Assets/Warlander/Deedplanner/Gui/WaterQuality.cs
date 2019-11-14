namespace Warlander.Deedplanner.Gui
{
    public enum WaterQuality
    {
        Simple = 0,
        High = 1,
        Ultra = 2,
        // extra values for legacy deserialization
        // ReSharper disable InconsistentNaming
        SIMPLE = Simple,
        HIGH = High,
        ULTRA = Ultra
        // ReSharper restore InconsistentNaming
    }
}
