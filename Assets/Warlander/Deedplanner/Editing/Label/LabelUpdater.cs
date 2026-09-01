using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Cameras;

namespace Warlander.Deedplanner.Editing
{
    public class LabelUpdater : IUpdater
    {
        private readonly CameraCoordinator _cameraCoordinator;
        private readonly TabContext _tabContext;

        public Tab TargetTab => Tab.Labels;

        public LabelUpdater(CameraCoordinator cameraCoordinator, TabContext tabContext)
        {
            _cameraCoordinator = cameraCoordinator;
            _tabContext = tabContext;
        }

        public void Initialize() { }

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
