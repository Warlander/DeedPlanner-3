using UnityEngine.CrashReportHandler;
using UnityEngine.Device;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Bridges;
using Warlander.Deedplanner.Graphics;
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

            Container.Bind<ITextureReferenceFactory>().To<TextureReferenceFactory>().AsSingle();
            Container.Bind<IWurmMeshLoader>().To<WurmMeshLoader>().AsSingle();
            Container.Bind<ITextureLoader>().To<AggregateTextureLoader>().AsSingle();
            Container.Bind<IMaterialLoader>().To<MaterialLoader>().AsSingle();
            Container.Bind<IMaterialCache>().To<MaterialCache>().AsSingle();
            Container.Bind<IWurmMaterialLoader>().To<WurmMaterialLoader>().AsSingle();
            Container.Bind<IWurmModelFactory>().To<WurmModelFactory>().AsSingle();
            Container.Bind<BridgePartDataFactory>().AsSingle();
            Container.Bind<IWurmModelLoader>().To<WurmModelLoader>().AsSingle();
        }
    }
}