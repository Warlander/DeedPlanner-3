#if UNITY_ANDROID || UNITY_IOS || UNITY_TIZEN || UNITY_TVOS || UNITY_WEBGL || UNITY_WSA || UNITY_PS4 || UNITY_WII || UNITY_XBOXONE || UNITY_SWITCH
    #define DISABLESTEAMWORKS
#else
    #undef DISABLESTEAMWORKS
#endif

using System;
using System.Linq;
using System.Text;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif
using UnityEngine;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Logic
{
    public class SteamManager : MonoBehaviour
    {
        public static bool ConnectedToSteam
        {
            get
            {
                bool connected = Instance;
#if !DISABLESTEAMWORKS
                connected = connected && SteamAPI.IsSteamRunning();
#endif
                return connected;
            }
        }

        private static SteamManager Instance { get; set; }
        
        private void Awake()
        {
            bool shouldInitializeSteamworks = CheckPrerequisites();
            if (!shouldInitializeSteamworks)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

#if !DISABLESTEAMWORKS
            bool steamworksInitialized = SteamAPI.Init();
            if (!steamworksInitialized)
            {
                Debug.LogError("Failed to initialize Steamworks.NET");
                Destroy(gameObject);
                return;
            }
            
            SteamClient.SetWarningMessageHook(SteamMessageHook);
            Debug.Log("Steamworks.NET initialized and connected to Steam");
#endif
        }

#if !DISABLESTEAMWORKS
        private void Update()
        {
            SteamAPI.RunCallbacks();
        }
#endif

        private static void SteamMessageHook(int severity, StringBuilder builder)
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

        private bool CheckPrerequisites()
        {
            if (Instance)
            {
                return false;
            }
            
            // checks to check if Steam connection should be initialized
            if (SystemInfo.deviceType != DeviceType.Desktop)
            {
                Debug.Log("Current platform is not Steam compatible, destroying SteamManager.", this);
                return false;
            }
            
#if !DISABLESTEAMWORKS
            if (!Environment.GetCommandLineArgs().Contains("enablesteam") && !Application.isEditor)
            {
                Debug.Log("Program not launched from Steam client and not editor, destroying SteamManager.", this);
                return false;
            }

            if (!SteamAPI.IsSteamRunning())
            {
                Debug.Log("Steam is not running, destroying SteamManager.", this);
                return false;
            }

            // sanity checks to ensure Steamworks.NET is setup correctly
            if (!Packsize.Test()) {
                Debug.LogError("[Steamworks.NET] Packsize Test returned false, the wrong version of Steamworks.NET is being run in this platform.", this);
                return false;
            }

            if (!DllCheck.Test()) {
                Debug.LogError("[Steamworks.NET] DllCheck Test returned false, One or more of the Steamworks binaries seems to be the wrong version.", this);
                return false;
            }
#endif

#if !DISABLESTEAMWORKS
            return true;
#else
            // project compiled or editor target set for Steamworks-incompatible platform
            return false;
#endif
        }

        private void OnDestroy()
        {
            if (!Instance)
            {
                return;
            }

            Instance = null;
#if !DISABLESTEAMWORKS
            SteamAPI.Shutdown();
#endif
        }
    }
}