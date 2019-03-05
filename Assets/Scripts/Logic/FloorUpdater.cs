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
    public class FloorUpdater : MonoBehaviour
    {

        public void OnEnable()
        {
            LayoutManager.Instance.TileSelectionMode = TileSelectionMode.Tiles;
        }

        public void Update()
        {
            RaycastHit raycast = LayoutManager.Instance.CurrentCamera.CurrentRaycast;
            if (raycast.transform == null)
            {
                return;
            }

            BasicTile tile = raycast.transform.GetComponentInParent<BasicTile>();
            GridTile gridTile = raycast.transform.GetComponent<GridTile>();
            Ground ground = raycast.transform.GetComponent<Ground>();
            Floor floor = raycast.transform.GetComponent<Floor>();

            int level = 0;
            int x = -1;
            int y = -1;
            if (ground)
            {
                level = 0;
                x = tile.X;
                y = tile.Y;
            }
            else if (gridTile)
            {
                level = LayoutManager.Instance.CurrentCamera.Floor;
                x = gridTile.X;
                y = gridTile.Y;
            }

            if (Input.GetMouseButton(0))
            {
                FloorData data = GuiManager.Instance.FloorsTree.SelectedValue as FloorData;
                GameManager.Instance.Map[x, y].GetTileForFloor(level).SetFloor(data, EntityOrientation.Up, level);
            }
            else if (Input.GetMouseButton(1))
            {
                GameManager.Instance.Map[x, y].GetTileForFloor(level).SetFloor(null, EntityOrientation.Up, level);
            }
            
        }

    }
}
