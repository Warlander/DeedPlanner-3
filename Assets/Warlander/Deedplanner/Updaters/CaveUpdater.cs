using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Caves;
using Warlander.Deedplanner.Gui.Updaters;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Logic.Cameras;

namespace Warlander.Deedplanner.Updaters
{
    public class CaveUpdater : IUpdater
    {
        private readonly ICaveUpdaterView _view;
        private readonly CameraCoordinator _cameraCoordinator;
        private readonly TabContext _tabContext;

        public Tab TargetTab => Tab.Caves;

        public CaveUpdater(ICaveUpdaterView view, CameraCoordinator cameraCoordinator, TabContext tabContext)
        {
            _view = view;
            _cameraCoordinator = cameraCoordinator;
            _tabContext = tabContext;
        }

        public void Initialize()
        {
            foreach (CaveData data in Database.Caves.Values)
            {
                foreach (string[] category in data.Categories)
                {
                    _view.AddCaveEntry(data, category);
                }
            }
        }

        public void Enable()
        {
            _tabContext.TileSelectionMode = TileSelectionMode.Tiles;
        }

        public void Disable() { }

        public void Tick()
        {
            RaycastHit raycast = _cameraCoordinator.Current.CurrentRaycast;
            if (!raycast.transform)
            {
                return;
            }

            OverlayMesh overlayMesh = raycast.transform.GetComponent<OverlayMesh>();
            LevelEntity levelEntity = raycast.transform.GetComponent<LevelEntity>();

        }
    }
}
