using System;
using System.Linq;
using System.Text;
using Steamworks;
using UnityEngine;

namespace Warlander.Deedplanner.Logic
{
    public class SteamManager : MonoBehaviour
    {

        public static bool ConnectedToSteam => Instance && SteamAPI.IsSteamRunning();

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

            bool steamworksInitialized = SteamAPI.Init();
            if (!steamworksInitialized)
            {
                Debug.LogError("Failed to initialize Steamworks.NET");
                Destroy(gameObject);
                return;
            }
            
            SteamClient.SetWarningMessageHook(SteamMessageHook);
            Debug.Log("Steamworks.NET initialized and connected to Steam");
        }

        private void Update()
        {
            SteamAPI.RunCallbacks();
        }

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
            if (!IsSteamCompatiblePlatform(Application.platform))
            {
                Debug.Log("Current platform is not Steam compatible, destroying SteamManager.", this);
                return false;
            }

            if (!Environment.GetCommandLineArgs().Contains("enablesteam") && !Application.isEditor)
            {
                Debug.Log("Program not launched from Steam client and not editor, destroying SteamManager.", this);
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

            return true;
        }

        private static bool IsSteamCompatiblePlatform(RuntimePlatform platform)
        {
            bool isWindows = platform == RuntimePlatform.WindowsPlayer || platform == RuntimePlatform.WindowsEditor;
            bool isLinux = platform == RuntimePlatform.LinuxPlayer || platform == RuntimePlatform.LinuxEditor;
            bool isMac = platform == RuntimePlatform.OSXPlayer || platform == RuntimePlatform.OSXEditor;

            return isWindows || isLinux || isMac;
        }
        
        private void OnDestroy()
        {
            if (!Instance)
            {
                return;
            }

            Instance = null;
            SteamAPI.Shutdown();
        }

    }
}