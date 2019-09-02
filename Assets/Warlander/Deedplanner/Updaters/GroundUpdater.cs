using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Grounds;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Updaters
{
    public class GroundUpdater : MonoBehaviour
    {
        
        [SerializeField] private Image leftClickImage = null;
        [SerializeField] private TextMeshProUGUI leftClickText = null;
        [SerializeField] private Image rightClickImage = null;
        [SerializeField] private TextMeshProUGUI rightClickText = null;

        [SerializeField] private Toggle leftClickToggle = null;

        [SerializeField] private Toggle pencilToggle = null;
        [SerializeField] private Toggle fillToggle = null;
        
        private GroundData leftClickData;
        private GroundData rightClickData;

        private bool editCorners = true;

        private GroundData LeftClickData {
            get => leftClickData;
            set {
                leftClickData = value;
                leftClickImage.sprite = leftClickData.Tex2d.Sprite;
                leftClickText.text = leftClickData.Name;
            }
        }

        private GroundData RightClickData {
            get => rightClickData;
            set {
                rightClickData = value;
                rightClickImage.sprite = rightClickData.Tex2d.Sprite;
                rightClickText.text = rightClickData.Name;
            }
        }

        public bool EditCorners {
            get => editCorners;
            set {
                editCorners = value;
                UpdateSelectionMode();
            }
        }

        private void Start()
        {
            GuiManager.Instance.GroundsTree.ValueChanged += OnGroundsTreeValueChanged;
            LeftClickData = Database.Grounds["gr"];
            RightClickData = Database.Grounds["di"];
        }

        private void OnEnable()
        {
            UpdateSelectionMode();
        }

        private void UpdateSelectionMode()
        {
            if (editCorners)
            {
                LayoutManager.Instance.TileSelectionMode = TileSelectionMode.Everything;
            }
            else
            {
                LayoutManager.Instance.TileSelectionMode = TileSelectionMode.Tiles;
            }
        }

        private void OnGroundsTreeValueChanged(object sender, object value)
        {
            bool leftClick = leftClickToggle.isOn;
            GroundData groundData = value as GroundData;
            if (leftClick)
            {
                LeftClickData = groundData;
            }
            else
            {
                RightClickData = groundData;
            }
        }

        private void Update()
        {
            if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
            {
                GameManager.Instance.Map.CommandManager.FinishAction();
            }
            
            RaycastHit raycast = LayoutManager.Instance.CurrentCamera.CurrentRaycast;
            if (!raycast.transform)
            {
                return;
            }

            Map map = GameManager.Instance.Map;
            GroundMesh groundMesh = raycast.transform.GetComponent<GroundMesh>();
            int tileX = Mathf.FloorToInt(raycast.point.x / 4f);
            int tileZ = Mathf.FloorToInt(raycast.point.z / 4f);
            Tile tile = map[tileX, tileZ];
            Ground ground = tile.Ground;

            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                if (Input.GetMouseButtonDown(0))
                {
                    LeftClickData = ground.Data;
                }
                else if (Input.GetMouseButtonDown(1))
                {
                    RightClickData = ground.Data;
                }
            }
            
            GroundData currentClickData = GetCurrentClickData();
            if (!currentClickData)
            {
                return;
            }

            if (pencilToggle.isOn)
            {
                if (editCorners && leftClickData.Diagonal)
                {
                    TileSelectionHit hit = TileSelection.PositionToTileSelectionHit(raycast.point, TileSelectionMode.TilesAndCorners);
                    if (hit.Target == TileSelectionTarget.InnerTile || hit.Target == TileSelectionTarget.Nothing)
                    {
                        ground.RoadDirection = RoadDirection.Center;
                    }
                    else if (hit.X - tile.X == 0 && hit.Y - tile.Y == 0)
                    {
                        ground.RoadDirection = RoadDirection.SW;
                    }
                    else if (hit.X - tile.X == 1 && hit.Y - tile.Y == 0)
                    {
                        ground.RoadDirection = RoadDirection.SE;
                    }
                    else if (hit.X - tile.X == 0 && hit.Y - tile.Y == 1)
                    {
                        ground.RoadDirection = RoadDirection.NW;
                    }
                    else if (hit.X - tile.X == 1 && hit.Y - tile.Y == 1)
                    {
                        ground.RoadDirection = RoadDirection.NE;
                    }
                }
                else
                {
                    ground.RoadDirection = RoadDirection.Center;
                }
                ground.Data = currentClickData;
            }
            else if (fillToggle.isOn)
            {
                GroundData toReplace = tile.Ground.Data;
                FloodFill(tile, currentClickData, toReplace);
            }
        }

        private void FloodFill(Tile tile, GroundData data, GroundData toReplace)
        {
            if (data == toReplace)
            {
                return;
            }
            Map map = GameManager.Instance.Map;
            Stack<Tile> stack = new Stack<Tile>();
            stack.Push(tile);

            while (stack.Count != 0)
            {
                Tile anchor = stack.Pop();
                if (anchor.Ground.Data == toReplace)
                {
                    anchor.Ground.Data = data;
                    AddTileIfNotNull(stack, map[anchor.X - 1, anchor.Y]);
                    AddTileIfNotNull(stack, map[anchor.X + 1, anchor.Y]);
                    AddTileIfNotNull(stack, map[anchor.X, anchor.Y - 1]);
                    AddTileIfNotNull(stack, map[anchor.X, anchor.Y + 1]);
                }
            }
        }

        private void AddTileIfNotNull(Stack<Tile> stack, Tile tile)
        {
            if (tile)
            {
                stack.Push(tile);
            }
        }

        private GroundData GetCurrentClickData()
        {
            if (Input.GetMouseButton(0))
            {
                return leftClickData;
            }
            else if (Input.GetMouseButton(1))
            {
                return rightClickData;
            }

            return null;
        }

    }
}
