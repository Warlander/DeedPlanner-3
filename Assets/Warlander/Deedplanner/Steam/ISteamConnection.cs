#if UNITY_ANDROID || UNITY_IOS || UNITY_TIZEN || UNITY_TVOS || UNITY_WEBGL || UNITY_WSA || UNITY_PS4 || UNITY_WII || UNITY_XBOXONE || UNITY_SWITCH
    #define DISABLESTEAMWORKS
#else
    #undef DISABLESTEAMWORKS
#endif

using System;
using Zenject;

namespace Warlander.Deedplanner.Steam
{
    public interface ISteamConnection : IInitializable, IDisposable
    {
        bool Supported { get; }
        bool Connected { get; }

        public string GetName();
    }
}