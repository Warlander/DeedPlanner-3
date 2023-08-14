using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Grounds;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Logic.Cameras;
using Zenject;

namespace Warlander.Deedplanner.Updaters
{
    public class GroundUpdater : AbstractUpdater
    {
        [Inject] private CameraCoordinator _cameraCoordinator;
        [Inject] private DPInput _input;
        
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
                leftClickText.text = leftClickData.Name;
                leftClickData.Tex2d.LoadOrGetSprite(sprite => leftClickImage.sprite = sprite);
            }
        }

        private GroundData RightClickData {
            get => rightClickData;
            set {
                rightClickData = value;
                rightClickText.text = rightClickData.Name;
                rightClickData.Tex2d.LoadOrGetSprite(sprite => rightClickImage.sprite = sprite);
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
            LeftClickData = Database.DefaultGroundData;
            RightClickData = Database.DefaultSecondaryGroundData;
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

        private void OnGroundsTreeValueChanged(object value)
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
            if (_input.UpdatersShared.Placement.WasReleasedThisFrame() || _input.UpdatersShared.Deletion.WasReleasedThisFrame())
            {
                GameManager.Instance.Map.CommandManager.FinishAction();
            }
            
            RaycastHit raycast = _cameraCoordinator.Current.CurrentRaycast;
            if (!raycast.transform)
            {
                return;
            }

            Map map = GameManager.Instance.Map;
            int tileX = Mathf.FloorToInt(raycast.point.x / 4f);
            int tileZ = Mathf.FloorToInt(raycast.point.z / 4f);
            Tile tile = map[tileX, tileZ];
            Ground ground = tile.Ground;

            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                if (_input.UpdatersShared.Placement.WasPressedThisFrame())
                {
                    LeftClickData = ground.Data;
                }
                else if (_input.UpdatersShared.Deletion.WasPressedThisFrame())
                {
                    RightClickData = ground.Data;
                }
            }
            
            GroundData currentClickData = GetCurrentClickData();
            if (currentClickData == null)
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
            Stack<Tile> checkStack = new Stack<Tile>();
            checkStack.Push(tile);
            HashSet<Tile> tilesToChange = new HashSet<Tile>();

            while (checkStack.Count != 0)
            {
                Tile anchor = checkStack.Pop();
                if (anchor.Ground.Data == toReplace && !tilesToChange.Contains(anchor))
                {
                    tilesToChange.Add(anchor);
                    AddTileIfNotNull(checkStack, map[anchor.X - 1, anchor.Y]);
                    AddTileIfNotNull(checkStack, map[anchor.X + 1, anchor.Y]);
                    AddTileIfNotNull(checkStack, map[anchor.X, anchor.Y - 1]);
                    AddTileIfNotNull(checkStack, map[anchor.X, anchor.Y + 1]);
                }
            }

            foreach (Tile tileToChange in tilesToChange)
            {
                tileToChange.Ground.Data = data;
            }
        }

        private void AddTileIfNotNull(Stack<Tile> stack, Tile tile)
        {
            if (tile != null)
            {
                stack.Push(tile);
            }
        }

        private GroundData GetCurrentClickData()
        {
            if (_input.UpdatersShared.Placement.ReadValue<float>() > 0)
            {
                return leftClickData;
            }
            else if (_input.UpdatersShared.Deletion.ReadValue<float>() > 0)
            {
                return rightClickData;
            }

            return null;
        }
    }
}
