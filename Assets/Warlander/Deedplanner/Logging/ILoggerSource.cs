namespace Warlander.Deedplanner.Logging
{
    public interface ILoggerSource
    {
        ICategoryLogger Create(LogCategory category);
    }
}
