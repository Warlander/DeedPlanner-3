using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Gui;

namespace Warlander.Deedplanner.Logic
{
    public class WallUpdater : MonoBehaviour
    {

        [SerializeField]
        private Toggle reverseToggle;
        [SerializeField]
        private Toggle automaticReverseToggle;

        private void OnEnable()
        {
            LayoutManager.Instance.TileSelectionMode = TileSelectionMode.Borders;
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
            Wall wallEntity = tileEntity as Wall;
            Ground groundEntity = tileEntity as Ground;

            bool automaticReverse = automaticReverseToggle.isOn;
            bool reverse = reverseToggle.isOn;
            int floor = 0;
            int x = -1;
            int y = -1;
            bool horizontal = false;
            if (wallEntity)
            {
                floor = tileEntity.Floor;
                if (LayoutManager.Instance.CurrentCamera.Floor == floor + 1)
                {
                    floor++;
                }
                x = tileEntity.Tile.X;
                y = tileEntity.Tile.Y;
                EntityType type = tileEntity.Type;
                horizontal = (type == EntityType.HWALL || type == EntityType.HFENCE);
            }
            else if (gridTile || groundEntity)
            {
                if (gridTile)
                {
                    floor = LayoutManager.Instance.CurrentCamera.Floor;
                }
                else if (groundEntity)
                {
                    floor = 0;
                }
                TileSelectionHit tileSelectionHit = TileSelection.PositionToTileSelectionHit(raycast.point, TileSelectionMode.Borders);
                TileSelectionTarget target = tileSelectionHit.Target;
                if (target == TileSelectionTarget.Nothing)
                {
                    return;
                }
                x = tileSelectionHit.X;
                y = tileSelectionHit.Y;
                horizontal = (target == TileSelectionTarget.BottomBorder);
            }

            if (Input.GetMouseButton(0))
            {
                Floor currentFloor = GameManager.Instance.Map[x, y].GetTileContent(floor) as Floor;
                bool shouldReverse = false;
                if (automaticReverse && horizontal)
                {
                    Floor nearFloor = GameManager.Instance.Map[x, y - 1].GetTileContent(floor) as Floor;
                    shouldReverse = currentFloor && !nearFloor;
                }
                else if (automaticReverse && !horizontal)
                {
                    Floor nearFloor = GameManager.Instance.Map[x - 1, y].GetTileContent(floor) as Floor;
                    shouldReverse = !currentFloor && nearFloor;
                }

                if (reverse)
                {
                    shouldReverse = !shouldReverse;
                }

                WallData data = GuiManager.Instance.WallsTree.SelectedValue as WallData;
                if (horizontal)
                {
                    GameManager.Instance.Map[x, y].SetHorizontalWall(data, shouldReverse, floor);
                }
                else
                {
                    GameManager.Instance.Map[x, y].SetVerticalWall(data, shouldReverse, floor);
                }
            }
            else if (Input.GetMouseButton(1))
            {
                if (floor != LayoutManager.Instance.CurrentCamera.Floor)
                {
                    return;
                }
                if (horizontal)
                {
                    GameManager.Instance.Map[x, y].SetHorizontalWall(null, false, floor);
                }
                else
                {
                    GameManager.Instance.Map[x, y].SetVerticalWall(null, false, floor);
                }
            }
        }

    }
}
