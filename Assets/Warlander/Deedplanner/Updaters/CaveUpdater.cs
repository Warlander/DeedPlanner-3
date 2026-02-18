using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Caves;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Gui.Widgets;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Logic.Cameras;
using Zenject;

namespace Warlander.Deedplanner.Updaters
{
    public class CaveUpdater : AbstractUpdater
    {
        [Inject] private CameraCoordinator _cameraCoordinator;
        [Inject] private DPInput _input;
        
        [SerializeField] private UnityTree _cavesTree;

        public override void Initialize()
        {
            foreach (CaveData data in Database.Caves.Values)
            {
                foreach (string[] category in data.Categories)
                {
                    _cavesTree.Add(data, category);
                }
            }
        }

        public override void Enable()
        {
            LayoutManager.Instance.TileSelectionMode = TileSelectionMode.Tiles;
        }

        public override void Disable() { }

        public override void Tick()
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
