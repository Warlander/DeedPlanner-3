using System;
using Zenject;

namespace Warlander.Deedplanner.Steam
{
    public class SteamConnectionFactory : IFactory<ISteamConnection>
    {
        [Inject] private DiContainer _container;
        
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