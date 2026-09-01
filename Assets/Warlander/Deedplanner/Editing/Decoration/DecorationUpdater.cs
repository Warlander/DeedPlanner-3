using Warlander.Deedplanner.Persistence;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Decorations;
using Warlander.Deedplanner.Docks;
using Warlander.Deedplanner.Data.Floors;
using Warlander.Deedplanner.Data.Grounds;
using Warlander.Deedplanner.Graphics;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Rendering.Outline;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Cameras;
using Warlander.Deedplanner.Logging;
using Warlander.Deedplanner.Settings;

namespace Warlander.Deedplanner.Editing
{
    public class DecorationUpdater : IUpdater
    {
        public static readonly LogCategory Category = new LogCategory("Decorations");

        private static readonly Color AllowedGhostColor = new Color(0f, 1f, 0f, 0.5882353f);
        private static readonly Color DisabledGhostColor = new Color(1f, 0f, 0f, 0.5882353f);
        private const float MinimumPlacementGap = 0.25f;
        private const float CornerSnapDistance = 0.25f;

        private readonly IDecorationUpdaterView _view;
        private readonly DPSettings _settings;
        private readonly CameraCoordinator _cameraCoordinator;
        private readonly DPInput _input;
        private readonly MapHandler _mapHandler;
        private readonly IOutlineCoordinator _outlineCoordinator;
        private readonly ISharedMaterials _sharedMaterials;
        private readonly TabContext _tabContext;
        private readonly ICategoryLogger _logger;
        private readonly PreviewAtlasCatalog _previewAtlasCatalog;
        private readonly IDataCatalog _dataCatalog;

        public Tab TargetTab => Tab.Objects;

        private DecorationData _selectedDecoration;
        private DecorationData _lastGhostData;
        private string _rotationSensitivity;
        private bool _rotationSnapping;
        private GameObject _ghostObject;

        private MaterialPropertyBlock _allowedGhostPropertyBlock;
        private MaterialPropertyBlock _disabledGhostPropertyBlock;

        private Vector3 _position;
        private float _rotation;
        private bool _placingDecoration = false;
        private Tile _targetedTile;
        private Vector2 _dragStartPos;

        private bool _isScrollRotate = false;

        public DecorationUpdater(IDecorationUpdaterView view, DPSettings settings, CameraCoordinator cameraCoordinator,
            DPInput input, MapHandler mapHandler, IOutlineCoordinator outlineCoordinator,
            ISharedMaterials sharedMaterials, TabContext tabContext, ILoggerSource loggerSource,
            PreviewAtlasCatalog previewAtlasCatalog, IDataCatalog dataCatalog)
        {
            _view = view;
            _settings = settings;
            _cameraCoordinator = cameraCoordinator;
            _input = input;
            _mapHandler = mapHandler;
            _outlineCoordinator = outlineCoordinator;
            _sharedMaterials = sharedMaterials;
            _tabContext = tabContext;
            _logger = loggerSource.Create(Category);
            _previewAtlasCatalog = previewAtlasCatalog;
            _dataCatalog = dataCatalog;
        }

        public void Initialize()
        {
            _allowedGhostPropertyBlock = new MaterialPropertyBlock();
            _allowedGhostPropertyBlock.SetColor(ShaderPropertyIds.BaseColor, AllowedGhostColor);
            _disabledGhostPropertyBlock = new MaterialPropertyBlock();
            _disabledGhostPropertyBlock.SetColor(ShaderPropertyIds.BaseColor, DisabledGhostColor);

            _view.DecorationSelected += OnDecorationSelected;
            _view.SnapToGridChanged += OnSnapToGridChanged;
            _view.RotationSnappingChanged += OnRotationSnappingChanged;
            _view.RotationSensitivityChanged += OnRotationSensitivityChanged;

            foreach (DecorationData data in _dataCatalog.GetAllDecorations())
            {
                foreach (string[] category in data.Categories)
                {
                    _previewAtlasCatalog.TryGetSprite(PreviewAtlasCategory.Objects, data.ShortName, out Sprite sprite);
                    _view.AddDecorationEntry(data, category, sprite);
                }
            }

            _rotationSensitivity = _settings.DecorationRotationSensitivity;
            _rotationSnapping = _settings.DecorationRotationSnapping;

            _view.SetRotationSensitivity(_rotationSensitivity);
            _view.SetSnapToGrid(_settings.DecorationSnapToGrid);
            _view.SetRotationSnapping(_rotationSnapping);
            _view.PushSelection();
        }

        public void Enable()
        {
            _tabContext.TileSelectionMode = TileSelectionMode.Nothing;
        }

        private void OnDecorationSelected(DecorationData data)
        {
            _selectedDecoration = data;
        }

