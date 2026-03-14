using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Bridges;
using Warlander.Deedplanner.Debugging;
using Warlander.Deedplanner.Graphics.Projectors;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Gui.Widgets.Bridges;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Graphics.Water;
using Warlander.Deedplanner.Logic.Cameras;
using Warlander.Deedplanner.Settings;
using Warlander.Deedplanner.Updaters;
using Zenject;

namespace Warlander.Deedplanner.Installers
{
    public class MainSceneInstaller : MonoInstaller
    {
        [SerializeField] private DebugProperties _debugProperties;
        [SerializeField] private LayoutManager _layoutManager;
        [SerializeField] private CameraCoordinator _cameraCoordinator;
        [SerializeField] private AbstractUpdater[] _updaters;
        
        [SerializeField] private BridgeTabSwapper _bridgeTabSwapper;
        
        public override void InstallBindings()
        {
            Container.Bind<LayoutManager>().FromInstance(_layoutManager);
            Container.BindInterfacesAndSelfTo<TabContext>().AsSingle().NonLazy();
            Container.Bind<CameraCoordinator>().FromInstance(_cameraCoordinator).AsSingle();
            Container.Bind<IOutlineCoordinator>().To<OutlineCoordinator>().AsSingle();
            Container.BindInterfacesAndSelfTo<OutlineFeatureBridge>().AsSingle().NonLazy();

            Container.Bind<ICameraController>().To<FppCameraController>().AsTransient();
            Container.Bind<ICameraController>().To<IsoCameraController>().AsTransient();
            Container.Bind<ICameraController>().To<TopCameraController>().AsTransient();
            Container.BindInterfacesAndSelfTo<WaterFacade>().AsSingle().NonLazy();
            
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
            Container.BindInterfacesAndSelfTo<InputSettings>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<MapRenderSettings>().AsSingle();
            
            Container.Bind<MapHandler>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<UndoRedoInputHandler>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<UpdaterCoordinator>().AsSingle().NonLazy();
            Container.BindInterfacesTo<MapProjectorFacade>().AsSingle();

            Container.BindInterfacesTo<StartupMapLoader>().AsSingle().NonLazy();
            
            Container.Bind<OverlayMeshLoader>().AsSingle();
            Container.Bind<HeightmapHandleMeshLoader>().AsSingle();
            
            // Factories.
            Container.Bind<TileFactory>().AsSingle();
            Container.Bind<BridgeFactory>().AsSingle();

            Container.Bind<MapHeightTracker>().AsSingle();
            Container.BindInterfacesAndSelfTo<MapRoofCalculator>().AsSingle();
            
            //Updaters.
            foreach (var updater in _updaters)
                BindUpdater(updater);

            Container.Bind<BridgeTabSwapper>().FromInstance(_bridgeTabSwapper);
        }

        private void BindUpdater(AbstractUpdater updater)
        {
            Container.BindInterfacesAndSelfTo(updater.GetType()).FromInstance(updater).AsSingle();
            Container.Bind<AbstractUpdater>().FromInstance(updater).AsCached();
        }
    }
}