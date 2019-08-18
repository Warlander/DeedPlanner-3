using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Decorations;
using Warlander.Deedplanner.Data.Floors;
using Warlander.Deedplanner.Graphics;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Updaters
{
    public class DecorationUpdater : MonoBehaviour
    {

        private DecorationData lastFrameData;
        private GameObject ghostObject;
        
        private void OnEnable()
        {
            LayoutManager.Instance.TileSelectionMode = TileSelectionMode.Nothing;
        }

        private void Update()
        {
            RaycastHit raycast = LayoutManager.Instance.CurrentCamera.CurrentRaycast;
            if (!raycast.transform)
            {
                if (ghostObject)
                {
                    ghostObject.SetActive(false);
                }
                return;
            }

            DecorationData data = GuiManager.Instance.ObjectsTree.SelectedValue as DecorationData;
            bool dataChanged = data != lastFrameData;
            lastFrameData = data;
            if (!data)
            {
                return;
            }
            
            GridTile gridTile = raycast.transform.GetComponent<GridTile>();
            TileEntity tileEntity = raycast.transform.GetComponent<TileEntity>();
            
            Material ghostMaterial = GraphicsManager.Instance.GhostMaterial;
            if (dataChanged)
            {
                if (ghostObject)
                {
                    Destroy(ghostObject);
                }
                ghostObject = data.Model.CreateOrGetModel(ghostMaterial);
                ghostObject.transform.SetParent(transform);
            }

            if (gridTile || (tileEntity.Valid && (tileEntity.Type == EntityType.Ground || tileEntity.GetType() == typeof(Floor))))
            {
                ghostObject.gameObject.SetActive(true);
                ghostObject.transform.position = raycast.point;
            }
            else
            {
                ghostObject.gameObject.SetActive(false);
            }
        }

    }
}