        private void OnSnapToGridChanged(bool value)
        {
            _settings.Modify(settings =>
            {
                settings.DecorationSnapToGrid = value;
            });
        }

        private void OnRotationSnappingChanged(bool value)
        {
            _rotationSnapping = value;
            _settings.Modify(settings =>
            {
                settings.DecorationRotationSnapping = value;
            });
        }

        private void OnRotationSensitivityChanged(string value)
        {
            _rotationSensitivity = value;
            _settings.Modify(settings =>
            {
                settings.DecorationRotationSensitivity = value;
            });
        }

        public void Tick()
        {
            float rotationEditSensitivity = 1;
            float.TryParse(_rotationSensitivity, NumberStyles.Any, CultureInfo.InvariantCulture, out rotationEditSensitivity);

            RaycastHit raycast = _cameraCoordinator.Current.CurrentRaycast;
            if (!raycast.transform)
            {
                if (_ghostObject)
                {
                    _ghostObject.SetActive(false);
                }
                return;
            }

            DecorationData data = _selectedDecoration;
            if (data == null)
            {
                return;
            }

            OverlayMesh overlayMesh = raycast.transform.GetComponent<OverlayMesh>();
            GroundMesh groundMesh = raycast.transform.GetComponent<GroundMesh>();
            LevelEntity levelEntity = raycast.transform.GetComponent<LevelEntity>();
            Dock dock = raycast.transform.GetComponent<Dock>();
            bool validDock = dock != null && dock.Tile != null && dock.Tile.Dock == dock;

            Material ghostMaterial = _sharedMaterials.GhostMaterial;
            if (data != _lastGhostData || !_ghostObject)
            {
                _lastGhostData = data;
                data.Model.CreateOrGetModel(ghostMaterial, OnGhostCreated);
                return;
            }

            int targetFloor = _cameraCoordinator.Current.Level;
            if (levelEntity && levelEntity.Valid && levelEntity.GetType() == typeof(Floor))
            {
                targetFloor = levelEntity.Level;
            }
            else if (validDock)
            {
                targetFloor = dock.AnchorLevel;
            }

            if (data.CenterOnly || data.Tree || data.Bush)
            {
                targetFloor = 0;
            }

            Map map = _mapHandler.Map;

            if (_targetedTile != null)
            {
                foreach (Decoration decoration in _targetedTile.GetDecorations())
                {
                    _outlineCoordinator.RemoveObject(decoration, 1);
                }
            }

            if (!_placingDecoration)
            {
                _position = CalculateCorrectedPosition(raycast.point, data, _settings.DecorationSnapToGrid);
                _targetedTile = null;
                if (overlayMesh)
                {
                    int tileX = Mathf.FloorToInt(_position.x / 4f);
                    int tileY = Mathf.FloorToInt(_position.z / 4f);
                    _targetedTile = map[tileX, tileY];
                    Dock targetedDock = _targetedTile?.Dock;
                    _position.y = targetedDock != null
                        ? (targetedDock.Height - targetedDock.AnchorLevel * 30) * 0.1f
                        : map.GetInterpolatedHeight(_position.x, _position.z);
                    if (data.Floating)
                    {
                        _position.y = Mathf.Max(_position.y, 0);
                    }
                    else
                    {
                        float floorHeight = 3f;
                        _position.y += targetFloor * floorHeight;
                    }
                }
                else if (levelEntity && levelEntity.Valid)
                {
                    _targetedTile = levelEntity.Tile;
                }
                else if (validDock)
                {
                    _targetedTile = dock.Tile;
                }
            }

            if (_targetedTile != null)
            {
                foreach (Decoration decoration in _targetedTile.GetDecorations())
                {
                    _outlineCoordinator.AddObject(decoration, OutlineType.Neutral, 1);
                }
            }

            bool canPlaceNewObject = overlayMesh || groundMesh ||
                (levelEntity && levelEntity.Valid && levelEntity.GetType() == typeof(Floor)) ||
                validDock;
            if (canPlaceNewObject || _placingDecoration)
            {
                _ghostObject.gameObject.SetActive(true);
                _ghostObject.transform.position = _position;
            }
            else
            {
                _ghostObject.gameObject.SetActive(false);
            }

            bool placementOverlap = true;
            Vector2 position2d = new Vector2(_position.x, _position.z);
            IEnumerable<Decoration> nearbyDecorations = GetAllNearbyDecorations(_targetedTile);

            foreach (Decoration decoration in nearbyDecorations)
            {
                Vector3 decorationPosition3d = decoration.transform.position;
                Vector2 decorationPosition2d = new Vector2(decorationPosition3d.x, decorationPosition3d.z);
                float distance = Vector2.Distance(position2d, decorationPosition2d);
                if (distance < MinimumPlacementGap)
                {
                    placementOverlap = false;
                    break;
                }
            }

            ToggleGhostPropertyBlock(placementOverlap ? _allowedGhostPropertyBlock : _disabledGhostPropertyBlock);

            if (_input.UpdatersShared.Placement.WasPressedThisFrame() && canPlaceNewObject && _targetedTile != null)
            {
                _placingDecoration = true;
                _dragStartPos = _cameraCoordinator.Current.MousePosition;
            }

            if (_input.DecorationUpdater.SmoothObjectRotate.IsPressed())
            {
                _isScrollRotate = true;
                _rotation += _input.DecorationUpdater.SmoothObjectRotate.ReadValue<float>();
                _ghostObject.transform.localRotation = Quaternion.Euler(0, _rotation, 0);
            }
            else if (_input.DecorationUpdater.SnappyObjectRotate.IsPressed())
            {
                _isScrollRotate = true;
                if (_input.DecorationUpdater.SnappyObjectRotate.ReadValue<float>() > 0)
                {
                    _rotation += 11.25f;
                }
                else
                {
                    _rotation -= 11.25f;
                }
                _rotation = Mathf.Round(_rotation / 11.25f) * 11.25f;
                _ghostObject.transform.localRotation = Quaternion.Euler(0, _rotation, 0);
            }

            if (!_isScrollRotate && _input.UpdatersShared.Placement.ReadValue<float>() > 0 && _placingDecoration)
            {
                Vector2 dragEndPos = _cameraCoordinator.Current.MousePosition;
                Vector2 difference = dragEndPos - _dragStartPos;
                _rotation = -difference.x * rotationEditSensitivity;
                if (_rotationSnapping)
                {
                    _rotation = Mathf.Round(_rotation / 45f) * 45f;
                }
                _ghostObject.transform.localRotation = Quaternion.Euler(0, _rotation, 0);
            }

            if (_input.UpdatersShared.Placement.WasReleasedThisFrame() && _placingDecoration)
            {
                float decorationPositionX = _position.x - _targetedTile.X * 4f;
                float decorationPositionY = _position.z - _targetedTile.Y * 4f;
                Vector2 decorationPosition = new Vector2(decorationPositionX, decorationPositionY);
                Decoration placed = _targetedTile.SetDecoration(data, decorationPosition, _rotation * Mathf.Deg2Rad, targetFloor, data.Floating);
                if (placed == null)
                {
                    _logger.Warning("Attempted placing decoration at X: " + decorationPosition.x + ", Y: " + decorationPosition.y);
                }
                map.CommandManager.FinishAction();

                _placingDecoration = false;
                _ghostObject.transform.localRotation = Quaternion.identity;
                _isScrollRotate = false;
                _rotation = 0f;
            }

            if (_input.UpdatersShared.Deletion.WasPerformedThisFrame())
            {
                _placingDecoration = false;
                _isScrollRotate = false;
                _ghostObject.transform.localRotation = Quaternion.identity;
            }

            if (_input.UpdatersShared.Deletion.WasPerformedThisFrame() && !_placingDecoration)
            {
                IEnumerable<Decoration> decorationsOnTile = _targetedTile.GetDecorations();
                foreach (Decoration decoration in decorationsOnTile)
                {
                    _targetedTile.SetDecoration(null, decoration.Position, decoration.Rotation, targetFloor);
                }
                map.CommandManager.FinishAction();
            }

            if (_input.DecorationUpdater.DeleteSingleObject.WasPressedThisFrame() && !_placingDecoration)
            {
                foreach (Decoration decoration in nearbyDecorations)
                {
                    Vector3 decorationPosition3d = decoration.transform.position;
                    Vector2 decorationPosition2d = new Vector2(decorationPosition3d.x, decorationPosition3d.z);
                    float distance = Vector2.Distance(position2d, decorationPosition2d);
                    if (distance < MinimumPlacementGap)
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
            if (_ghostObject)
            {
                Object.Destroy(_ghostObject);
            }

            _ghostObject = ghost;
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
                if (magnitude < CornerSnapDistance)
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
            if (!_ghostObject)
            {
                return;
            }

            foreach (Renderer render in _ghostObject.GetComponentsInChildren<Renderer>())
            {
                render.SetPropertyBlock(propertyBlock);
            }
        }

        public void Disable()
        {
            if (_ghostObject)
            {
                _ghostObject.SetActive(false);
            }
            ResetState();
        }

        private void ResetState()
        {
            _placingDecoration = false;
            _dragStartPos = new Vector2();

            _mapHandler.Map.CommandManager.UndoAction();
        }
    }
}
