using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Floors;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Gui.Tooltips;
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
        
        [SerializeField] private Toggle southToggle = null;
        [SerializeField] private Toggle westToggle = null;
        [SerializeField] private Toggle northToggle = null;
        [SerializeField] private Toggle eastToggle = null;

        private void OnEnable()
        {
            LayoutManager.Instance.TileSelectionMode = TileSelectionMode.Tiles;
        }

        private void Update()
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
            TileEntity tileEntity = raycast.transform.GetComponent<TileEntity>();

            int floor = 0;
            int x = -1;
            int y = -1;
            if (tileEntity && tileEntity.Valid)
            {
                floor = tileEntity.Floor;
                x = tileEntity.Tile.X;
                y = tileEntity.Tile.Y;
            }
            else if (overlayMesh)
            {
                floor = _cameraCoordinator.Current.Floor;
                x = Mathf.FloorToInt(raycast.point.x / 4f);
                y = Mathf.FloorToInt(raycast.point.z / 4f);
            }

            FloorData data = GuiManager.Instance.FloorsTree.SelectedValue as FloorData;
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
