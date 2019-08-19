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

        private MaterialPropertyBlock greenGhostPropertyBlock;
        private MaterialPropertyBlock redGhostPropertyBlock;

        private void Awake()
        {
            
        }
        
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

            DecorationData data = (DecorationData) GuiManager.Instance.ObjectsTree.SelectedValue;
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
                Vector3 position = raycast.point;
                if (data.CenterOnly)
                {
                    position.x = Mathf.Floor(position.x / 4f) * 4f + 2f;
                    position.z = Mathf.Floor(position.z / 4f) * 4f + 2f;
                }
                else if (data.CornerOnly)
                {
                    position.x = Mathf.Round(position.x / 4f) * 4f;
                    position.z = Mathf.Round(position.z / 4f) * 4f;
                }

                if (data.Floating)
                {
                    position.y = Mathf.Max(position.y, 0);
                }
                
                ghostObject.transform.position = position;
            }
            else
            {
                ghostObject.gameObject.SetActive(false);
            }
        }

    }
}
