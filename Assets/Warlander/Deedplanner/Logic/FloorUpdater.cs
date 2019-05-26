using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Gui;

namespace Warlander.Deedplanner.Logic
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

            EntityOrientation orientation;
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
            else
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
