using System;
using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Caves;
using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Domain.Entities.Caves;
using Warlander.Deedplanner.Domain.Entities.Decorations;
using Warlander.Deedplanner.Domain.Entities.Floors;
using Warlander.Deedplanner.Domain.Entities.Grounds;
using Warlander.Deedplanner.Domain.Entities.Roofs;
using Warlander.Deedplanner.Domain.Entities.Walls;
using Warlander.Deedplanner.Persistence;
using Warlander.Deedplanner.Docks;
using Warlander.Deedplanner.Bridges;
using System.Linq;

namespace Warlander.Deedplanner.Editing
{
    public sealed class MapEditFacade : IMapEditFacade
    {
        private readonly MapHandler _mapHandler;
        private readonly SymmetrySession _symmetry;
        private readonly DockFactory _dockFactory;
        private readonly IDataCatalog _dataCatalog;
        private readonly BridgeFactory _bridgeFactory;

        public MapEditFacade(MapHandler mapHandler, SymmetrySession symmetry, DockFactory dockFactory,
            IDataCatalog dataCatalog, BridgeFactory bridgeFactory)
        {
            _mapHandler = mapHandler;
            _symmetry = symmetry;
            _dockFactory = dockFactory;
            _dataCatalog = dataCatalog;
            _bridgeFactory = bridgeFactory;
        }

        public void PaintGround(int x, int y, GroundData data, RoadDirection direction)
        {
            Map map = _mapHandler.Map;
            ForEachCell(map, _symmetry.Capture(), x, y, target =>
            {
                Ground ground = map[target.X, target.Y].Ground;
                RoadDirection transformedDirection = target.Transform.Transform(direction);
                if (ground.RoadDirection != transformedDirection)
                {
                    ground.RoadDirection = transformedDirection;
                }
                if (ground.Data != data)
                {
                    ground.Data = data;
                }
            });
        }

        public void ReplaceGroundData(int x, int y, GroundData data)
        {
            Map map = _mapHandler.Map;
            ForEachCell(map, _symmetry.Capture(), x, y, target =>
            {
                Ground ground = map[target.X, target.Y].Ground;
                if (ground.Data != data)
                {
                    ground.Data = data;
                }
            });
        }

        public ICaveEditStroke BeginCaveTerrainStroke(CaveData data,
            CaveOccupiedCellPolicy occupiedCellPolicy)
        {
            Map map = _mapHandler.Map;
            return new SymmetryCaveStroke(map, map.CaveEditor.BeginTerrainStroke(data, occupiedCellPolicy),
                _symmetry.Capture());
        }

        public IMapHeightEdit BeginHeightEdit(bool cave, CaveHeightMode caveMode,
            bool preserveCeilingHeight, HeightEditBehavior behavior)
        {
            Map map = _mapHandler.Map;
            ICaveHeightEdit caveEdit = null;
            if (cave)
            {
                caveEdit = caveMode == CaveHeightMode.Floor
                    ? map.CaveEditor.BeginFloorHeightEdit(preserveCeilingHeight)
                    : map.CaveEditor.BeginClearanceEdit();
            }
            return new SymmetryHeightEdit(map, caveEdit, _symmetry.Capture(), cave, caveMode, behavior);
        }

        public void SetFloor(int x, int y, FloorData data, EntityOrientation orientation, int level)
        {
            Map map = _mapHandler.Map;
            ForEachCell(map, _symmetry.Capture(), x, y, target =>
            {
                if (level < 0 && !map.Caves.IsOpen(target.X, target.Y))
                {
                    return;
                }
                DockRealm realm = level < 0 ? DockRealm.Cave : DockRealm.Surface;
                Dock dock = map[target.X, target.Y].GetDock(realm);
                if (data != null && dock != null && dock.AnchorLevel == level)
                {
                    return;
                }
                map[target.X, target.Y].SetFloor(data, target.Transform.Transform(orientation), level);
            });
        }

        public void SetRoof(int x, int y, RoofData data, int level)
        {
            Map map = _mapHandler.Map;
            ForEachCell(map, _symmetry.Capture(), x, y,
                target =>
                {
                    if (level >= 0 || map.Caves.IsOpen(target.X, target.Y))
                    {
                        map[target.X, target.Y].SetRoof(data, level);
                    }
                });
        }

