using System.Collections.Generic;

namespace Warlander.Deedplanner.Logging
{
    public class LoggerSource : ILoggerSource
    {
        public LogLevelFilter Filter { get; }

        private readonly object _loggersLock = new object();
        private readonly Dictionary<LogCategory, ICategoryLogger> _loggers = new Dictionary<LogCategory, ICategoryLogger>();

        public LoggerSource(LogLevelFilter filter)
        {
            Filter = filter;
        }

        public LoggerSource() : this(new LogLevelFilter())
        {
        }

        public ICategoryLogger Create(LogCategory category)
        {
            lock (_loggersLock)
            {
                if (!_loggers.TryGetValue(category, out ICategoryLogger logger))
                {
                    logger = new CategoryLogger(category, Filter);
                    _loggers.Add(category, logger);
                }

                return logger;
            }
        }
    }
}
