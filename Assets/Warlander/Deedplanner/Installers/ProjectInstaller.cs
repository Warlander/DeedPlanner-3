using Warlander.Deedplanner.Settings;
using Zenject;

namespace Warlander.Deedplanner.Installers
{
    public class ProjectInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<DPSettings>().FromFactory<SettingsFactory>().AsSingle();
        }
    }
}