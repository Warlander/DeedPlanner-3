using Warlander.Deedplanner.Logic;
using Zenject;

namespace Warlander.Deedplanner.Installers
{
    public class MainSceneInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            Container.BindInterfacesAndSelfTo<FileDragManager>().AsSingle().NonLazy();
#endif
        }
    }
}