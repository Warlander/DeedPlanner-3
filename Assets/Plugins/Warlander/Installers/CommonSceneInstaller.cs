using Warlander.UI.Windows;
using Zenject;

namespace Warlander.Installers
{
    /// <summary>
    /// Add this to each scene SceneContext.
    /// </summary>
    public class CommonSceneInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<WindowCoordinator>().AsSingle();
        }
    }
}