        public void SetHorizontalWall(int x, int y, WallData data, bool reversed, int level)
        {
            Map map = _mapHandler.Map;
            if (x < 0 || x >= map.Width || y < 0 || y > map.Height)
            {
                return;
            }
            SymmetrySnapshot snapshot = _symmetry.Capture();
            var visited = new HashSet<GridCoordinate>();
            snapshot.ForEachTransform(transform =>
            {
                int targetX = snapshot.TransformCellX(x, transform);
                int targetY = snapshot.TransformVertexY(y, transform);
                var coordinate = new GridCoordinate(targetX, targetY);
                if (targetX < 0 || targetX >= map.Width || targetY < 0 || targetY > map.Height ||
                    !visited.Add(coordinate))
                {
                    return;
                }
                if (level < 0 && (targetY >= map.Height || !map.Caves.IsOpen(targetX, targetY)))
                {
                    return;
                }

                bool targetReversed = (transform & SymmetryTransform.ReflectY) != 0 ? !reversed : reversed;
                map[targetX, targetY].SetHorizontalWall(data, targetReversed, level);
            });
        }

        public void SetVerticalWall(int x, int y, WallData data, bool reversed, int level)
        {
            Map map = _mapHandler.Map;
            if (x < 0 || x > map.Width || y < 0 || y >= map.Height)
            {
                return;
            }
            SymmetrySnapshot snapshot = _symmetry.Capture();
            var visited = new HashSet<GridCoordinate>();
            snapshot.ForEachTransform(transform =>
            {
                int targetX = snapshot.TransformVertexX(x, transform);
                int targetY = snapshot.TransformCellY(y, transform);
                var coordinate = new GridCoordinate(targetX, targetY);
                if (targetX < 0 || targetX > map.Width || targetY < 0 || targetY >= map.Height ||
                    !visited.Add(coordinate))
                {
                    return;
                }
                if (level < 0 && (targetX >= map.Width || !map.Caves.IsOpen(targetX, targetY)))
                {
                    return;
                }

                bool targetReversed = (transform & SymmetryTransform.ReflectX) != 0 ? !reversed : reversed;
                map[targetX, targetY].SetVerticalWall(data, targetReversed, level);
            });
        }

        public Decoration SetDecoration(int x, int y, DecorationData data, Vector2 localPosition, float rotation,
            int level, bool floatOnWater = false)
        {
            Map map = _mapHandler.Map;
            if (x < 0 || x >= map.Width || y < 0 || y >= map.Height)
            {
                return null;
            }
            SymmetrySnapshot snapshot = _symmetry.Capture();
            var visited = new HashSet<PoseCoordinate>();
            Decoration original = null;
            snapshot.ForEachTransform(transform =>
            {
                float worldX = x * 4f + localPosition.x;
                float worldY = y * 4f + localPosition.y;
                worldX = snapshot.TransformWorldX(worldX, transform);
                worldY = snapshot.TransformWorldY(worldY, transform);
                int targetX = Mathf.FloorToInt(worldX / 4f);
                int targetY = Mathf.FloorToInt(worldY / 4f);
                var targetPosition = new Vector2(worldX - targetX * 4f, worldY - targetY * 4f);
                var coordinate = new PoseCoordinate(targetX, targetY, targetPosition);
                if (targetX < 0 || targetX >= map.Width || targetY < 0 || targetY >= map.Height ||
                    !visited.Add(coordinate))
                {
                    return;
                }
                if (level < 0 && !map.Caves.IsOpen(targetX, targetY))
                {
                    return;
                }
                if (data != null && HasNearbyDecoration(map, worldX, worldY))
                {
                    return;
                }

                Decoration result = map[targetX, targetY].SetDecoration(data, targetPosition,
                    transform.TransformRotation(rotation), level, floatOnWater);
                if (transform == SymmetryTransform.Identity)
                {
                    original = result;
                }
            });

            return original;
        }

        public IMapDockStroke BeginDockStroke(int anchorX, int anchorY)
        {
            return new SymmetryDockStroke(_mapHandler.Map, _dockFactory, _dataCatalog,
                _symmetry.Capture(), anchorX, anchorY);
        }

