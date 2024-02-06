using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Floors;
using Warlander.Deedplanner.Data.Grounds;
using Warlander.Deedplanner.Data.Walls;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Gui.Widgets;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Logic.Cameras;
using Warlander.Deedplanner.Settings;
using Zenject;

namespace Warlander.Deedplanner.Updaters
{
    public class WallUpdater : AbstractUpdater
    {
        [Inject] private DPSettings _settings;
        [Inject] private CameraCoordinator _cameraCoordinator;
        [Inject] private DPInput _input;
        [Inject] private GameManager _gameManager;

        [SerializeField] private UnityTree _wallsTree;

        [SerializeField] private Toggle reverseToggle;
        [SerializeField] private Toggle automaticReverseToggle;

        private void Start()
        {
            foreach (WallData data in Database.Walls.Values)
            {
                foreach (string[] category in data.Categories)
                {
                    IconUnityListElement iconListElement = (IconUnityListElement) _wallsTree.Add(data, category);
                    iconListElement.TextureReference = data.Icon;
                }
            }
            
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
            if (_input.UpdatersShared.Placement.WasReleasedThisFrame() || _input.UpdatersShared.Deletion.WasReleasedThisFrame())
            {
                _gameManager.Map.CommandManager.FinishAction();
            }
            
            RaycastHit raycast = _cameraCoordinator.Current.CurrentRaycast;
            if (!raycast.transform)
            {
                return;
            }

            OverlayMesh overlayMesh = raycast.transform.GetComponent<OverlayMesh>();
            GroundMesh groundMesh = raycast.transform.GetComponent<GroundMesh>();
            LevelEntity levelEntity = raycast.transform.GetComponent<LevelEntity>();
            Wall wallEntity = levelEntity as Wall;
            
            int floor = 0;
            int x = -1;
            int y = -1;
            bool horizontal = false;

            if (wallEntity && wallEntity.Valid)
            {
                floor = levelEntity.Floor;
                if (_cameraCoordinator.Current.Floor == floor + 1)
                {
                    floor++;
                }
                x = levelEntity.Tile.X;
                y = levelEntity.Tile.Y;
                EntityType type = levelEntity.Type;
                horizontal = (type == EntityType.Hwall || type == EntityType.Hfence);
            }
            else if (overlayMesh || groundMesh)
            {
                if (overlayMesh)
                {
                    floor = _cameraCoordinator.Current.Floor;
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

            if (_input.UpdatersShared.Placement.ReadValue<float>() > 0)
            {
                Floor currentFloor = _gameManager.Map[x, y].GetTileContent(floor) as Floor;
                bool shouldReverse = false;
                if (_settings.WallAutomaticReverse && horizontal)
                {
                    Floor nearFloor = _gameManager.Map[x, y - 1].GetTileContent(floor) as Floor;
                    shouldReverse = currentFloor && !nearFloor;
                }
                else if (_settings.WallAutomaticReverse && !horizontal)
                {
                    Floor nearFloor = _gameManager.Map[x - 1, y].GetTileContent(floor) as Floor;
                    shouldReverse = !currentFloor && nearFloor;
                }

                if (_settings.WallReverse)
                {
                    shouldReverse = !shouldReverse;
                }

                WallData data = _wallsTree.SelectedValue as WallData;
                if (horizontal)
                {
                    _gameManager.Map[x, y].SetHorizontalWall(data, shouldReverse, floor);
                }
                else
                {
                    _gameManager.Map[x, y].SetVerticalWall(data, shouldReverse, floor);
                }
            }
            else if (_input.UpdatersShared.Deletion.ReadValue<float>() > 0)
            {
                if (floor != _cameraCoordinator.Current.Floor)
                {
                    return;
                }
                if (horizontal)
                {
                    _gameManager.Map[x, y].SetHorizontalWall(null, false, floor);
                }
                else
                {
                    _gameManager.Map[x, y].SetVerticalWall(null, false, floor);
                }
            }
        }
    }
}
