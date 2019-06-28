using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Floors;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Updaters
{
    public class FloorUpdater : MonoBehaviour
    {

        [SerializeField]
        private Toggle southToggle = null;
        [SerializeField]
        private Toggle westToggle = null;
        [SerializeField]
        private Toggle northToggle = null;
        [SerializeField]
        private Toggle eastToggle = null;

        private void OnEnable()
        {
            LayoutManager.Instance.TileSelectionMode = TileSelectionMode.Tiles;
        }

        private void Update()
        {
            RaycastHit raycast = LayoutManager.Instance.CurrentCamera.CurrentRaycast;
            if (!raycast.transform)
            {
                return;
            }

            GridTile gridTile = raycast.transform.GetComponent<GridTile>();
            TileEntity tileEntity = raycast.transform.GetComponent<TileEntity>();

            int floor = 0;
            int x = -1;
            int y = -1;
            if (tileEntity)
            {
                floor = tileEntity.Floor;
                x = tileEntity.Tile.X;
                y = tileEntity.Tile.Y;
            }
            else if (gridTile)
            {
                floor = LayoutManager.Instance.CurrentCamera.Floor;
                x = gridTile.X;
                y = gridTile.Y;
            }

            FloorData data = GuiManager.Instance.FloorsTree.SelectedValue as FloorData;
            if (data.Opening && (floor == 0 || floor == -1))
            {
                return;
            }

            FloorOrientation orientation;
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
            else
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

            if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
            {
                GameManager.Instance.Map.CommandManager.FinishAction();
            }
        }

    }
}
