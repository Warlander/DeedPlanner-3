using Plugins.Warlander.Utils;
using Zenject;

namespace Warlander.Installers
{
    /// <summary>
    /// Add this to Zenject ProjectContext of the project.
    /// </summary>
    public class CommonProjectInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<UnityThreadRunner>().FromNewComponentOnNewGameObject().AsSingle();
        }
    }
}