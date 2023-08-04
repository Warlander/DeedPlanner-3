using Warlander.Deedplanner.Gui.Tooltips;
using Zenject;

namespace Warlander.Deedplanner.Installers
{
    /// <summary>
    /// Per-scene installer, on every scene.
    /// </summary>
    public class DPSceneInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<TooltipHandler>().AsSingle().Lazy();
        }
    }
}