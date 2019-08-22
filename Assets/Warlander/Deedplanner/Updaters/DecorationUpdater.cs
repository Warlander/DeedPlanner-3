using System.Collections.Generic;
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

        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

        [SerializeField] private Color allowedGhostColor = Color.green;
        [SerializeField] private Color disabledGhostColor = Color.red;
        [SerializeField] private float minimumPlacementGap = 0.25f;
        [SerializeField] private float rotationEditSensitivity = 1f;
        
        private DecorationData lastFrameData;
        private GameObject ghostObject;

        private MaterialPropertyBlock allowedGhostPropertyBlock;
        private MaterialPropertyBlock disabledGhostPropertyBlock;

        private Vector3 position;
        private float rotation;
        private bool placingDecoration = false;
        private Tile targetedTile;
        private Vector2 dragStartPos;

        private void Awake()
        {
            allowedGhostPropertyBlock = new MaterialPropertyBlock();
            allowedGhostPropertyBlock.SetColor(ColorPropertyId, allowedGhostColor);
            disabledGhostPropertyBlock = new MaterialPropertyBlock();
            disabledGhostPropertyBlock.SetColor(ColorPropertyId, disabledGhostColor);
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

            Map map = GameManager.Instance.Map;
            
            if (!placingDecoration)
            {
                position = CalculateCorrectedPosition(raycast.point, data);
                targetedTile = null;
                if (gridTile)
                {
                    targetedTile = map[gridTile.X, gridTile.Y];
                }
                else if (tileEntity && tileEntity.Valid)
                {
                    targetedTile = tileEntity.Tile;
                }
            }

            if (gridTile || (tileEntity && tileEntity.Valid && (tileEntity.Type == EntityType.Ground || tileEntity.GetType() == typeof(Floor))))
            {
                ghostObject.gameObject.SetActive(true);
                ghostObject.transform.position = position;
            }
            else
            {
                ghostObject.gameObject.SetActive(false);
            }
            
            bool placementAllowed;
            if (data.CenterOnly || data.Tree || data.Bush)
            {
                int entityFloor = 0;
                if (gridTile)
                {
                    entityFloor = LayoutManager.Instance.CurrentCamera.Floor;
                }
                else if (tileEntity && tileEntity.Valid)
                {
                    entityFloor = tileEntity.Floor;
                }
                
                placementAllowed = entityFloor == 0;
            }
            else
            {
                Vector2 position2d = new Vector2(position.x, position.z);
                List<Decoration> nearbyDecorations = GetAllNearbyDecorations(targetedTile);

                placementAllowed = true;
                foreach (Decoration decoration in nearbyDecorations)
                {
                    Vector3 decorationPosition3d = decoration.transform.position;
                    Vector2 decorationPosition2d = new Vector2(decorationPosition3d.x, decorationPosition3d.z);
                    float distance = Vector2.Distance(position2d, decorationPosition2d);
                    if (distance < minimumPlacementGap)
                    {
                        placementAllowed = false;
                        break;
                    }
                }
            }
            
            if (placementAllowed)
            {
                ToggleGhostPropertyBlock(allowedGhostPropertyBlock);
            }
            else
            {
                ToggleGhostPropertyBlock(disabledGhostPropertyBlock);
            }

            if (Input.GetMouseButtonDown(0))
            {
                placingDecoration = true;
                dragStartPos = LayoutManager.Instance.CurrentCamera.MousePosition;
            }

            if (Input.GetMouseButton(0))
            {
                Vector2 dragEndPos = LayoutManager.Instance.CurrentCamera.MousePosition;
                Vector2 difference = dragEndPos - dragStartPos;
                rotation = -difference.x * rotationEditSensitivity;
                ghostObject.transform.localRotation = Quaternion.Euler(0, rotation, 0);
            }

            if (Input.GetMouseButtonUp(0) && placingDecoration)
            {
                float decorationPositionX = position.x - targetedTile.X * 4f;
                float decorationPositionY = position.z - targetedTile.Y * 4f;
                Vector2 decorationPosition = new Vector2(decorationPositionX, decorationPositionY);
                targetedTile.SetDecoration(data, decorationPosition, rotation * Mathf.Deg2Rad, LayoutManager.Instance.CurrentCamera.Floor);
                map.CommandManager.FinishAction();
                
                placingDecoration = false;
                ghostObject.transform.localRotation = Quaternion.identity;
            }

            if (Input.GetMouseButtonDown(1))
            {
                placingDecoration = false;
                ghostObject.transform.localRotation = Quaternion.identity;
            }

            if (Input.GetMouseButtonDown(1) && !placingDecoration)
            {
                List<Decoration> decorationsOnTile = targetedTile.GetDecorations();
                foreach (Decoration decoration in decorationsOnTile)
                {
                    targetedTile.SetDecoration(null, decoration.Position, decoration.Rotation, LayoutManager.Instance.CurrentCamera.Floor);
                }
                map.CommandManager.FinishAction();
            }
        }

        private Vector3 CalculateCorrectedPosition(Vector3 originalPosition, DecorationData data)
        {
            Vector3 position = originalPosition;
            if (data.CenterOnly)
            {
                position.x = Mathf.Floor(originalPosition.x / 4f) * 4f + 2f;
                position.z = Mathf.Floor(originalPosition.z / 4f) * 4f + 2f;
            }
            else if (data.CornerOnly)
            {
                position.x = Mathf.Round(originalPosition.x / 4f) * 4f;
                position.z = Mathf.Round(originalPosition.z / 4f) * 4f;
            }

            if (data.Floating)
            {
                position.y = Mathf.Max(originalPosition.y, 0);
            }

            return position;
        }
        
        private List<Decoration> GetAllNearbyDecorations(Tile centralTile)
        {
            List<Decoration> decorations = new List<Decoration>();
            
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    decorations.AddRange(centralTile.Map.GetRelativeTile(centralTile, x, y).GetDecorations());
                }
            }

            return decorations;
        }
        
        private void ToggleGhostPropertyBlock(MaterialPropertyBlock propertyBlock)
        {
            if (!ghostObject)
            {
                return;
            }
            
            foreach (Renderer render in ghostObject.GetComponentsInChildren<Renderer>())
            {
                render.SetPropertyBlock(propertyBlock);
            }
        }

    }
}