        public Bridge PlaceBridge(BridgePlacementRequest request)
        {
            Map map = _mapHandler.Map;
            SymmetrySnapshot snapshot = _symmetry.Capture();
            Bridge original = null;
            var visited = new HashSet<string>();
            var occupied = new HashSet<BridgePartCoordinate>();
            var plans = new List<BridgePlacementPlan>();
            snapshot.ForEachTransform(transform =>
            {
                TileCoords start = TransformCell(snapshot, request.Start, transform);
                TileCoords end = TransformCell(snapshot, request.End, transform);
                bool reverse = ShouldReverseBridge(start, end);
                if (reverse)
                {
                    TileCoords swap = start;
                    start = end;
                    end = swap;
                }
                string key = BridgeKey(start, end);
                if (!visited.Add(key) || !BridgeSpanValidator.Validate(map, start, end, out _))
                {
                    return;
                }
                string segments = reverse ? Reverse(request.Segments) : request.Segments;
                HashSet<BridgePartCoordinate> footprint = CreateBridgeFootprint(start, end);
                if (footprint.Any(occupied.Contains))
                {
                    return;
                }
                occupied.UnionWith(footprint);
                plans.Add(new BridgePlacementPlan(start, end, segments, transform));
            });
            foreach (BridgePlacementPlan plan in plans)
            {
                Bridge bridge = _bridgeFactory.CreateBridge(map, plan.Start, plan.End, request.Material, request.Type,
                    request.AdditionalData, plan.Segments);
                map.CommandManager.AddToActionAndExecute(new BridgePlacementCommand(map, bridge));
                if (plan.Transform == SymmetryTransform.Identity)
                {
                    original = bridge;
                }
            }
            FinishAction();
            return original;
        }

