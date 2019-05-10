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
    public class RoofUpdater : MonoBehaviour
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

            
        }

    }
}
