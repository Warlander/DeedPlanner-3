using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Floors;
using Warlander.Deedplanner.Data.Grounds;
using Warlander.Deedplanner.Data.Walls;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Settings;
using Zenject;

namespace Warlander.Deedplanner.Updaters
{
    public class WallUpdater : AbstractUpdater
    {
        [Inject] private DPSettings _settings;
        
        [SerializeField] private Toggle reverseToggle = null;
        [SerializeField] private Toggle automaticReverseToggle = null;

        private void Start()
        {
            automaticReverseToggle.isOn = _settings.WallAutomaticReverse;
            reverseToggle.isOn = _settings.WallReverse;
            
            automaticReverseToggle.onValueChanged.AddListener(AutomaticReverseToggleOnValueChanged);
            reverseToggle.onValueChanged.AddListener(ReverseToggleOnValueChanged);
        }
        
        private void OnEnable()
        {
            LayoutManager.Instance.TileSelectionMode = TileSelectionMode.Borders;
        }

        private void AutomaticReverseToggleOnValueChanged(bool value)
        {
            _settings.Modify(settings =>
            {
                settings.WallAutomaticReverse = automaticReverseToggle.isOn;
            });
        }

        private void ReverseToggleOnValueChanged(bool value)
        {
            _settings.Modify(settings =>
            {
                settings.WallReverse = reverseToggle.isOn;
            });
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

            OverlayMesh overlayMesh = raycast.transform.GetComponent<OverlayMesh>();
            GroundMesh groundMesh = raycast.transform.GetComponent<GroundMesh>();
            TileEntity tileEntity = raycast.transform.GetComponent<TileEntity>();
            Wall wallEntity = tileEntity as Wall;
            
            int floor = 0;
            int x = -1;
            int y = -1;
            bool horizontal = false;

            if (wallEntity && wallEntity.Valid)
            {
                floor = tileEntity.Floor;
                if (LayoutManager.Instance.CurrentCamera.Floor == floor + 1)
                {
                    floor++;
                }
                x = tileEntity.Tile.X;
                y = tileEntity.Tile.Y;
                EntityType type = tileEntity.Type;
                horizontal = (type == EntityType.Hwall || type == EntityType.Hfence);
            }
            else if (overlayMesh || groundMesh)
            {
                if (overlayMesh)
                {
                    floor = LayoutManager.Instance.CurrentCamera.Floor;
                }
                else if (groundMesh)
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
                if (_settings.WallAutomaticReverse && horizontal)
                {
                    Floor nearFloor = GameManager.Instance.Map[x, y - 1].GetTileContent(floor) as Floor;
                    shouldReverse = currentFloor && !nearFloor;
                }
                else if (_settings.WallAutomaticReverse && !horizontal)
                {
                    Floor nearFloor = GameManager.Instance.Map[x - 1, y].GetTileContent(floor) as Floor;
                    shouldReverse = !currentFloor && nearFloor;
                }

                if (_settings.WallReverse)
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
