using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Data.Bridges;
using Warlander.Deedplanner.Data.Caves;
using Warlander.Deedplanner.Data.Decorations;
using Warlander.Deedplanner.Data.Floors;
using Warlander.Deedplanner.Data.Grounds;
using Warlander.Deedplanner.Data.Roofs;
using Warlander.Deedplanner.Data.Summary;
using Warlander.Deedplanner.Data.Walls;
using Warlander.Deedplanner.Utils;
using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Data
{
    public class Tile : IXmlSerializable
    {
        private int surfaceHeight = 0;
        private int caveHeight = 0;
        private int caveSize = 0;

        private BridgePart surfaceBridgePart;
        
        public Map Map { get; }
        public int X { get; }
        public int Y { get; }
        private Dictionary<EntityData, TileEntity> Entities { get; set; }

        public Ground Ground { get; private set; }
        public Cave Cave { get; private set; }

        public int SurfaceHeight {
            get => surfaceHeight;
            set {
                if (surfaceHeight != value)
                {
                    Map.CommandManager.AddToActionAndExecute(new SurfaceHeightChangeCommand(this, surfaceHeight, value));
                }
            }
        }

        public int CaveHeight {
            get => caveHeight;
            set {
                caveHeight = value;
                // TODO: add cave mesh handling
                RefreshCaveEntities();
                Map.GetRelativeTile(this, -1, 0)?.RefreshCaveEntities();
                Map.GetRelativeTile(this, 0, -1)?.RefreshCaveEntities();
                Map.GetRelativeTile(this, -1, -1)?.RefreshCaveEntities();

                Map.RecalculateCaveHeight(X, Y);
            }
        }

        public int CaveSize {
            get => caveSize;
            set {
                caveSize = value;
                // TODO: add cave mesh handling
                RefreshCaveEntities();
                Map.GetRelativeTile(this, -1, 0)?.RefreshCaveEntities();
                Map.GetRelativeTile(this, 0, -1)?.RefreshCaveEntities();
                Map.GetRelativeTile(this, -1, -1)?.RefreshCaveEntities();

                Map.RecalculateCaveHeight(X, Y);
            }
        }

        public Tile(Map map, int x, int y)
        {
            Map = map;
            X = x;
            Y = y;

            Entities = new Dictionary<EntityData, TileEntity>();

            Ground = new Ground(this, Database.DefaultGroundData);

            // GameObject caveObject = new GameObject("Cave", typeof(Cave));
            // caveObject.transform.localPosition = new Vector3(X * 4, 0, Y * 4);
            // Cave = caveObject.GetComponent<Cave>();
            // Map.AddEntityToMap(caveObject, -1);
            // Cave.Initialize(this, Database.DefaultCaveData);
        }

        public void PasteTile(Tile otherTile)
        {
            surfaceHeight = otherTile.surfaceHeight;
            caveHeight = otherTile.caveHeight;
            caveSize = otherTile.caveSize;

            Ground.Data = otherTile.Ground.Data;
            Ground.RoadDirection = otherTile.Ground.RoadDirection;

            // Cave.Data = otherTile.Cave.Data;

            foreach (KeyValuePair<EntityData,TileEntity> pair in otherTile.Entities)
            {
                EntityData data = pair.Key;
                TileEntity entity = pair.Value;
                PasteEntity(data, entity);
            }

            RefreshSurfaceEntities();
            RefreshCaveEntities();
        }

        private void PasteEntity(EntityData data, TileEntity entity)
        {
            entity.Tile = this;
            Entities[data] = entity;
            Map.AddEntityToMap(entity.gameObject, data.Floor);
        }

        public bool ContainsEntity(TileEntity entity)
        {
            if (entity == Cave)
            {
                return true;
            }

            foreach (KeyValuePair<EntityData, TileEntity> pair in Entities)
            {
                EntityData key = pair.Key;
                TileEntity checkedEntity = pair.Value;
                if (entity == checkedEntity)
                {
                    return true;
                }
            }

            return false;
        }

        public EntityType FindTypeOfEntity(TileEntity entity)
        {
            if (entity == Cave)
            {
                return EntityType.Cave;
            }

            foreach (KeyValuePair<EntityData, TileEntity> pair in Entities)
            {
                EntityData key = pair.Key;
                TileEntity checkedEntity = pair.Value;
                if (entity == checkedEntity)
                {
                    return key.Type;
                }
            }

            throw new ArgumentException("Entity is not part of the tile");
        }

        public int FindFloorOfEntity(TileEntity entity)
        {
            if (entity == Cave)
            {
                return -1;
            }

            foreach (KeyValuePair<EntityData, TileEntity> pair in Entities)
            {
                EntityData key = pair.Key;
                TileEntity checkedEntity = pair.Value;
                if (entity == checkedEntity)
                {
                    return key.Floor;
                }
            }

            throw new ArgumentException("Entity is not part of the tile");
        }

        public int GetHeightForFloor(int floor)
        {
            if (floor < 0)
            {
                return caveHeight;
            }
            else
            {
                return SurfaceHeight;
            }
        }

        public Materials CalculateFloorMaterials(int floor, TilePart tilePart)
        {
            Materials tileMaterials = new Materials();

            foreach (KeyValuePair<EntityData,TileEntity> pair in Entities)
            {
                EntityData key = pair.Key;
                if (key.Floor != floor)
                {
                    continue;
                }

                bool addAlways = tilePart == TilePart.Everything;
                bool addHorizontalWalls = tilePart == TilePart.HorizontalWallOnly && key.Type.IsHorizontalTileBorder();
                bool addVerticalWalls = tilePart == TilePart.VerticalWallOnly && key.Type.IsVerticalTileBorder();

                if (addAlways || addHorizontalWalls || addVerticalWalls)
                {
                    tileMaterials.Add(pair.Value.Materials);
                }
            }

            return tileMaterials;
        }

        public Materials CalculateTileMaterials(TilePart tilePart)
        {
            Materials tileMaterials = new Materials();

            foreach (KeyValuePair<EntityData,TileEntity> pair in Entities)
            {
                EntityData key = pair.Key;

                bool addAlways = tilePart == TilePart.Everything;
                bool addHorizontalWalls = tilePart == TilePart.HorizontalWallOnly && key.Type.IsHorizontalTileBorder();
                bool addVerticalWalls = tilePart == TilePart.VerticalWallOnly && key.Type.IsVerticalTileBorder();

                if (addAlways || addHorizontalWalls || addVerticalWalls)
                {
                    tileMaterials.Add(pair.Value.Materials);
                }
            }

            return tileMaterials;
        }

        public TileEntity GetTileContent(int level)
        {
            EntityData entityData = new EntityData(level, EntityType.Floorroof);
            TileEntity tileEntity;
            Entities.TryGetValue(entityData, out tileEntity);
            return tileEntity;
        }

        public Floor SetFloor(FloorData data, EntityOrientation orientation, int level)
        {
            EntityData entityData = new EntityData(level, EntityType.Floorroof);
            TileEntity tileEntity;
            Entities.TryGetValue(entityData, out tileEntity);
            Floor currentFloor = tileEntity as Floor;
            Roof currentRoof = tileEntity as Roof;

            bool needsChange = !tileEntity || (currentFloor && (currentFloor.Data != data || currentFloor.Orientation != orientation)) || currentRoof;

            if (data != null && needsChange)
            {
                Floor floor = CreateNewFloor(entityData, data, orientation);
                Map.CommandManager.AddToActionAndExecute(new TileEntityChangeCommand(this, entityData, tileEntity, floor));
                return floor;
            }
            if (data == null && tileEntity)
            {
                Map.CommandManager.AddToActionAndExecute(new TileEntityChangeCommand(this, entityData, tileEntity, null));
                return null;
            }

            return null;
        }

        private Floor CreateNewFloor(EntityData entity, FloorData data, EntityOrientation orientation)
        {
            GameObject floorObject = new GameObject("Floor " + data.Name, typeof(Floor));
            Floor floor = floorObject.GetComponent<Floor>();
            floor.Initialize(this, data, orientation);
            Map.AddEntityToMap(floorObject, entity.Floor);

            return floor;
        }

        public Roof SetRoof(RoofData data, int floor)
        {
            EntityData entityData = new EntityData(floor, EntityType.Floorroof);
            TileEntity tileEntity;
            Entities.TryGetValue(entityData, out tileEntity);
            Roof currentRoof = tileEntity as Roof;
            Floor currentFloor = tileEntity as Floor;

            bool needsChange = !tileEntity || (currentRoof && (currentRoof.Data != data)) || currentFloor;

            if (data != null && needsChange)
            {
                Roof roof = CreateNewRoof(entityData, data);
                Map.CommandManager.AddToActionAndExecute(new TileEntityChangeCommand(this, entityData, tileEntity, roof));
                return roof;
            }
            if (data == null && tileEntity)
            {
                Map.CommandManager.AddToActionAndExecute(new TileEntityChangeCommand(this, entityData, tileEntity, null));
                return null;
            }

            return null;
        }

        private Roof CreateNewRoof(EntityData entity, RoofData data)
        {
            GameObject roofObject = new GameObject("Roof " + data.Name, typeof(Roof));
            Roof roof = roofObject.GetComponent<Roof>();
            roof.Initialize(this, data);

            Entities[entity] = roof;
            Map.AddEntityToMap(roofObject, entity.Floor);
            RefreshSurfaceEntities();

            return roof;
        }

        public Wall GetVerticalHouseWall(int level)
        {
            Wall wall = GetVerticalWall(level);
            if (!wall)
            {
                return null;
            }

            if (!wall.Data.HouseWall)
            {
                return null;
            }

            return wall;
        }

        public Wall GetVerticalWall(int level)
        {
            EntityData wallEntityData = new EntityData(level, EntityType.Vwall);
            TileEntity wallEntity;
            Entities.TryGetValue(wallEntityData, out wallEntity);
            Wall currentWall = wallEntity as Wall;

            return currentWall;
        }

        public Wall GetVerticalFence(int level)
        {
            EntityData fenceEntityData = new EntityData(level, EntityType.Vfence);
            TileEntity fenceEntity;
            Entities.TryGetValue(fenceEntityData, out fenceEntity);
            Wall currentFence = fenceEntity as Wall;

            return currentFence;
        }

        public Wall GetVerticalWallOrFence(int level)
        {
            Wall wall = GetVerticalWall(level);
            if (wall)
            {
                return wall;
            }

            return GetVerticalFence(level);
        }

        public Wall SetVerticalWall(WallData data, bool reversed, int level)
        {
            EntityData wallEntityData = new EntityData(level, EntityType.Vwall);
            TileEntity wallEntity;
            Entities.TryGetValue(wallEntityData, out wallEntity);
            Wall currentWall = wallEntity as Wall;

            EntityData fenceEntityData = new EntityData(level, EntityType.Vfence);
            TileEntity fenceEntity;
            Entities.TryGetValue(fenceEntityData, out fenceEntity);
            Wall currentFence = fenceEntity as Wall;

            if (data != null)
            {
                bool wallNeedsChange = !data.ArchBuildable && (!currentWall || currentWall.Data != data || currentWall.Reversed != reversed);
                bool fenceNeedsChange = data.ArchBuildable && (!currentFence || currentFence.Data != data || currentFence.Reversed != reversed);
                bool wallNeedsRemoval = (wallNeedsChange && !data.ArchBuildable && currentWall) || (data.ArchBuildable && currentWall && !currentWall.Data.Arch);

                if (wallNeedsChange)
                {
                    Wall wall = CreateNewVerticalWall(wallEntityData, data, reversed);
                    Map.CommandManager.AddToActionAndExecute(new TileEntityChangeCommand(this, wallEntityData, currentWall, wall));
                    return wall;
                }
                if (fenceNeedsChange)
                {
                    Wall fence = CreateNewVerticalWall(fenceEntityData, data, reversed);
                    Map.CommandManager.AddToActionAndExecute(new TileEntityChangeCommand(this, fenceEntityData, currentFence, fence));
                    return fence;
                }

                if (wallNeedsRemoval)
                {
                    Map.CommandManager.AddToActionAndExecute(new TileEntityChangeCommand(this, wallEntityData, currentWall, null));
                    return null;
                }

            }
            else
            {
                if (currentWall)
                {
                    Map.CommandManager.AddToActionAndExecute(new TileEntityChangeCommand(this, wallEntityData, currentWall, null));
                    return null;
                }
                if (currentFence)
                {
                    Map.CommandManager.AddToActionAndExecute(new TileEntityChangeCommand(this, fenceEntityData, currentFence, null));
                    return null;
                }
            }

            return null;
        }

        private Wall CreateNewVerticalWall(EntityData entity, WallData data, bool reversed)
        {
            int slopeDifference = GetHeightForFloor(entity.Floor) - Map.GetRelativeTile(this, 0, 1).GetHeightForFloor(entity.Floor);
            GameObject wallObject = new GameObject("Vertical Wall " + data.Name, typeof(Wall));
            Wall wall = wallObject.GetComponent<Wall>();
            wall.Initialize(this, data, reversed, slopeDifference);
            wallObject.transform.rotation = Quaternion.Euler(0, 90, 0);

            Entities[entity] = wall;
            Map.AddEntityToMap(wallObject, entity.Floor);
            RefreshSurfaceEntities();

            return wall;
        }

        public Wall GetHorizontalHouseWall(int level)
        {
            Wall wall = GetHorizontalWall(level);
            if (!wall)
            {
                return null;
            }

            if (!wall.Data.HouseWall)
            {
                return null;
            }

            return wall;
        }

        public Wall GetHorizontalWall(int level)
        {
            EntityData wallEntityData = new EntityData(level, EntityType.Hwall);
            TileEntity wallEntity;
            Entities.TryGetValue(wallEntityData, out wallEntity);
            Wall currentWall = wallEntity as Wall;

            return currentWall;
        }

        public Wall GetHorizontalFence(int level)
        {
            EntityData fenceEntityData = new EntityData(level, EntityType.Hfence);
            TileEntity fenceEntity;
            Entities.TryGetValue(fenceEntityData, out fenceEntity);
            Wall currentFence = fenceEntity as Wall;

            return currentFence;
        }

        public Wall GetHorizontalWallOrFence(int level)
        {
            Wall wall = GetHorizontalWall(level);
            if (wall)
            {
                return wall;
            }

            return GetHorizontalFence(level);
        }

        public Wall SetHorizontalWall(WallData data, bool reversed, int level)
        {
            EntityData wallEntityData = new EntityData(level, EntityType.Hwall);
            TileEntity wallEntity;
            Entities.TryGetValue(wallEntityData, out wallEntity);
            Wall currentWall = wallEntity as Wall;

            EntityData fenceEntityData = new EntityData(level, EntityType.Hfence);
            TileEntity fenceEntity;
            Entities.TryGetValue(fenceEntityData, out fenceEntity);
            Wall currentFence = fenceEntity as Wall;

            if (data != null)
            {
                bool wallNeedsChange = !data.ArchBuildable && (!currentWall || currentWall.Data != data || currentWall.Reversed != reversed);
                bool fenceNeedsChange = data.ArchBuildable && (!currentFence || currentFence.Data != data || currentFence.Reversed != reversed);
                bool wallNeedsRemoval = (wallNeedsChange && !data.ArchBuildable && currentWall) || (data.ArchBuildable && currentWall && !currentWall.Data.Arch);

                if (wallNeedsChange)
                {
                    Wall wall = CreateNewHorizontalWall(wallEntityData, data, reversed);
                    Map.CommandManager.AddToActionAndExecute(new TileEntityChangeCommand(this, wallEntityData, currentWall, wall));
                    return wall;
                }
                if (fenceNeedsChange)
                {
                    Wall fence = CreateNewHorizontalWall(fenceEntityData, data, reversed);
                    Map.CommandManager.AddToActionAndExecute(new TileEntityChangeCommand(this, fenceEntityData, currentFence, fence));
                    return fence;
                }

                if (wallNeedsRemoval)
                {
                    Map.CommandManager.AddToActionAndExecute(new TileEntityChangeCommand(this, wallEntityData, currentWall, null));
                    return null;
                }
            }
            else
            {
                if (currentWall)
                {
                    Map.CommandManager.AddToActionAndExecute(new TileEntityChangeCommand(this, wallEntityData, currentWall, null));
                    return null;
                }
                if (currentFence)
                {
                    Map.CommandManager.AddToActionAndExecute(new TileEntityChangeCommand(this, fenceEntityData, currentFence, null));
                    return null;
                }
            }

            return null;
        }

        private Wall CreateNewHorizontalWall(EntityData entity, WallData data, bool reversed)
        {
            int slopeDifference = GetHeightForFloor(entity.Floor) - Map.GetRelativeTile(this, 1, 0).GetHeightForFloor(entity.Floor);
            GameObject wallObject = new GameObject("Horizontal Wall " + data.Name, typeof(Wall));
            Wall wall = wallObject.GetComponent<Wall>();
            wall.Initialize(this, data, reversed, slopeDifference);
            wallObject.transform.rotation = Quaternion.Euler(0, 180, 0);

            Entities[entity] = wall;
            Map.AddEntityToMap(wallObject, entity.Floor);
            RefreshSurfaceEntities();

            return wall;
        }

        /// <summary>
        /// Returns decoration on tile center or null if it doesn't exist
        /// </summary>
        public Decoration GetCentralDecoration()
        {
            foreach (TileEntity tileEntity in Entities.Values)
            {
                if (tileEntity is Decoration decoration && decoration.Data.CenterOnly)
                {
                    return decoration;
                }
            }

            return null;
        }

        public IEnumerable<Decoration> GetDecorations()
        {
            List<Decoration> decorations = new List<Decoration>();
            foreach (TileEntity tileEntity in Entities.Values)
            {
                if (tileEntity is Decoration decoration)
                {
                    decorations.Add(decoration);
                }
            }

            return decorations;
        }

        public Decoration SetDecoration(DecorationData data, Vector2 position, float rotation, int floor, bool floatOnWater = false)
        {
            if (position.x < 0 || position.x >= 4 || position.y < 0 || position.y >= 4)
            {
                Debug.LogWarning("Attempted placing decoration at X: " + position.x + ", Y: " + position.y);
                return null;
            }

            FreeformEntityData decorationEntityData = new FreeformEntityData(floor, EntityType.Object, position.x, position.y, floatOnWater);
            TileEntity decorationEntity;
            Entities.TryGetValue(decorationEntityData, out decorationEntity);
            Decoration currentDecoration = decorationEntity as Decoration;
            bool needsChange = !currentDecoration || currentDecoration.Data != data ||
                               currentDecoration.Position != position || Math.Abs(currentDecoration.Rotation - rotation) > float.Epsilon;

            if (data != null && needsChange)
            {
                Decoration decoration = CreateNewDecoration(decorationEntityData, data, position, rotation);
                Map.CommandManager.AddToActionAndExecute(new TileEntityChangeCommand(this, decorationEntityData, currentDecoration, decoration));
                return decoration;
            }

            if (data == null && decorationEntity)
            {
                Map.CommandManager.AddToActionAndExecute(new TileEntityChangeCommand(this, decorationEntityData, currentDecoration, null));
                return null;
            }

            return null;
        }

        private Decoration CreateNewDecoration(FreeformEntityData entity, DecorationData data, Vector2 position, float rotation)
        {
            GameObject decorationObject = new GameObject("Decoration " + data.Name, typeof(Decoration));
            Decoration decoration = decorationObject.GetComponent<Decoration>();
            decoration.Initialize(this, data, position, rotation);
            Entities[entity] = decoration;
            Map.AddEntityToMap(decorationObject, entity.Floor);
            RefreshSurfaceEntities();

            return decoration;
        }

        public void RegisterBridgePart(BridgePart bridgePart)
        {
            surfaceBridgePart = bridgePart;
            bridgePart.transform.SetParent(Map.transform);
        }

        public void Serialize(XmlDocument document, XmlElement localRoot)
        {
            localRoot.SetAttribute("x", X.ToString());
            localRoot.SetAttribute("y", Y.ToString());
            localRoot.SetAttribute("height", SurfaceHeight.ToString());
            localRoot.SetAttribute("caveHeight", CaveHeight.ToString());
            localRoot.SetAttribute("caveSize", CaveSize.ToString());

            XmlElement ground = document.CreateElement("ground");
            Ground.Serialize(document, ground);
            localRoot.AppendChild(ground);

            // XmlElement cave = document.CreateElement("cave");
            // Cave.Serialize(document, cave);
            // localRoot.AppendChild(cave);

            Dictionary<int, XmlElement> levels = new Dictionary<int, XmlElement>();
            foreach (KeyValuePair<EntityData, TileEntity> e in Entities)
            {
                EntityData key = e.Key;
                TileEntity entity = e.Value;
                int floor = key.Floor;

                XmlElement level;
                levels.TryGetValue(floor, out level);
                if (level == null)
                {
                    level = document.CreateElement("level");
                    level.SetAttribute("value", key.Floor.ToString());
                    levels[key.Floor] = level;
                    localRoot.AppendChild(level);
                }

                string elementName = GetEntitySerializedName(key, entity);
                XmlElement element = document.CreateElement(elementName);
                key.Serialize(document, element);
                entity.Serialize(document, element);
                level.AppendChild(element);
            }
        }

        private string GetEntitySerializedName(EntityData key, TileEntity entity)
        {
            switch (key.Type)
                {
                    case EntityType.Floorroof:
                        return entity.GetType() == typeof(Floor) ? "floor" : "roof";
                    case EntityType.Hwall:
                    case EntityType.Hfence:
                        return "hWall";
                    case EntityType.Vwall:
                    case EntityType.Vfence:
                        return "vWall";
                    case EntityType.Hborder:
                        return "hBorder";
                    case EntityType.Vborder:
                        return "vBorder";
                    case EntityType.Object:
                        return "object";
                    case EntityType.Label:
                        return "label";
                    default:
                        throw new ArgumentException("Invalid entity type for serialization: " + key.Type);
                }
        }

        public void DeserializeHeightmap(XmlElement tileElement)
        {
            SurfaceHeight = (int) Convert.ToSingle(tileElement.GetAttribute("height"), CultureInfo.InvariantCulture);
            if (tileElement.HasAttribute("caveHeight"))
            {
                CaveHeight = (int) Convert.ToSingle(tileElement.GetAttribute("caveHeight"), CultureInfo.InvariantCulture);
            }
        }

        public void DeserializeEntities(XmlElement tileElement)
        {
            foreach (XmlElement childElement in tileElement)
            {
                string tag = childElement.Name;
                switch (tag)
                {
                    case "ground":
                        Ground.Deserialize(childElement);
                        break;
                    case "level":
                        DeserializeLevel(childElement);
                        break;
                }
            }
        }

        private void DeserializeLevel(XmlElement floorElement)
        {
            int floor = Convert.ToInt32(floorElement.GetAttribute("value"));

            foreach (XmlElement childElement in floorElement)
            {
                string tag = childElement.Name.ToLowerInvariant();
                switch (tag)
                {
                    case "floor":
                        DeserializeFloor(childElement, floor);
                        break;
                    case "hwall": case "vwall":
                        DeserializeWall(childElement, floor);
                        break;
                    case "roof":
                        DeserializeRoof(childElement, floor);
                        break;
                    case "object":
                        DeserializeDecoration(childElement, floor);
                        break;
                }
            }
        }

        private void DeserializeFloor(XmlElement element, int floor)
        {
            string id = element.GetAttribute("id");
            FloorData data;
            Database.Floors.TryGetValue(id, out data);
            if (data == null)
            {
                Debug.LogWarning("Unable to load floor " + id);
                return;
            }

            string orientationString = element.GetAttribute("orientation");
            EntityOrientation orientation = EntityOrientation.Down;
            switch (orientationString.ToUpperInvariant())
            {
                case "UP":
                    orientation = EntityOrientation.Up;
                    break;
                case "LEFT":
                    orientation = EntityOrientation.Left;
                    break;
                case "DOWN":
                    orientation = EntityOrientation.Down;
                    break;
                case "RIGHT":
                    orientation = EntityOrientation.Right;
                    break;
            }

            SetFloor(data, orientation, floor);
        }

        private void DeserializeWall(XmlElement element, int floor)
        {
            string id = element.GetAttribute("id");
            WallData data;
            Database.Walls.TryGetValue(id, out data);
            if (data == null)
            {
                Debug.LogWarning("Unable to load wall " + id);
                return;
            }

            bool horizontal = (element.Name.Equals("hWall", StringComparison.OrdinalIgnoreCase));
            bool reversed = element.GetAttribute("reversed").Equals("true", StringComparison.OrdinalIgnoreCase);
            if (data.Arch && !Map.OriginalExporter.Contains("DeedPlanner 2") && Map.OriginalExporterVersion != null && Map.OriginalExporterVersion.CompareTo(new Version("3.1.2")) < 0)
            {
                reversed = !reversed;
            }

            if (horizontal)
            {
                SetHorizontalWall(data, reversed, floor);
            }
            else
            {
                SetVerticalWall(data, reversed, floor);
            }
        }

        private void DeserializeRoof(XmlElement element, int floor)
        {
            string id = element.GetAttribute("id");
            RoofData data;
            Database.Roofs.TryGetValue(id, out data);
            if (data == null)
            {
                Debug.LogWarning("Unable to load roof " + id);
                return;
            }

            SetRoof(data, floor);
        }

        public void DeserializeDecoration(XmlElement element, int floor)
        {
            string id = element.GetAttribute("id");
            string positionString = element.GetAttribute("position");
            string rotationString = element.GetAttribute("rotation");
            string xString = element.GetAttribute("x");
            string yString = element.GetAttribute("y");

            DecorationData data;
            Database.Decorations.TryGetValue(id, out data);
            if (data == null)
            {
                Debug.LogWarning("Unable to load decoration " + id);
                return;
            }

            Vector2 position;
            if (string.IsNullOrEmpty(xString) || string.IsNullOrEmpty(yString))
            {
                position = DecorationPositionUtils.ParseDecorationPositionEnum(positionString);
            }
            else
            {
                float x = float.Parse(xString, CultureInfo.InvariantCulture);
                float y = float.Parse(yString, CultureInfo.InvariantCulture);
                position = new Vector2(x, y);
            }

            float rotation = float.Parse(rotationString, CultureInfo.InvariantCulture);
            if (Map.OriginalExporter.Contains("DeedPlanner 2"))
            {
                rotation = -rotation + 180 * Mathf.Deg2Rad;
            }

            SetDecoration(data, position, rotation, floor, data.Floating);
        }

        public void Refresh()
        {
            RefreshSurfaceEntities();
            RefreshCaveEntities();
        }

        private void RefreshSurfaceEntities()
        {
            foreach (KeyValuePair<EntityData, TileEntity> pair in Entities)
            {
                EntityData data = pair.Key;
                if (data.Floor < 0)
                {
                    continue;
                }
                TileEntity tileEntity = pair.Value;
                RefreshEntity(data, tileEntity);
            }
        }

        private void RefreshCaveEntities()
        {
            foreach (KeyValuePair<EntityData, TileEntity> pair in Entities)
            {
                EntityData data = pair.Key;
                if (data.Floor >= 0)
                {
                    continue;
                }
                TileEntity tileEntity = pair.Value;
                RefreshEntity(data, tileEntity);
            }
        }

        private void RefreshEntity(EntityData data, TileEntity entity)
        {
            data.Apply(this, entity.transform);

            if (data.Type == EntityType.Hfence || data.Type == EntityType.Hwall)
            {
                int slopeDifference = GetHeightForFloor(entity.Floor) - Map.GetRelativeTile(this, 1, 0).GetHeightForFloor(entity.Floor);
                Wall wall = (Wall) entity;
                wall.UpdateModel(slopeDifference, data.IsGroundFloor);
            }
            else if (data.Type == EntityType.Vfence || data.Type == EntityType.Vwall)
            {
                int slopeDifference = GetHeightForFloor(entity.Floor) - Map.GetRelativeTile(this, 0, 1).GetHeightForFloor(entity.Floor);
                Wall wall = (Wall) entity;
                wall.UpdateModel(slopeDifference, data.IsGroundFloor);
            }

            if (entity is Decoration decoration)
            {
                CheckDecorationRendering(decoration);
            }
        }

        private void CheckDecorationRendering(Decoration decoration)
        {
            bool renderDecorations = Map.RenderDecorations;
            bool renderTrees = !decoration.Data.Tree || Map.RenderTrees;
            bool renderBushes = !decoration.Data.Bush || Map.RenderBushes;
            bool renderShips = !decoration.Data.Floating || Map.RenderShips;

            bool shouldRender = renderDecorations && renderTrees && renderBushes && renderShips;

            decoration.gameObject.SetActive(shouldRender);
        }

        private class TileEntityChangeCommand : IReversibleCommand
        {
            private readonly Tile tile;
            private readonly EntityData data;

            private readonly TileEntity oldEntity;
            private readonly TileEntity newEntity;

            public TileEntityChangeCommand(Tile tile, EntityData data, TileEntity oldEntity, TileEntity newEntity)
            {
                this.tile = tile;
                this.data = data;
                this.oldEntity = oldEntity;
                this.newEntity = newEntity;
            }

            public void Execute()
            {
                tile.Entities.Remove(data);
                if (newEntity)
                {
                    tile.Entities[data] = newEntity;
                }

                if (oldEntity)
                {
                    oldEntity.gameObject.SetActive(false);
                }

                if (newEntity)
                {
                    newEntity.gameObject.SetActive(true);
                }

                if (data.IsSurface)
                {
                    tile.RefreshSurfaceEntities();
                }
                else
                {
                    tile.RefreshCaveEntities();
                }

                if (data.Type == EntityType.Floorroof)
                {
                    tile.Map.RecalculateRoofs();
                }

                UpdateEntityRendering(newEntity);
            }

            public void Undo()
            {
                tile.Entities.Remove(data);
                if (oldEntity)
                {
                    tile.Entities[data] = oldEntity;
                }

                if (oldEntity)
                {
                    oldEntity.gameObject.SetActive(true);
                }

                if (newEntity)
                {
                    newEntity.gameObject.SetActive(false);
                }

                tile.RefreshSurfaceEntities();

                if (data.IsSurface)
                {
                    tile.RefreshSurfaceEntities();
                }
                else
                {
                    tile.RefreshCaveEntities();
                }

                if (data.Type == EntityType.Floorroof)
                {
                    tile.Map.RecalculateRoofs();
                }

                UpdateEntityRendering(oldEntity);
            }

            private void UpdateEntityRendering(TileEntity entity)
            {
                if (!entity)
                {
                    return;
                }

                int renderedFloor = tile.Map.RenderedFloor;
                bool renderEntireMap = tile.Map.RenderEntireMap;

                bool underground = renderedFloor < 0;
                int absoluteFloor = underground ? -renderedFloor + 1 : renderedFloor;
                int relativeFloor = entity.Floor - absoluteFloor;
                bool renderFloor = renderEntireMap || (relativeFloor <= 0 && relativeFloor > -3);

                if (renderFloor)
                {
                    float opacity = renderEntireMap ? 1f : tile.Map.GetRelativeFloorOpacity(relativeFloor);
                    MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                    propertyBlock.SetColor(ShaderPropertyIds.Color, new Color(opacity, opacity, opacity));
                    Renderer[] renderers = entity.GetComponentsInChildren<Renderer>();
                    foreach (Renderer renderer in renderers)
                    {
                        renderer.SetPropertyBlock(propertyBlock);
                    }
                }
            }

            public void DisposeUndo()
            {
                if (oldEntity)
                {
                    Object.Destroy(oldEntity.gameObject);
                }

            }

            public void DisposeRedo()
            {
                if (newEntity)
                {
                    Object.Destroy(newEntity.gameObject);
                }
            }
        }

        private class SurfaceHeightChangeCommand : IReversibleCommand
        {
            private readonly Tile tile;

            private readonly int oldHeight;
            private readonly int newHeight;

            public SurfaceHeightChangeCommand(Tile tile, int oldHeight, int newHeight)
            {
                this.tile = tile;
                this.oldHeight = oldHeight;
                this.newHeight = newHeight;
            }

            public void Execute()
            {
                tile.surfaceHeight = newHeight;
                Refresh();
            }

            public void Undo()
            {
                tile.surfaceHeight = oldHeight;
                Refresh();
            }

            private void Refresh()
            {
                tile.Map.Ground.SetSlope(tile.X, tile.Y, tile.surfaceHeight);
                tile.RefreshSurfaceEntities();
                tile.Map.GetRelativeTile(tile, -1, 0)?.RefreshSurfaceEntities();
                tile.Map.GetRelativeTile(tile, 0, -1)?.RefreshSurfaceEntities();
                tile.Map.GetRelativeTile(tile, -1, -1)?.RefreshSurfaceEntities();

                tile.Map.RecalculateSurfaceHeight(tile.X, tile.Y);
            }

            public void DisposeUndo()
            {
                // no operation needed
            }

            public void DisposeRedo()
            {
                // no operation needed
            }
        }
    }
}
