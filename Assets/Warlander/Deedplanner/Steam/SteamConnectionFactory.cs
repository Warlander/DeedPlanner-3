namespace Warlander.Deedplanner.Steam
{
    public class SteamConnectionFactory
    {
        public ISteamConnection Create()
        {
#if DISABLESTEAMWORKS
            return new DummySteamConnection();
#else
            return new SteamConnection();
#endif
        }
    }
}
