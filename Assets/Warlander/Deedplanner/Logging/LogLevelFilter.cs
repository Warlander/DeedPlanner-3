using System.Collections.Concurrent;
using UnityEngine;

namespace Warlander.Deedplanner.Logging
{
    public class LogLevelFilter
    {
        // LogType numeric values are not severity-ordered, so filtering ranks them explicitly.
        private const int LogRank = 0;
        private const int WarningRank = 1;
        private const int ErrorRank = 2;

        private const int OverrideOffRank = int.MinValue;

        private readonly ConcurrentDictionary<LogCategory, int> _minimumRanks = new ConcurrentDictionary<LogCategory, int>();
        private volatile int _globalOverrideRank = OverrideOffRank;

        public void SetMinimum(LogCategory category, LogType minimum)
        {
            _minimumRanks[category] = SeverityRank(minimum);
        }

        public void SetGlobalOverride(LogType? minimum)
        {
            _globalOverrideRank = minimum.HasValue ? SeverityRank(minimum.Value) : OverrideOffRank;
        }

        public bool IsAllowed(LogCategory category, LogType type)
        {
            if (type == LogType.Exception)
            {
                return true;
            }

            int rank = SeverityRank(type);
            if (rank < _globalOverrideRank)
            {
                return false;
            }

            return !_minimumRanks.TryGetValue(category, out int minimumRank) || rank >= minimumRank;
        }

        private static int SeverityRank(LogType type)
        {
            switch (type)
            {
                case LogType.Log:
                    return LogRank;
                case LogType.Warning:
                    return WarningRank;
                case LogType.Error:
                case LogType.Assert:
                    return ErrorRank;
                default:
                    return ErrorRank;
            }
        }
    }
}
