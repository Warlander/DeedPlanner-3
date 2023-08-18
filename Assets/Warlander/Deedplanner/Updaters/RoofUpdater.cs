using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Roofs;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Gui.Tooltips;
using Warlander.Deedplanner.Gui.Widgets;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Logic.Cameras;
using Zenject;

namespace Warlander.Deedplanner.Updaters
{
    public class RoofUpdater : AbstractUpdater
    {
        [Inject] private TooltipHandler _tooltipHandler;
        [Inject] private CameraCoordinator _cameraCoordinator;
        [Inject] private DPInput _input;
        [Inject] private GameManager _gameManager;

        [SerializeField] private UnityList _roofsList;

        private void Start()
        {
            foreach (RoofData data in Database.Roofs.Values)
            {
                _roofsList.Add(data);
            }
        }
        
        private void OnEnable()
        {
            LayoutManager.Instance.TileSelectionMode = TileSelectionMode.Tiles;
        }

        private void Update()
        {
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

            if (floor == 0 || floor == -1)
            {
                _tooltipHandler.ShowTooltipText("<color=red><b>It's not possible to place roofs on ground floor</b></color>");
                return;
            }

            if (_input.UpdatersShared.Placement.ReadValue<float>() > 0)
            {
                RoofData data = _roofsList.SelectedValue as RoofData;
                _gameManager.Map[x, y].SetRoof(data, floor);
            }
            else if (_input.UpdatersShared.Deletion.ReadValue<float>() > 0)
            {
                _gameManager.Map[x, y].SetRoof(null, floor);
            }
            
            if (_input.UpdatersShared.Placement.WasReleasedThisFrame() || _input.UpdatersShared.Deletion.WasReleasedThisFrame())
            {
                _gameManager.Map.CommandManager.FinishAction();
            }
        }
    }
}
