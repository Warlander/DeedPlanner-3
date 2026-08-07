using UnityEngine;
using VContainer;
using VContainer.Unity;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Bridges;
using Warlander.Deedplanner.Debugging;
using Warlander.Deedplanner.Graphics.Projectors;
using Warlander.Deedplanner.Graphics.Water;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Gui.Updaters;
using Warlander.Deedplanner.Gui.Widgets.Bridges;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Logic.Cameras;
using Warlander.Deedplanner.Logic.Outlines;
using Warlander.Deedplanner.Settings;
using Warlander.Deedplanner.Updaters;

namespace Warlander.Deedplanner.Scopes
{
    public class MainSceneLifetimeScope : DPSceneLifetimeScope
    {
        [SerializeField] private DebugProperties _debugProperties;
        [SerializeField] private CameraCoordinator _cameraCoordinator;
        [SerializeField] private BridgeTabSwapper _bridgeTabSwapper;

        protected override void Configure(IContainerBuilder builder)
        {
            // TODO: Refactor this. Injecting dependencies into GO's isn't recommended, injection patterns around GO's should be done VContainer way instead of Zenject way - inject GO's into C# classes only, never other way around.
            builder.RegisterBuildCallback(container =>
            {
                foreach (GameObject root in gameObject.scene.GetRootGameObjects())
                {
                    foreach (MonoBehaviour mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        container.InjectGameObject(mb.gameObject);
                    }
                }
            });
            
            base.Configure(builder);

            builder.RegisterComponentInHierarchy<EditorAreaLayouterView>().As<IEditorAreaLayouterView>();
            builder.RegisterEntryPoint<EditorAreaLayouterPresenter>();

            builder.RegisterComponentInHierarchy<TabTransitionView>().As<ITabTransitionView>();
            builder.RegisterEntryPoint<TabTransitionPresenter>();

            builder.RegisterComponentInHierarchy<TabSelectionView>().As<ITabSelectionView>();
            builder.RegisterEntryPoint<TabSelectionPresenter>();

            builder.RegisterComponentInHierarchy<BridgeCreationView>().As<IBridgeCreationView>();
            builder.RegisterEntryPoint<BridgeCreationPresenter>();

            builder.RegisterComponentInHierarchy<BridgeEditingView>().As<IBridgeEditingView>();
            builder.RegisterEntryPoint<BridgeEditingPresenter>();

            builder.RegisterComponentInHierarchy<BridgeSegmentContainer>().As<IBridgeSegmentBarView>();
            builder.RegisterEntryPoint<BridgeSegmentBarPresenter>();

            builder.RegisterComponentInHierarchy<GroundUpdaterView>().As<IGroundUpdaterView>();
            builder.RegisterComponentInHierarchy<CaveUpdaterView>().As<ICaveUpdaterView>();
            builder.RegisterComponentInHierarchy<RoofUpdaterView>().As<IRoofUpdaterView>();
            builder.RegisterComponentInHierarchy<FloorUpdaterView>().As<IFloorUpdaterView>();
            builder.RegisterComponentInHierarchy<WallUpdaterView>().As<IWallUpdaterView>();
            builder.RegisterComponentInHierarchy<MenuUpdaterView>().As<IMenuUpdaterView>();
            builder.RegisterComponentInHierarchy<DecorationUpdaterView>().As<IDecorationUpdaterView>();
            builder.RegisterComponentInHierarchy<ToolsUpdaterView>().As<IToolsUpdaterView>();
            builder.RegisterComponentInHierarchy<HeightUpdaterView>().As<IHeightUpdaterView>();

            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                foreach (WindowOpenerButtonView view in root.GetComponentsInChildren<WindowOpenerButtonView>(true))
                    builder.RegisterInstance(view).As<IWindowOpenerButtonView>();
            }
            builder.RegisterEntryPoint<WindowOpenerPresenter>();

            builder.Register<LayoutContext>(Lifetime.Singleton);
            builder.RegisterEntryPoint<TabContext>().AsSelf();
            builder.RegisterInstance(_cameraCoordinator);
            builder.Register<OutlineCoordinator>(Lifetime.Singleton).As<IOutlineCoordinator>();
            builder.RegisterEntryPoint<OutlineFeatureBridge>();

            builder.Register<FppCameraController>(Lifetime.Transient).As<ICameraController>();
            builder.Register<IsoCameraController>(Lifetime.Transient).As<ICameraController>();
            builder.Register<TopCameraController>(Lifetime.Transient).As<ICameraController>();

            builder.RegisterEntryPoint<WaterFacade>();

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            builder.RegisterEntryPoint<FileDragManager>();
#endif

            if (Debug.isDebugBuild)
            {
                builder.RegisterEntryPoint<DebugApplier>();
                builder.RegisterInstance(_debugProperties);
            }

            DPInput input = new DPInput();
            input.Enable();
            builder.RegisterInstance(input);

            builder.RegisterEntryPoint<InputSettings>().AsSelf();
            builder.Register<MapRenderSettings>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();

            builder.Register<MapHandler>(Lifetime.Singleton);
            builder.RegisterEntryPoint<UndoRedoInputHandler>();
            builder.RegisterEntryPoint<UpdaterCoordinator>();
            builder.Register<MapProjectorFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.RegisterEntryPoint<StartupMapLoader>();

            builder.Register<OverlayMeshLoader>(Lifetime.Singleton);
            builder.Register<HeightmapHandleMeshLoader>(Lifetime.Singleton);

            builder.Register<TileFactory>(Lifetime.Singleton);
            builder.Register<BridgeFactory>(Lifetime.Singleton);

            builder.Register<MapHeightTracker>(Lifetime.Singleton);
            builder.RegisterEntryPoint<MapRoofCalculator>().AsSelf();

            builder.Register<BorderUpdater>(Lifetime.Singleton).As<IUpdater>();
            builder.Register<LabelUpdater>(Lifetime.Singleton).As<IUpdater>();
            builder.Register<MirrorUpdater>(Lifetime.Singleton).As<IUpdater>();
            builder.Register<BridgesUpdater>(Lifetime.Singleton).As<IUpdater>().AsSelf();
            builder.Register<GroundUpdater>(Lifetime.Singleton).As<IUpdater>();
            builder.Register<CaveUpdater>(Lifetime.Singleton).As<IUpdater>();
            builder.Register<RoofUpdater>(Lifetime.Singleton).As<IUpdater>();
            builder.Register<FloorUpdater>(Lifetime.Singleton).As<IUpdater>();
            builder.Register<WallUpdater>(Lifetime.Singleton).As<IUpdater>();
            builder.Register<MenuUpdater>(Lifetime.Singleton).As<IUpdater>();
            builder.Register<DecorationUpdater>(Lifetime.Singleton).As<IUpdater>();
            builder.Register<ToolsUpdater>(Lifetime.Singleton).As<IUpdater>();
            builder.Register<HeightUpdater>(Lifetime.Singleton).As<IUpdater>();

            builder.RegisterInstance(_bridgeTabSwapper);
        }
    }
}
