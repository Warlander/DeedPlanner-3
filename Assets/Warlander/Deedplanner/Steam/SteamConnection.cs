#if !DISABLESTEAMWORKS
using System;
using System.Linq;
using System.Text;
using Steamworks;
using UnityEngine;
using Zenject;

namespace Warlander.Deedplanner.Steam
{
    public class SteamConnection : ISteamConnection
    {
        public bool Supported => true;
        public bool Connected => _initialized && SteamAPI.IsSteamRunning();

        private bool _initialized;
        
        void IInitializable.Initialize()
        {
            if (ShouldInitialize() == false)
            {
                return;
            }
            
            _initialized = SteamAPI.Init();
            if (!_initialized)
            {
                Debug.LogError("Failed to initialize Steamworks.NET");
                return;
            }
            
            SteamClient.SetWarningMessageHook(SteamMessageHook);
            Debug.Log("Steamworks.NET initialized and connected to Steam");
        }

        private bool ShouldInitialize()
        {
            if (!Environment.GetCommandLineArgs().Contains("enablesteam") && !Application.isEditor)
            {
                Debug.Log("Program not launched from Steam client or editor.");
                return false;
            }

            if (!SteamAPI.IsSteamRunning())
            {
                Debug.Log("Steam is not running, destroying SteamManager.");
                return false;
            }

            // sanity checks to ensure Steamworks.NET is setup correctly
            if (!Packsize.Test()) {
                Debug.LogError("Packsize Test returned false, the wrong version of Steamworks.NET is being run in this platform.");
                return false;
            }

            if (!DllCheck.Test()) {
                Debug.LogError("DllCheck Test returned false, One or more of the Steamworks binaries seems to be the wrong version.");
                return false;
            }

            return true;
        }
        
        private void SteamMessageHook(int severity, StringBuilder builder)
        {
            switch (severity)
            {
                case 0:
                    Debug.Log(builder);
                    break;
                case 1:
                    Debug.LogWarning(builder);
                    break;
                default:
                    Debug.LogWarning("Unrecognized Steam message severity: " + severity);
                    Debug.LogWarning(builder);
                    break;
            }
        }

        public string GetName()
        {
            return SteamFriends.GetPersonaName();
        }
        
        void IDisposable.Dispose()
        {
            SteamAPI.Shutdown();
        }
    }
}
#endif