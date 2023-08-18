using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Settings;
using Warlander.Deedplanner.Steam;
using Zenject;

namespace Warlander.Deedplanner.Installers
{
    public class ProjectInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<DPSettings>().FromFactory<SettingsFactory>().AsSingle();

            Container.BindInterfacesAndSelfTo<ISteamConnection>().FromFactory<ISteamConnection, SteamConnectionFactory>().AsSingle().NonLazy();
        }
    }
}