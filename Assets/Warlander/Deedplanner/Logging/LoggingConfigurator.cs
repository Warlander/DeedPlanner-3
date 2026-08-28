using VContainer.Unity;

namespace Warlander.Deedplanner.Logging
{
    public class LoggingConfigurator : IInitializable
    {
        public LoggingConfigurator(LoggerSource source)
        {
        }

        public void Initialize()
        {
            // Intentionally empty after the migration - output matches pre-migration behavior.
            // Per-category tuning goes here, e.g. _source.Filter.SetMinimum(someSystem.Category, LogType.Warning);
        }
    }
}
