using UnityEngine;
using Warlander.Deedplanner.Debugging;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Logic;
using Zenject;

namespace Warlander.Deedplanner.Installers
{
    public class MainSceneInstaller : MonoInstaller
    {
        [SerializeField] private DebugProperties _debugProperties;
        [SerializeField] private LayoutManager _layoutManager;
        
        public override void InstallBindings()
        {
            Container.Bind<LayoutManager>().FromInstance(_layoutManager);
            
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            Container.BindInterfacesAndSelfTo<FileDragManager>().AsSingle().NonLazy();
#endif

            if (Debug.isDebugBuild)
            {
                Container.BindInterfacesAndSelfTo<DebugApplier>().AsSingle().NonLazy();
                Container.Bind<DebugProperties>().FromInstance(_debugProperties);
            }
        }
    }
}