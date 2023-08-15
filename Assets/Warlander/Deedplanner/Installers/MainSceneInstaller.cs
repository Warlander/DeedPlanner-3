using UnityEngine;
using Warlander.Deedplanner.Debugging;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Logic.Cameras;
using Warlander.Deedplanner.Logic.Projectors;
using Zenject;

namespace Warlander.Deedplanner.Installers
{
    public class MainSceneInstaller : MonoInstaller
    {
        [SerializeField] private DebugProperties _debugProperties;
        [SerializeField] private LayoutManager _layoutManager;
        [SerializeField] private CameraCoordinator _cameraCoordinator;
        
        public override void InstallBindings()
        {
            Container.Bind<LayoutManager>().FromInstance(_layoutManager);
            Container.Bind<CameraCoordinator>().FromInstance(_cameraCoordinator).AsSingle();

            Container.Bind<ICameraController>().To<FppCameraController>().AsTransient();
            Container.Bind<ICameraController>().To<IsoCameraController>().AsTransient();
            Container.Bind<ICameraController>().To<TopCameraController>().AsTransient();
            
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            Container.BindInterfacesAndSelfTo<FileDragManager>().AsSingle().NonLazy();
#endif

            if (Debug.isDebugBuild)
            {
                Container.BindInterfacesAndSelfTo<DebugApplier>().AsSingle().NonLazy();
                Container.Bind<DebugProperties>().FromInstance(_debugProperties);
            }

            DPInput input = new DPInput();
            input.Enable();
            Container.Bind<DPInput>().FromInstance(input).AsSingle();

            Container.Bind<GameManager>().FromComponentInHierarchy().AsSingle();
            Container.Bind<MapProjectorManager>().FromComponentInHierarchy().AsSingle();
        }
    }
}