        private static HashSet<BridgePartCoordinate> CreateBridgeFootprint(TileCoords start, TileCoords end)
        {
            int minX = Mathf.Min(start.X, end.X);
            int maxX = Mathf.Max(start.X, end.X);
            int minY = Mathf.Min(start.Y, end.Y);
            int maxY = Mathf.Max(start.Y, end.Y);
            bool vertical = maxY - minY > maxX - minX;
            if (vertical)
            {
                minY++;
                maxY--;
            }
            else
            {
                minX++;
                maxX--;
            }

            int realm = start.Level < 0 ? -1 : 0;
            var footprint = new HashSet<BridgePartCoordinate>();
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    footprint.Add(new BridgePartCoordinate(x, y, realm));
                }
            }
            return footprint;
        }

        public void RemoveBridge(Bridge bridge)
        {
            ForEachSymmetricBridge(bridge, (target, transform) =>
                _mapHandler.Map.CommandManager.AddToActionAndExecute(
                    new BridgeRemovalCommand(_mapHandler.Map, target)));
            FinishAction();
        }

        public void ChangeBridgeMaterial(Bridge bridge, BridgeData material, string newSegments)
        {
            ForEachSymmetricBridge(bridge, (target, transform) =>
            {
                string transformedSegments = ReversesBridgeLength(target, transform)
                    ? Reverse(newSegments)
                    : newSegments;
                _mapHandler.Map.CommandManager.AddToActionAndExecute(new BridgeMaterialChangeCommand(
                    _mapHandler.Map, target, target.Data, material, target.GetSegmentsString(), transformedSegments));
            });
            FinishAction();
        }

        public void ChangeBridgeExtraArgument(Bridge bridge, int value)
        {
            ForEachSymmetricBridge(bridge, (target, transform) =>
                _mapHandler.Map.CommandManager.AddToActionAndExecute(new BridgeExtraArgumentChangeCommand(
                    _mapHandler.Map, target, target.AdditionalData, value)));
            FinishAction();
        }

        public void ChangeBridgeSegments(Bridge bridge, string newSegments)
        {
            ForEachSymmetricBridge(bridge, (target, transform) =>
            {
                string transformedSegments = ReversesBridgeLength(target, transform)
                    ? Reverse(newSegments)
                    : newSegments;
                _mapHandler.Map.CommandManager.AddToActionAndExecute(new BridgeSegmentsChangeCommand(
                    _mapHandler.Map, target, target.GetSegmentsString(), transformedSegments));
            });
            FinishAction();
        }

        public IBridgePavingStroke BeginBridgePavingStroke() =>
            new SymmetryBridgePavingStroke(this, _symmetry.Capture());

        private void ForEachSymmetricBridgePart(BridgePart part, SymmetrySnapshot snapshot,
            Action<BridgePart> action)
        {
            int segment = part.SegmentIndex;
            int lane = part.LaneIndex;
            ForEachSymmetricBridge(part.ParentBridge, snapshot, (target, transform) =>
            {
                int targetSegment = ReversesBridgeLength(target, transform)
                    ? target.SegmentCount - segment - 1
                    : segment;
                bool flipsLane = target.IsLongitudinal()
                    ? (transform & SymmetryTransform.ReflectX) != 0
                    : (transform & SymmetryTransform.ReflectY) != 0;
                int laneCount = target.GetSegmentParts(targetSegment).Count;
                int targetLane = flipsLane ? laneCount - lane - 1 : lane;
                BridgePart targetPart = target.GetPart(targetSegment, targetLane);
                if (targetPart != null)
                {
                    action(targetPart);
                }
            });
        }

        public void RecordBridgePaving(BridgePart[] parts, BridgePavementData[] oldPavements,
            BridgePavementData[] newPavements)
        {
            _mapHandler.Map.CommandManager.AddToStack(
                new BridgePavingChangeCommand(parts, oldPavements, newPavements));
        }

        private void ForEachSymmetricBridge(Bridge bridge, Action<Bridge, SymmetryTransform> action)
        {
            ForEachSymmetricBridge(bridge, _symmetry.Capture(), action);
        }

        private void ForEachSymmetricBridge(Bridge bridge, SymmetrySnapshot snapshot,
            Action<Bridge, SymmetryTransform> action)
        {
            Map map = _mapHandler.Map;
            var visited = new HashSet<Bridge>();
            snapshot.ForEachTransform(transform =>
            {
                var footprint = new HashSet<BridgePartCoordinate>();
                foreach (BridgePart part in bridge.Parts)
                {
                    footprint.Add(new BridgePartCoordinate(
                        snapshot.TransformCellX(part.Tile.X, transform),
                        snapshot.TransformCellY(part.Tile.Y, transform), part.Level));
                }
                Bridge target = map.Bridges.FirstOrDefault(candidate =>
                    BridgeMatches(candidate, footprint));
                if (target != null && visited.Add(target))
                {
                    action(target, transform);
                }
            });
        }

        private static bool BridgeMatches(Bridge bridge, HashSet<BridgePartCoordinate> footprint)
        {
            if (bridge.Parts.Count != footprint.Count)
            {
                return false;
            }
            return bridge.Parts.All(part =>
                footprint.Contains(new BridgePartCoordinate(part.Tile.X, part.Tile.Y, part.Level)));
        }

        private readonly struct BridgePartCoordinate : IEquatable<BridgePartCoordinate>
        {
            private readonly int _x;
            private readonly int _y;
            private readonly int _level;

            public BridgePartCoordinate(int x, int y, int level)
            {
                _x = x;
                _y = y;
                _level = level;
            }

            public bool Equals(BridgePartCoordinate other) =>
                _x == other._x && _y == other._y && _level == other._level;

            public override bool Equals(object obj) =>
                obj is BridgePartCoordinate other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(_x, _y, _level);
        }

        private sealed class BridgePlacementPlan
        {
            public TileCoords Start { get; }
            public TileCoords End { get; }
            public string Segments { get; }
            public SymmetryTransform Transform { get; }

            public BridgePlacementPlan(TileCoords start, TileCoords end, string segments,
                SymmetryTransform transform)
            {
                Start = start;
                End = end;
                Segments = segments;
                Transform = transform;
            }
        }

        private sealed class SymmetryBridgePavingStroke : IBridgePavingStroke
        {
            private readonly MapEditFacade _facade;
            private readonly SymmetrySnapshot _snapshot;

            public SymmetryBridgePavingStroke(MapEditFacade facade, SymmetrySnapshot snapshot)
            {
                _facade = facade;
                _snapshot = snapshot;
            }

            public void ForEachSymmetricPart(BridgePart part, Action<BridgePart> action)
            {
                _facade.ForEachSymmetricBridgePart(part, _snapshot, action);
            }
        }

        private static TileCoords TransformCell(SymmetrySnapshot snapshot, TileCoords value,
            SymmetryTransform transform) => new TileCoords(snapshot.TransformCellX(value.X, transform),
            snapshot.TransformCellY(value.Y, transform), value.Level);

        private static bool ShouldReverseBridge(TileCoords start, TileCoords end) =>
            Mathf.Abs(end.Y - start.Y) > Mathf.Abs(end.X - start.X)
                ? start.Y > end.Y
                : start.X > end.X;

        private static bool ReversesBridgeLength(Bridge bridge, SymmetryTransform transform) =>
            bridge.IsLongitudinal()
                ? (transform & SymmetryTransform.ReflectY) != 0
                : (transform & SymmetryTransform.ReflectX) != 0;

        private static int TransformPriority(SymmetryTransform transform)
        {
            switch (transform)
            {
                case SymmetryTransform.Identity:
                    return 0;
                case SymmetryTransform.ReflectX:
                    return 1;
                case SymmetryTransform.ReflectY:
                    return 2;
                default:
                    return 3;
            }
        }

        private static string Reverse(string value) => new string(value.Reverse().ToArray());

        private static string BridgeKey(TileCoords start, TileCoords end) =>
            start.X + ":" + start.Y + ":" + start.Level + "-" + end.X + ":" + end.Y + ":" + end.Level;

        public void FinishAction()
        {
            _mapHandler.Map?.CommandManager.FinishAction();
        }

        public void CancelAction()
        {
            _mapHandler.Map?.CommandManager.UndoAction();
        }

        internal static void ForEachCell(Map map, SymmetrySnapshot snapshot, int x, int y,
            Action<SymmetryCellTarget> action)
        {
            if (x < 0 || x >= map.Width || y < 0 || y >= map.Height)
            {
                return;
            }
            var visited = new HashSet<GridCoordinate>();
            snapshot.ForEachTransform(transform =>
            {
                int targetX = snapshot.TransformCellX(x, transform);
                int targetY = snapshot.TransformCellY(y, transform);
                var coordinate = new GridCoordinate(targetX, targetY);
                if (targetX < 0 || targetX >= map.Width || targetY < 0 || targetY >= map.Height ||
                    !visited.Add(coordinate))
                {
                    return;
                }

                action(new SymmetryCellTarget(targetX, targetY, transform));
            });
        }

        private static bool HasNearbyDecoration(Map map, float worldX, float worldY)
        {
            int centerX = Mathf.FloorToInt(worldX / 4f);
            int centerY = Mathf.FloorToInt(worldY / 4f);
            var target = new Vector2(worldX, worldY);
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    int x = centerX + offsetX;
                    int y = centerY + offsetY;
                    if (x < 0 || x >= map.Width || y < 0 || y >= map.Height)
                    {
                        continue;
                    }
                    foreach (Decoration decoration in map[x, y].GetDecorations())
                    {
                        Vector3 position = decoration.transform.position;
                        if (Vector2.Distance(target, new Vector2(position.x, position.z)) < 0.25f)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        internal static void ForEachVertex(Map map, SymmetrySnapshot snapshot, int x, int y,
            Action<SymmetryVertexTarget> action)
        {
            var visited = new HashSet<GridCoordinate>();
            snapshot.ForEachTransform(transform =>
            {
                int targetX = snapshot.TransformVertexX(x, transform);
                int targetY = snapshot.TransformVertexY(y, transform);
                var coordinate = new GridCoordinate(targetX, targetY);
                if (targetX < 0 || targetX > map.Width || targetY < 0 || targetY > map.Height ||
                    !visited.Add(coordinate))
                {
                    return;
                }

                action(new SymmetryVertexTarget(targetX, targetY, transform));
            });
        }

        private readonly struct GridCoordinate : IEquatable<GridCoordinate>
        {
            private readonly int _x;
            private readonly int _y;

            public GridCoordinate(int x, int y)
            {
                _x = x;
                _y = y;
            }

            public int X => _x;
            public int Y => _y;

            public bool Equals(GridCoordinate other) => _x == other._x && _y == other._y;
            public override bool Equals(object obj) => obj is GridCoordinate other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(_x, _y);
        }

        private readonly struct PoseCoordinate : IEquatable<PoseCoordinate>
        {
            private readonly int _x;
            private readonly int _y;
            private readonly Vector2 _position;

            public PoseCoordinate(int x, int y, Vector2 position)
            {
                _x = x;
                _y = y;
                _position = position;
            }

            public bool Equals(PoseCoordinate other) =>
                _x == other._x && _y == other._y && _position.Equals(other._position);

            public override bool Equals(object obj) => obj is PoseCoordinate other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(_x, _y, _position);
        }

        private sealed class SymmetryCaveStroke : ICaveEditStroke
        {
            private readonly Map _map;
            private readonly ICaveEditStroke _stroke;
            private readonly SymmetrySnapshot _snapshot;

            public SymmetryCaveStroke(Map map, ICaveEditStroke stroke, SymmetrySnapshot snapshot)
            {
                _map = map;
                _stroke = stroke;
                _snapshot = snapshot;
            }

            public bool ApplyAt(int x, int y)
            {
                bool changed = false;
                ForEachCell(_map, _snapshot, x, y,
                    target => changed |= _stroke.ApplyAt(target.X, target.Y));
                return changed;
            }

            public void Commit() => _stroke.Commit();
            public void Cancel() => _stroke.Cancel();
            public void Dispose() => _stroke.Dispose();
        }

        private sealed class SymmetryDockStroke : IMapDockStroke
        {
            private readonly Map _map;
            private readonly DockFactory _dockFactory;
            private readonly IDataCatalog _dataCatalog;
            private readonly SymmetrySnapshot _snapshot;
            private readonly Dictionary<SymmetryTransform, Tile> _previousTiles =
                new Dictionary<SymmetryTransform, Tile>();
            private readonly Dictionary<GridCoordinate, int> _paintedCoordinates =
                new Dictionary<GridCoordinate, int>();
            private bool _completed;

            public SymmetryDockStroke(Map map, DockFactory dockFactory, IDataCatalog dataCatalog,
                SymmetrySnapshot snapshot, int anchorX, int anchorY)
            {
                _map = map;
                _dockFactory = dockFactory;
                _dataCatalog = dataCatalog;
                _snapshot = snapshot;
                snapshot.ForEachTransform(transform =>
                {
                    int x = snapshot.TransformCellX(anchorX, transform);
                    int y = snapshot.TransformCellY(anchorY, transform);
                    if (x >= 0 && x < map.Width && y >= 0 && y < map.Height)
                    {
                        _previousTiles[transform] = map[x, y];
                    }
                });
            }

            public bool Place(DockPaintRequest request)
            {
                ThrowIfCompleted();
                bool originalPlaced = false;
                _snapshot.ForEachTransform(transform =>
                {
                    int x = _snapshot.TransformCellX(request.X, transform);
                    int y = _snapshot.TransformCellY(request.Y, transform);
                    var coordinate = new GridCoordinate(x, y);
                    if (x < 0 || x >= _map.Width || y < 0 || y >= _map.Height)
                    {
                        return;
                    }
                    int priority = TransformPriority(transform);
                    if (_paintedCoordinates.TryGetValue(coordinate, out int existingPriority)
                        && existingPriority <= priority)
                    {
                        _previousTiles[transform] = _map[x, y];
                        return;
                    }
                    if (DockSupportResolver.GetHardBlock(_map, x, y, request.Height, request.Realm)
                        != DockHardBlock.None)
                    {
                        return;
                    }

                    _previousTiles.TryGetValue(transform, out Tile previousTile);
                    DockSupportData support;
                    EntityOrientation braceDirection;
                    if (request.AutomaticSupport)
                    {
                        support = DockSupportResolver.ResolveAutoSupport(_map, x, y, request.Height,
                            request.Realm, request.LastPillarSupport, _dataCatalog, out braceDirection);
                    }
                    else
                    {
                        support = request.Support;
                        braceDirection = transform.Transform(EntityOrientation.Up);
                        if (support != null && support.Type == DockSupportType.Brace)
                        {
                            DockSupportResolver.TryPickBraceSide(_map, x, y, request.Height,
                                request.Realm, previousTile, out braceDirection);
                        }
                    }

                    Tile targetTile = _map[x, y];
                    Dock replacedDock = targetTile.GetDock(request.Realm);
                    Dock newDock = _dockFactory.CreateDock(_map, x, y, request.Height, request.Floor,
                        support, braceDirection, request.Realm, request.AnchorLevel);
                    _map.CommandManager.AddToActionAndExecute(
                        new DockPlacementCommand(_map, newDock, replacedDock));
                    _paintedCoordinates[coordinate] = priority;
                    _previousTiles[transform] = targetTile;
                    if (transform == SymmetryTransform.Identity)
                    {
                        originalPlaced = true;
                    }
                });
                return originalPlaced;
            }

            public void Remove(int x, int y, DockRealm realm)
            {
                ThrowIfCompleted();
                ForEachCell(_map, _snapshot, x, y, target =>
                {
                    Dock dock = _map[target.X, target.Y].GetDock(realm);
                    if (dock != null)
                    {
                        _map.CommandManager.AddToActionAndExecute(new DockRemovalCommand(_map, dock));
                    }
                });
            }

            public void Commit()
            {
                if (_completed)
                {
                    return;
                }
                _map.CommandManager.FinishAction();
                _completed = true;
            }

            public void Cancel()
            {
                if (_completed)
                {
                    return;
                }
                _map.CommandManager.UndoAction();
                _completed = true;
            }

            public void Dispose() => Cancel();

            private void ThrowIfCompleted()
            {
                if (_completed)
                {
                    throw new InvalidOperationException("The dock stroke is already complete.");
                }
            }
        }

        private sealed class SymmetryHeightEdit : IMapHeightEdit
        {
            private readonly Map _map;
            private readonly ICaveHeightEdit _caveEdit;
            private readonly SymmetrySnapshot _snapshot;
            private readonly bool _cave;
            private readonly CaveHeightMode _caveMode;
            private readonly HeightEditBehavior _behavior;
            private readonly SymmetryHeightPatch _patch = new SymmetryHeightPatch();
            private readonly Dictionary<GridCoordinate, int> _originalValues =
                new Dictionary<GridCoordinate, int>();
            private bool _completed;

            public SymmetryHeightEdit(Map map, ICaveHeightEdit caveEdit, SymmetrySnapshot snapshot,
                bool cave, CaveHeightMode caveMode, HeightEditBehavior behavior)
            {
                _map = map;
                _caveEdit = caveEdit;
                _snapshot = snapshot;
                _cave = cave;
                _caveMode = caveMode;
                _behavior = behavior;
            }

            public void BeginPreview()
            {
                ThrowIfCompleted();
                _patch.Clear();
                if (_behavior != HeightEditBehavior.ReplacePreview)
                {
                    return;
                }

                if (_cave)
                {
                    foreach (KeyValuePair<GridCoordinate, int> pair in _originalValues)
                    {
                        _caveEdit.SetAt(pair.Key.X, pair.Key.Y, ToStoredValue(pair.Key, pair.Value));
                    }
                }
                else
                {
                    _map.CommandManager.UndoAction();
                }
            }

            public void SetAt(int x, int y, int value)
            {
                ThrowIfCompleted();
                _patch.Add(_snapshot, _map.Width, _map.Height, x, y, value);
            }

            public void ApplyPreview(Action<int, int> onChanged = null)
            {
                ThrowIfCompleted();
                _patch.Apply((x, y, value) =>
                {
                    var coordinate = new GridCoordinate(x, y);
                    if (!_originalValues.ContainsKey(coordinate))
                    {
                        _originalValues.Add(coordinate, GetSemanticValue(coordinate));
                    }

                    bool changed;
                    if (_cave)
                    {
                        changed = _caveEdit.SetAt(coordinate.X, coordinate.Y,
                            ToStoredValue(coordinate, value));
                    }
                    else
                    {
                        Tile tile = _map[coordinate.X, coordinate.Y];
                        changed = tile.SurfaceHeight != value;
                        tile.SurfaceHeight = value;
                    }

                    if (changed)
                    {
                        onChanged?.Invoke(coordinate.X, coordinate.Y);
                    }
                });
            }

            public void Commit()
            {
                if (_completed)
                {
                    return;
                }
                if (_cave)
                {
                    _caveEdit.Commit();
                }
                else
                {
                    _map.CommandManager.FinishAction();
                }
                _completed = true;
            }

            public void Cancel()
            {
                if (_completed)
                {
                    return;
                }
                if (_cave)
                {
                    _caveEdit.Cancel();
                }
                else
                {
                    _map.CommandManager.UndoAction();
                }
                _completed = true;
            }

            public void Dispose()
            {
                Cancel();
                _caveEdit?.Dispose();
            }

            private int GetSemanticValue(GridCoordinate coordinate)
            {
                Tile tile = _map[coordinate.X, coordinate.Y];
                if (!_cave)
                {
                    return tile.SurfaceHeight;
                }
                return _caveMode == CaveHeightMode.Floor
                    ? tile.CaveHeight
                    : tile.CaveHeight + tile.CaveSize;
            }

            private int ToStoredValue(GridCoordinate coordinate, int semanticValue)
            {
                return _caveMode == CaveHeightMode.Clearance
                    ? Math.Max(0, semanticValue - _map[coordinate.X, coordinate.Y].CaveHeight)
                    : semanticValue;
            }

            private void ThrowIfCompleted()
            {
                if (_completed)
                {
                    throw new InvalidOperationException("The height edit is already complete.");
                }
            }

        }
    }
}
