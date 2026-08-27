using Warlander.Deedplanner.Logging;

namespace Warlander.Deedplanner.Gui
{
    public class UiLog
    {
        private static readonly LogCategory Category = new LogCategory("Ui");

        public ICategoryLogger Logger { get; }

        public UiLog(ILoggerSource loggerSource)
        {
            Logger = loggerSource.Create(Category);
        }
    }
}
