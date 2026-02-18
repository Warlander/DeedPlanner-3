using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Floors;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Gui.Tooltips;
using Warlander.Deedplanner.Gui.Widgets;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Logic.Cameras;
using Zenject;

namespace Warlander.Deedplanner.Updaters
{
    public class FloorUpdater : AbstractUpdater
    {
        [Inject] private TooltipHandler _tooltipHandler;
        [Inject] private CameraCoordinator _cameraCoordinator;
        [Inject] private DPInput _input;
        [Inject] private GameManager _gameManager;

        [SerializeField] private UnityTree _floorsTree;

        [SerializeField] private Toggle southToggle;
        [SerializeField] private Toggle westToggle;
        [SerializeField] private Toggle northToggle;
        [SerializeField] private Toggle eastToggle;

        public override void Initialize()
        {
            foreach (FloorData data in Database.Floors.Values)
            {
                foreach (string[] category in data.Categories)
                {
                    _floorsTree.Add(data, category);
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
            if (_input.UpdatersShared.Placement.WasReleasedThisFrame() || _input.UpdatersShared.Deletion.WasReleasedThisFrame())
            {
                _gameManager.Map.CommandManager.FinishAction();
            }
            
            RaycastHit raycast = _cameraCoordinator.Current.CurrentRaycast;
            if (!raycast.transform)
            {
                return;
            }

            OverlayMesh overlayMesh = raycast.transform.GetComponent<OverlayMesh>();
            LevelEntity levelEntity = raycast.transform.GetComponent<LevelEntity>();

            int floor = 0;
            int x = -1;
            int y = -1;
            if (levelEntity && levelEntity.Valid)
            {
                floor = levelEntity.Level;
                x = levelEntity.Tile.X;
                y = levelEntity.Tile.Y;
            }
            else if (overlayMesh)
            {
                floor = _cameraCoordinator.Current.Level;
                x = Mathf.FloorToInt(raycast.point.x / 4f);
                y = Mathf.FloorToInt(raycast.point.z / 4f);
            }

            FloorData data = _floorsTree.SelectedValue as FloorData;
            if (data.Opening && (floor == 0 || floor == -1))
            {
                _tooltipHandler.ShowTooltipText("<color=red><b>It's not possible to place openings/stairs on ground floor</b></color>");
                return;
            }

            EntityOrientation orientation = EntityOrientation.Down;
            if (southToggle.isOn)
            {
                orientation = EntityOrientation.Down;
            }
            else if (westToggle.isOn)
            {
                orientation = EntityOrientation.Right;
            }
            else if (northToggle.isOn)
            {
                orientation = EntityOrientation.Up;
            }
            else if (eastToggle.isOn)
            {
                orientation = EntityOrientation.Left;
            }

            if (_input.UpdatersShared.Placement.ReadValue<float>() > 0)
            {
                _gameManager.Map[x, y].SetFloor(data, orientation, floor);
            }
            else if (_input.UpdatersShared.Deletion.ReadValue<float>() > 0)
            {
                _gameManager.Map[x, y].SetFloor(null, orientation, floor);
            }
        }
    }
}
