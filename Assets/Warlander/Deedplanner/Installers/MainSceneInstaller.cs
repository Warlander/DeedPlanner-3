using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Bridges;
using Warlander.Deedplanner.Debugging;
using Warlander.Deedplanner.Graphics.Projectors;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Gui.Widgets.Bridges;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Logic;
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
        [SerializeField] private MapProjectorManager _mapProjectorManager;

        [SerializeField] private GroundUpdater _groundUpdater;
        [SerializeField] private CaveUpdater _caveUpdater;
        [SerializeField] private HeightUpdater _heightUpdater;
        [SerializeField] private FloorUpdater _floorsUpdater;
        [SerializeField] private WallUpdater _wallUpdater;
        [SerializeField] private RoofUpdater _roofUpdater;
        [SerializeField] private DecorationUpdater _decorationUpdater;
        [SerializeField] private LabelUpdater _labelUpdater;
        [SerializeField] private BorderUpdater _borderUpdater;
        [SerializeField] private BridgesUpdater _bridgesUpdater;
        [SerializeField] private MirrorUpdater _mirrorUpdater;
        [SerializeField] private ToolsUpdater _toolsUpdater;
        [SerializeField] private MenuUpdater _menuUpdater;
        
        [SerializeField] private BridgeTabSwapper _bridgeTabSwapper;
        
        public override void InstallBindings()
        {
            Container.Bind<LayoutManager>().FromInstance(_layoutManager);
            Container.Bind<CameraCoordinator>().FromInstance(_cameraCoordinator).AsSingle();
            Container.Bind<OutlineCoordinator>().AsSingle();

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
            Container.BindInterfacesAndSelfTo<InputSettings>().AsSingle().NonLazy();

            Container.Bind<GameManager>().FromComponentInHierarchy().AsSingle();
            Container.Bind<MapProjectorManager>().FromComponentInNewPrefab(_mapProjectorManager).AsSingle();

            Container.Bind<StartupMapLoader>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
            
            // Factories.
            Container.Bind<TileFactory>().AsSingle();
            Container.Bind<BridgeFactory>().AsSingle();
            
            //Updaters.
            BindUpdater(_groundUpdater);
            BindUpdater(_caveUpdater);
            BindUpdater(_heightUpdater);
            BindUpdater(_floorsUpdater);
            BindUpdater(_wallUpdater);
            BindUpdater(_roofUpdater);
            BindUpdater(_decorationUpdater);
            BindUpdater(_labelUpdater);
            BindUpdater(_borderUpdater);
            BindUpdater(_bridgesUpdater);
            BindUpdater(_mirrorUpdater);
            BindUpdater(_toolsUpdater);
            BindUpdater(_menuUpdater);

            Container.Bind<BridgeTabSwapper>().FromInstance(_bridgeTabSwapper);
        }

        private void BindUpdater<T>(T updater) where T : AbstractUpdater
        {
            Container.BindInterfacesAndSelfTo<T>().FromInstance(updater).AsSingle();
            Container.Bind<AbstractUpdater>().FromInstance(updater).AsCached();
        }
    }
}