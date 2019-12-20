using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Floors;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Updaters
{
    public class FloorUpdater : AbstractUpdater
    {

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
                LayoutManager.Instance.TooltipText += "\n<color=red><b>It's not possible to place openings/stairs on ground floor</b></color>";
                return;
            }

            FloorOrientation orientation = FloorOrientation.Down;
            if (southToggle.isOn)
            {
                orientation = FloorOrientation.Down;
            }
            else if (westToggle.isOn)
            {
                orientation = FloorOrientation.Right;
            }
            else if (northToggle.isOn)
            {
                orientation = FloorOrientation.Up;
            }
            else if (eastToggle.isOn)
            {
                orientation = FloorOrientation.Left;
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
