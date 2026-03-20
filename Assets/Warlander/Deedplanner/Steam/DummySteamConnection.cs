using System;
using VContainer.Unity;

namespace Warlander.Deedplanner.Steam
{
    public class DummySteamConnection : ISteamConnection
    {
        public bool Supported => false;
        public bool Connected => false;
        
        void IInitializable.Initialize()
        {
            // Do nothing.
        }
        
        public string GetName()
        {
            return "Unknown";
        }

        void IDisposable.Dispose()
        {
            // Do nothing.
        }
    }
}