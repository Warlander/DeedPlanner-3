#if !DISABLESTEAMWORKS
using System;
using System.Linq;
using System.Text;
using Steamworks;
using UnityEngine;
using VContainer.Unity;
using Warlander.Deedplanner.Logging;

namespace Warlander.Deedplanner.Steam
{
    public class SteamConnection : ISteamConnection
    {
        public static readonly LogCategory Category = new LogCategory("Steam");

        public bool Supported => true;
        public bool Connected => _initialized && SteamAPI.IsSteamRunning();

        private readonly ICategoryLogger _logger;
        private bool _initialized;

        public SteamConnection(ILoggerSource loggerSource)
        {
            _logger = loggerSource.Create(Category);
        }

        void IInitializable.Initialize()
        {
            if (ShouldInitialize() == false)
            {
                return;
            }
            
            _initialized = SteamAPI.Init();
            if (!_initialized)
            {
                _logger.Error("Failed to initialize Steamworks.NET");
                return;
            }
            
            SteamClient.SetWarningMessageHook(SteamMessageHook);
            _logger.Message("Steamworks.NET initialized and connected to Steam");
        }

        private bool ShouldInitialize()
        {
            if (!Environment.GetCommandLineArgs().Contains("enablesteam") && !Application.isEditor)
            {
                _logger.Message("Program not launched from Steam client or editor.");
                return false;
            }

            if (!SteamAPI.IsSteamRunning())
            {
                _logger.Message("Steam is not running, destroying SteamManager.");
                return false;
            }

            // sanity checks to ensure Steamworks.NET is setup correctly
            if (!Packsize.Test()) {
                _logger.Error("Packsize Test returned false, the wrong version of Steamworks.NET is being run in this platform.");
                return false;
            }

            if (!DllCheck.Test()) {
                _logger.Error("DllCheck Test returned false, One or more of the Steamworks binaries seems to be the wrong version.");
                return false;
            }

            return true;
        }
        
        private void SteamMessageHook(int severity, StringBuilder builder)
        {
            switch (severity)
            {
                case 0:
                    _logger.Message(builder.ToString());
                    break;
                case 1:
                    _logger.Warning(builder.ToString());
                    break;
                default:
                    _logger.Warning("Unrecognized Steam message severity: " + severity);
                    _logger.Warning(builder.ToString());
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