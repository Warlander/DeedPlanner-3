using UnityEngine.CrashReportHandler;
using UnityEngine.Device;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Settings;
using Warlander.Deedplanner.Steam;
using Warlander.Deedplanner.Utils;
using Zenject;

namespace Warlander.Deedplanner.Installers
{
    public class ProjectInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            // Disable exception reporting as soon as possible if in editor,
            // before any other code could throw an exception.
            // We do this to prevent crash reporting a bad data.
            if (Application.isEditor)
            {
                CrashReportHandler.enableCaptureExceptions = false;
            }
            
            Container.Bind<DPSettings>().FromFactory<SettingsFactory>().AsSingle();

            Container.BindInterfacesAndSelfTo<ISteamConnection>().FromFactory<ISteamConnection, SteamConnectionFactory>().AsSingle().NonLazy();

            Container.BindInterfacesAndSelfTo<DefaultTargetFrameRateSetter>().AsSingle().NonLazy();
        }
    }
}