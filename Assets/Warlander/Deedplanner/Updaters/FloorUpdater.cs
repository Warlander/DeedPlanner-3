using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Floors;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Gui.Tooltips;
using Warlander.Deedplanner.Logic;
using Zenject;

namespace Warlander.Deedplanner.Updaters
{
    public class FloorUpdater : AbstractUpdater
    {
        [Inject] private TooltipHandler _tooltipHandler;
        
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
            if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
            {
                GameManager.Instance.Map.CommandManager.FinishAction();
            }
            
            RaycastHit raycast = LayoutManager.Instance.CurrentCamera.CurrentRaycast;
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
                floor = LayoutManager.Instance.CurrentCamera.Floor;
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

            if (Input.GetMouseButton(0))
            {
                GameManager.Instance.Map[x, y].SetFloor(data, orientation, floor);
            }
            else if (Input.GetMouseButton(1))
            {
                GameManager.Instance.Map[x, y].SetFloor(null, orientation, floor);
            }
        }
    }
}
