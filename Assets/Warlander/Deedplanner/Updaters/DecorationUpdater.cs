using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Decorations;
using Warlander.Deedplanner.Data.Floors;
using Warlander.Deedplanner.Data.Grounds;
using Warlander.Deedplanner.Graphics;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Updaters
{
    public class DecorationUpdater : AbstractUpdater
    {
        [SerializeField] private Toggle snapToGridToggle = null;

        [SerializeField] private Color allowedGhostColor = Color.green;
        [SerializeField] private Color disabledGhostColor = Color.red;
        [SerializeField] private float minimumPlacementGap = 0.25f;
        [SerializeField] private float cornerSnapDistance = 0.25f;

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
            allowedGhostPropertyBlock.SetColor(ShaderPropertyIds.Color, allowedGhostColor);
            disabledGhostPropertyBlock = new MaterialPropertyBlock();
            disabledGhostPropertyBlock.SetColor(ShaderPropertyIds.Color, disabledGhostColor);
        }

        private void Start()
        {
            snapToGridToggle.isOn = Properties.Instance.DecorationSnapToGrid;
        }

        private void OnEnable()
        {
            LayoutManager.Instance.TileSelectionMode = TileSelectionMode.Nothing;
        }

        private void Update()
        {
            bool snapToGrid = snapToGridToggle.isOn;

            bool propertiesNeedSaving = false;

            if (snapToGrid != Properties.Instance.DecorationSnapToGrid)
            {
                Properties.Instance.DecorationSnapToGrid = snapToGrid;
                propertiesNeedSaving = true;
            }

            if (propertiesNeedSaving)
            {
                Properties.Instance.SaveProperties();
            }

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
            if (data == null)
            {
                return;
            }

            OverlayMesh overlayMesh = raycast.transform.GetComponent<OverlayMesh>();
            GroundMesh groundMesh = raycast.transform.GetComponent<GroundMesh>();
            TileEntity tileEntity = raycast.transform.GetComponent<TileEntity>();

            Material ghostMaterial = GraphicsManager.Instance.GhostMaterial;
            if (dataChanged)
            {
                data.Model.CreateOrGetModel(ghostMaterial, OnGhostCreated);
                return;
            }

            if (!ghostObject)
            {
                return;
            }

            int targetFloor = LayoutManager.Instance.CurrentCamera.Floor;
            if (tileEntity && tileEntity.Valid && tileEntity.GetType() == typeof(Floor))
            {
                targetFloor = tileEntity.Floor;
            }

            if (data.CenterOnly || data.Tree || data.Bush)
            {
                targetFloor = 0;
            }

            Map map = GameManager.Instance.Map;

            if (!placingDecoration)
            {
                position = CalculateCorrectedPosition(raycast.point, data, snapToGrid);
                targetedTile = null;
                if (overlayMesh)
                {
                    int tileX = Mathf.FloorToInt(position.x / 4f);
                    int tileY = Mathf.FloorToInt(position.z / 4f);
                    targetedTile = map[tileX, tileY];
                    position.y = map.GetInterpolatedHeight(position.x, position.z);
                    if (data.Floating)
                    {
                        position.y = Mathf.Max(position.y, 0);
                    }
                    else
                    {
                        float floorHeight = 3f;
                        position.y += targetFloor * floorHeight;
                    }
                }
                else if (tileEntity && tileEntity.Valid)
                {
                    targetedTile = tileEntity.Tile;
                }
            }

            bool canPlaceNewObject = overlayMesh || groundMesh || (tileEntity && tileEntity.Valid && tileEntity.GetType() == typeof(Floor));
            if (canPlaceNewObject || placingDecoration)
            {
                ghostObject.gameObject.SetActive(true);
                ghostObject.transform.position = position;
            }
            else
            {
                ghostObject.gameObject.SetActive(false);
            }

            bool placementOverlap = true;
            Vector2 position2d = new Vector2(position.x, position.z);
            IEnumerable<Decoration> nearbyDecorations = GetAllNearbyDecorations(targetedTile);

            foreach (Decoration decoration in nearbyDecorations)
            {
                Vector3 decorationPosition3d = decoration.transform.position;
                Vector2 decorationPosition2d = new Vector2(decorationPosition3d.x, decorationPosition3d.z);
                float distance = Vector2.Distance(position2d, decorationPosition2d);
                if (distance < minimumPlacementGap)
                {
                    placementOverlap = false;
                    break;
                }
            }

            ToggleGhostPropertyBlock(placementOverlap ? allowedGhostPropertyBlock : disabledGhostPropertyBlock);

            if (Input.GetMouseButtonDown(0))
            {
                placingDecoration = true;
                dragStartPos = LayoutManager.Instance.CurrentCamera.MousePosition;
            }

            if (Input.GetAxis("Mouse ScrollWheel") != 0f)
            {
                if (Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftControl))
                {
                    rotation += Input.GetAxis("Mouse ScrollWheel");
                }
                else if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftControl))
                {
                    if (Input.GetAxis("Mouse ScrollWheel") > 0f) {
                        rotation += 11.25f;
                    }
                    else {
                        rotation -= 11.25f;
                    }
                    rotation = Mathf.Round(rotation / 11.25f) * 11.25f;
                }

                ghostObject.transform.localRotation = Quaternion.Euler(0, rotation, 0);
            }

            if (Input.GetMouseButtonUp(0) && placingDecoration)
            {
                float decorationPositionX = position.x - targetedTile.X * 4f;
                float decorationPositionY = position.z - targetedTile.Y * 4f;
                Vector2 decorationPosition = new Vector2(decorationPositionX, decorationPositionY);
                targetedTile.SetDecoration(data, decorationPosition, rotation * Mathf.Deg2Rad, targetFloor, data.Floating);
                map.CommandManager.FinishAction();

                placingDecoration = false;
                ghostObject.transform.localRotation = Quaternion.identity;
                rotation = 0f;
            }

            if (Input.GetMouseButtonDown(1))
            {
                placingDecoration = false;
                ghostObject.transform.localRotation = Quaternion.identity;
            }

            if (Input.GetMouseButtonDown(1) && !placingDecoration)
            {
                IEnumerable<Decoration> decorationsOnTile = targetedTile.GetDecorations();
                foreach (Decoration decoration in decorationsOnTile)
                {
                    targetedTile.SetDecoration(null, decoration.Position, decoration.Rotation, targetFloor);
                }
                map.CommandManager.FinishAction();
            }

            if (Input.GetKeyDown("delete") && !placingDecoration)
            {
                foreach (Decoration decoration in nearbyDecorations)
                {
                    Vector3 decorationPosition3d = decoration.transform.position;
                    Vector2 decorationPosition2d = new Vector2(decorationPosition3d.x, decorationPosition3d.z);
                    float distance = Vector2.Distance(position2d, decorationPosition2d);
                    if (distance < minimumPlacementGap)
                    {
                        decoration.Tile.SetDecoration(null, decoration.Position, decoration.Rotation, targetFloor);
                        break;
                    }
                }
                map.CommandManager.FinishAction();
            }
        }

        private void OnGhostCreated(GameObject ghost)
        {
            if (ghostObject)
            {
                Destroy(ghostObject);
            }

            rotation = 0f;
            ghostObject = ghost;
            ghostObject.transform.SetParent(transform);
        }

        private Vector3 CalculateCorrectedPosition(Vector3 originalPosition, DecorationData data, bool snapToGrid)
        {
            Vector3 pos = originalPosition;
            if (data.CenterOnly)
            {
                pos.x = Mathf.Floor(originalPosition.x / 4f) * 4f + 2f;
                pos.z = Mathf.Floor(originalPosition.z / 4f) * 4f + 2f;
            }
            else if (data.CornerOnly)
            {
                pos.x = Mathf.Round(originalPosition.x / 4f) * 4f;
                pos.z = Mathf.Round(originalPosition.z / 4f) * 4f;
            }
            else if (snapToGrid)
            {
                float distToCornerX = 2f - Mathf.Abs(originalPosition.x % 4f - 2f);
                float distToCornerZ = 2f - Mathf.Abs(originalPosition.z % 4f - 2f);
                Vector2 distVector = new Vector2(distToCornerX, distToCornerZ);
                float magnitude = distVector.magnitude;
                if (magnitude < cornerSnapDistance)
                {
                    pos.x = Mathf.Round(originalPosition.x / 4f) * 4f;
                    pos.z = Mathf.Round(originalPosition.z / 4f) * 4f;
                }
                else
                {
                    pos.x = Mathf.Floor(originalPosition.x / (4f / 3f)) * (4f / 3f) + (2f / 3f);
                    pos.z = Mathf.Floor(originalPosition.z / (4f / 3f)) * (4f / 3f) + (2f / 3f);
                }
            }

            if (data.Floating)
            {
                pos.y = Mathf.Max(originalPosition.y, 0);
            }

            return pos;
        }

        private IEnumerable<Decoration> GetAllNearbyDecorations(Tile centralTile)
        {
            List<Decoration> decorations = new List<Decoration>();

            if (centralTile == null)
            {
                return decorations;
            }

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Tile relativeTile = centralTile.Map.GetRelativeTile(centralTile, x, y);
                    if (relativeTile != null)
                    {
                        decorations.AddRange(relativeTile.GetDecorations());
                    }
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

        private void OnDisable()
        {
            ResetState();
        }

        private void ResetState()
        {
            placingDecoration = false;
            dragStartPos = new Vector2();

            GameManager.Instance.Map.CommandManager.UndoAction();
        }
    }
}
