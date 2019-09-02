using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Updaters
{
    public class BorderUpdater : MonoBehaviour
    {

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

            GridMesh gridMesh = raycast.transform.GetComponent<GridMesh>();
            TileEntity tileEntity = raycast.transform.GetComponent<TileEntity>();

        }

    }
}
