using Warlander.Deedplanner.Logging;

namespace Warlander.Deedplanner.Platform.Steam
{
    public class SteamConnectionFactory
    {
        private readonly ILoggerSource _loggerSource;

        public SteamConnectionFactory(ILoggerSource loggerSource)
        {
            _loggerSource = loggerSource;
        }

        public ISteamConnection Create()
        {
#if DISABLESTEAMWORKS
            return new DummySteamConnection();
#else
            return new SteamConnection(_loggerSource);
#endif
        }
    }
}
