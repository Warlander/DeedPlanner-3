using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Data.Floor;
using Warlander.Deedplanner.Data.Object;
using Warlander.Deedplanner.Data.Roof;
using Warlander.Deedplanner.Data.Wall;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data
{
    public class Tile : ScriptableObject, IXMLSerializable
    {

        private int surfaceHeight = 0;
        private int caveHeight = 0;
        private int caveSize = 0;

        public Map Map { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        private bool Edge { get; set; }
        private Mesh SurfaceHeightMesh { get; set; }
        private Mesh CaveHeightMesh { get; set; }
        private Dictionary<EntityData, TileEntity> Entities { get; set; }
        private GridTile surfaceGridTile;
        private GridTile caveGridTile;

        public Ground.Ground Ground { get; private set; }
        public Cave.Cave Cave { get; private set; }

        public int SurfaceHeight {
            get => surfaceHeight;
            set {
                surfaceHeight = value;
                RefreshSurfaceMesh();
                UpdateSurfaceEntitiesPositions();
                Map.GetRelativeTile(this, -1, 0)?.RefreshSurfaceMesh();
                Map.GetRelativeTile(this, -1, 0)?.UpdateSurfaceEntitiesPositions();
                Map.GetRelativeTile(this, 0, -1)?.RefreshSurfaceMesh();
                Map.GetRelativeTile(this, 0, -1)?.UpdateSurfaceEntitiesPositions();
                Map.GetRelativeTile(this, -1, -1)?.RefreshSurfaceMesh();
                Map.GetRelativeTile(this, -1, -1)?.UpdateSurfaceEntitiesPositions();

                Map.RecalculateSurfaceHeight(X, Y);
            }
        }

        public int CaveHeight {
            get => caveHeight;
            set {
                caveHeight = value;
                RefreshCaveMesh();
                UpdateCaveEntitiesPositions();
                Map.GetRelativeTile(this, -1, 0)?.RefreshCaveMesh();
                Map.GetRelativeTile(this, -1, 0)?.UpdateCaveEntitiesPositions();
                Map.GetRelativeTile(this, 0, -1)?.RefreshCaveMesh();
                Map.GetRelativeTile(this, 0, -1)?.UpdateCaveEntitiesPositions();
                Map.GetRelativeTile(this, -1, -1)?.RefreshCaveMesh();
                Map.GetRelativeTile(this, -1, -1)?.UpdateCaveEntitiesPositions();

                Map.RecalculateCaveHeight(X, Y);
            }
        }

        public int CaveSize {
            get => caveSize;
            set {
                caveSize = value;
                RefreshCaveMesh();
                UpdateCaveEntitiesPositions();
                Map.GetRelativeTile(this, -1, 0)?.RefreshCaveMesh();
                Map.GetRelativeTile(this, -1, 0)?.UpdateCaveEntitiesPositions();
                Map.GetRelativeTile(this, 0, -1)?.RefreshCaveMesh();
                Map.GetRelativeTile(this, 0, -1)?.UpdateCaveEntitiesPositions();
                Map.GetRelativeTile(this, -1, -1)?.RefreshCaveMesh();
                Map.GetRelativeTile(this, -1, -1)?.UpdateCaveEntitiesPositions();

                Map.RecalculateCaveHeight(X, Y);
            }
        }

        public void Initialize(Map map, GridTile surfaceGridTile, GridTile caveGridTile, int x, int y, bool edge)
        {
            Map = map;
            X = x;
            Y = y;
            Edge = edge;

            SurfaceHeightMesh = InitializeHeightMesh();
            CaveHeightMesh = InitializeHeightMesh();

            Entities = new Dictionary<EntityData, TileEntity>();

            this.surfaceGridTile = surfaceGridTile;
            surfaceGridTile.Initialize(SurfaceHeightMesh, X, Y);

            this.caveGridTile = caveGridTile;
            caveGridTile.Initialize(CaveHeightMesh, X, Y);

            GameObject groundObject = new GameObject("Ground", typeof(Ground.Ground));
            groundObject.transform.localPosition = new Vector3(X * 4, 0, Y * 4);
            Ground = groundObject.GetComponent<Ground.Ground>();
            Map.AddEntityToMap(groundObject, 0);
            Ground.Initialize(this, Database.Grounds["gr"], SurfaceHeightMesh);
            if (edge)
            {
                Ground.gameObject.SetActive(false);
            }

            GameObject caveObject = new GameObject("Cave", typeof(Cave.Cave));
            caveObject.transform.localPosition = new Vector3(X * 4, 0, Y * 4);
            Cave = caveObject.GetComponent<Cave.Cave>();
            Map.AddEntityToMap(caveObject, -1);
            Cave.Initialize(this, Database.DefaultCaveData);
            if (edge)
            {
                Cave.gameObject.SetActive(false);
            }
        }

        private Mesh InitializeHeightMesh()
        {
            Vector3[] vertices = { new Vector3(0, 0, 0), new Vector3(4, 0, 0), new Vector3(0, 0, 4), new Vector3(4, 0, 4), new Vector3(2, 0, 2) };
            Vector2[] uv = { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 0.5f) };

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.subMeshCount = 4;
            mesh.SetTriangles(new int[] { 0, 2, 4 }, 0);
            mesh.SetTriangles(new int[] { 2, 3, 4 }, 1);
            mesh.SetTriangles(new int[] { 3, 1, 4 }, 2);
            mesh.SetTriangles(new int[] { 1, 0, 4 }, 3);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        public EntityType FindTypeOfEntity(TileEntity entity)
        {
            if (entity == Ground)
            {
                return EntityType.Ground;
            }
            else if (entity == Cave)
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
            if (entity == Ground)
            {
                return 0;
            }
            else if (entity == Cave)
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
            }else
            {
                return SurfaceHeight;
            }
        }

        public TileEntity GetTileContent(int level)
        {
            EntityData entityData = new EntityData(level, EntityType.Floorroof);
            TileEntity tileEntity;
            Entities.TryGetValue(entityData, out tileEntity);
            return tileEntity;
        }

        public Floor.Floor SetFloor(FloorData data, FloorOrientation orientation, int level)
        {
            EntityData entityData = new EntityData(level, EntityType.Floorroof);
            TileEntity tileEntity;
            Entities.TryGetValue(entityData, out tileEntity);
            Floor.Floor currentFloor = tileEntity as Floor.Floor;
            Roof.Roof currentRoof = tileEntity as Roof.Roof;

            bool needsChange = !tileEntity || (currentFloor && (currentFloor.Data != data || currentFloor.Orientation != orientation)) || currentRoof;
            
            if (data && needsChange)
            {
                Floor.Floor floor = CreateNewFloor(entityData, data, orientation);
                Map.CommandManager.AddToActionAndExecute(new TileEntityChangeCommand(this, entityData, tileEntity, floor));
                return floor;
            }
            else if (!data && tileEntity)
            {
                Map.CommandManager.AddToActionAndExecute(new TileEntityChangeCommand(this, entityData, tileEntity, null));
                return null;
            }
            
            return null;
        }

        private Floor.Floor CreateNewFloor(EntityData entity, FloorData data, FloorOrientation orientation)
        {
            GameObject floorObject = new GameObject("Floor " + entity.Floor, typeof(Floor.Floor));
            Floor.Floor floor = floorObject.GetComponent<Floor.Floor>();
            floor.Initialize(this, data, orientation);
            Map.AddEntityToMap(floorObject, entity.Floor);

            return floor;
        }

        public Roof.Roof SetRoof(RoofData data, int floor)
        {
            EntityData entityData = new EntityData(floor, EntityType.Floorroof);
            TileEntity tileEntity;
            Entities.TryGetValue(entityData, out tileEntity);
            Roof.Roof currentRoof = tileEntity as Roof.Roof;
            Floor.Floor currentFloor = tileEntity as Floor.Floor;

            if (!tileEntity && data)
            {
                return CreateNewRoof(entityData, data);
            }
            else if (!data && tileEntity)
            {
                DestroyEntity(entityData);
                return null;
            }
            else if ((currentRoof && (currentRoof.Data != data)) || currentFloor)
            {
                DestroyEntity(entityData);
                return CreateNewRoof(entityData, data);
            }

            return null;
        }

        private Roof.Roof CreateNewRoof(EntityData entity, RoofData data)
        {
            GameObject roofObject = new GameObject("Roof " + entity.Floor, typeof(Roof.Roof));
            Roof.Roof roof = roofObject.GetComponent<Roof.Roof>();
            roof.Initialize(this, data);

            Entities[entity] = roof;
            Map.AddEntityToMap(roofObject, entity.Floor);
            Map.RecalculateRoofs();
            UpdateSurfaceEntitiesPositions();

            return roof;
        }

        public Wall.Wall SetVerticalWall(WallData data, bool reversed, int level)
        {
            EntityData wallEntityData = new EntityData(level, EntityType.Vwall);
            TileEntity wallEntity;
            Entities.TryGetValue(wallEntityData, out wallEntity);
            Wall.Wall currentWall = wallEntity as Wall.Wall;

            EntityData fenceEntityData = new EntityData(level, EntityType.Vfence);
            TileEntity fenceEntity;
            Entities.TryGetValue(fenceEntityData, out fenceEntity);
            Wall.Wall currentFence = fenceEntity as Wall.Wall;

            if (data)
            {
                if (!data.ArchBuildable && !currentWall)
                {
                    return CreateNewVerticalWall(wallEntityData, data, reversed);
                }
                else if (data.ArchBuildable && !currentFence)
                {
                    return CreateNewVerticalWall(fenceEntityData, data, reversed);
                }
                else if (!data.ArchBuildable && (currentWall.Data != data || currentWall.Reversed != reversed))
                {
                    DestroyEntity(wallEntityData);
                    return CreateNewVerticalWall(wallEntityData, data, reversed);
                }
                else if (data.ArchBuildable && (currentFence.Data != data || currentFence.Reversed != reversed))
                {
                    DestroyEntity(fenceEntityData);
                    return CreateNewVerticalWall(fenceEntityData, data, reversed);
                }
            }
            else if (!data)
            {
                if (currentWall)
                {
                    DestroyEntity(wallEntityData);
                    return null;
                }
                if (currentFence)
                {
                    DestroyEntity(fenceEntityData);
                    return null;
                }
            }

            return null;
        }

        private Wall.Wall CreateNewVerticalWall(EntityData entity, WallData data, bool reversed)
        {
            int slopeDifference = GetHeightForFloor(entity.Floor) - Map.GetRelativeTile(this, 0, 1).GetHeightForFloor(entity.Floor);
            GameObject wallObject = new GameObject("Vertical Wall " + entity.Floor, typeof(Wall.Wall));
            Wall.Wall wall = wallObject.GetComponent<Wall.Wall>();
            wall.Initialize(this, data, reversed, entity.IsGroundFloor, slopeDifference);
            wallObject.transform.rotation = Quaternion.Euler(0, 90, 0);

            Entities[entity] = wall;
            Map.AddEntityToMap(wallObject, entity.Floor);
            UpdateSurfaceEntitiesPositions();

            return wall;
        }

        public Wall.Wall SetHorizontalWall(WallData data, bool reversed, int level)
        {
            EntityData wallEntityData = new EntityData(level, EntityType.Hwall);
            TileEntity wallEntity;
            Entities.TryGetValue(wallEntityData, out wallEntity);
            Wall.Wall currentWall = wallEntity as Wall.Wall;

            EntityData fenceEntityData = new EntityData(level, EntityType.Hfence);
            TileEntity fenceEntity;
            Entities.TryGetValue(fenceEntityData, out fenceEntity);
            Wall.Wall currentFence = fenceEntity as Wall.Wall;

            if (data)
            {
                if (!data.ArchBuildable && !currentWall)
                {
                    return CreateNewHorizontalWall(wallEntityData, data, reversed);
                }
                else if (data.ArchBuildable && !currentFence)
                {
                    return CreateNewHorizontalWall(fenceEntityData, data, reversed);
                }
                else if (!data.ArchBuildable && (currentWall.Data != data || currentWall.Reversed != reversed))
                {
                    DestroyEntity(wallEntityData);
                    return CreateNewHorizontalWall(wallEntityData, data, reversed);
                }
                else if (data.ArchBuildable && (currentFence.Data != data || currentFence.Reversed != reversed))
                {
                    DestroyEntity(fenceEntityData);
                    return CreateNewHorizontalWall(fenceEntityData, data, reversed);
                }
            }
            else if (!data)
            {
                if (currentWall)
                {
                    DestroyEntity(wallEntityData);
                    return null;
                }
                if (currentFence)
                {
                    DestroyEntity(fenceEntityData);
                    return null;
                }
            }

            return null;
        }

        private Wall.Wall CreateNewHorizontalWall(EntityData entity, WallData data, bool reversed)
        {
            int slopeDifference = GetHeightForFloor(entity.Floor) - Map.GetRelativeTile(this, 1, 0).GetHeightForFloor(entity.Floor);
            GameObject wallObject = new GameObject("Horizontal Wall " + entity.Floor, typeof(Wall.Wall));
            Wall.Wall wall = wallObject.GetComponent<Wall.Wall>();
            wall.Initialize(this, data, reversed, entity.IsGroundFloor, slopeDifference);
            wallObject.transform.rotation = Quaternion.Euler(0, 180, 0);

            Entities[entity] = wall;
            Map.AddEntityToMap(wallObject, entity.Floor);
            UpdateSurfaceEntitiesPositions();

            return wall;
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
            
            XmlElement cave = document.CreateElement("cave");
            Cave.Serialize(document, cave);
            localRoot.AppendChild(cave);

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
                        return entity.GetType() == typeof(Floor.Floor) ? "floor" : "roof";
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

        public void Deserialize(XmlElement tileElement)
        {
            surfaceHeight = (int) Convert.ToSingle(tileElement.GetAttribute("height"), CultureInfo.InvariantCulture);
            caveHeight = (int) Convert.ToSingle(tileElement.GetAttribute("caveHeight"), CultureInfo.InvariantCulture);

            foreach (XmlElement childElement in tileElement)
            {
                string tag = childElement.Name;
                switch (tag)
                {
                    case "ground":
                        Ground.Deserialize(childElement);
                        break;
                    case "level":
                        DeserializeFloor(childElement);
                        break;
                }
            }
        }

        private void DeserializeFloor(XmlElement floorElement)
        {
            int floor = Convert.ToInt32(floorElement.GetAttribute("value"));

            foreach (XmlElement childElement in floorElement)
            {
                string tag = childElement.Name;
                switch (tag)
                {
                    case "Floor":
                        DeserializeFloor(childElement, floor);
                        break;
                    case "hWall": case "vWall":
                        DeserializeWall(childElement, floor);
                        break;
                    case "roof":
                        DeserializeRoof(childElement, floor);
                        break;
                }
            }
        }

        private void DeserializeFloor(XmlElement element, int floor)
        {
            string id = element.GetAttribute("id");
            FloorData data;
            Database.Floors.TryGetValue(id, out data);
            if (!data)
            {
                Debug.LogWarning("Unable to load floor " + id);
            }

            string orientationString = element.GetAttribute("orientation");
            FloorOrientation orientation = FloorOrientation.Down;
            switch (orientationString.ToUpper())
            {
                case "UP":
                    orientation = FloorOrientation.Up;
                    break;
                case "LEFT":
                    orientation = FloorOrientation.Left;
                    break;
                case "DOWN":
                    orientation = FloorOrientation.Down;
                    break;
                case "RIGHT":
                    orientation = FloorOrientation.Right;
                    break;
            }

            EntityData entityData = new EntityData(floor, EntityType.Floorroof);
            CreateNewFloor(entityData, data, orientation);
        }

        private void DeserializeWall(XmlElement element, int floor)
        {
            string id = element.GetAttribute("id");
            WallData data;
            Database.Walls.TryGetValue(id, out data);
            if (!data)
            {
                Debug.LogWarning("Unable to load wall " + id);
            }

            EntityType entityType;
            bool horizontal = (element.Name == "hWall");
            
            if (horizontal && data.ArchBuildable)
            {
                entityType = EntityType.Hfence;
            }
            else if (horizontal)
            {
                entityType = EntityType.Hwall;
            }
            else if (!horizontal && data.ArchBuildable)
            {
                entityType = EntityType.Vfence;
            }
            else
            {
                entityType = EntityType.Vwall;
            }

            bool reversed = element.GetAttribute("reversed") == "true";
            EntityData entityData = new EntityData(floor, entityType);
            if (horizontal)
            {
                CreateNewHorizontalWall(entityData, data, reversed);
            }
            else
            {
                CreateNewVerticalWall(entityData, data, reversed);
            }
        }

        private void DeserializeRoof(XmlElement element, int floor)
        {
            string id = element.GetAttribute("id");
            RoofData data;
            Database.Roofs.TryGetValue(id, out data);
            if (!data)
            {
                Debug.LogWarning("Unable to load roof " + id);
            }

            EntityData entityData = new EntityData(floor, EntityType.Floorroof);
            CreateNewRoof(entityData, data);
        }

        public void Refresh()
        {
            RefreshSurfaceMesh();
            RefreshCaveMesh();
            UpdateSurfaceEntitiesPositions();
            UpdateCaveEntitiesPositions();
        }

        private void RefreshSurfaceMesh()
        {
            if (Edge)
            {
                return;
            }

            float h00 = SurfaceHeight * 0.1f;
            float h10 = Map.GetRelativeTile(this, 1, 0).SurfaceHeight * 0.1f;
            float h01 = Map.GetRelativeTile(this, 0, 1).SurfaceHeight * 0.1f;
            float h11 = Map.GetRelativeTile(this, 1, 1).SurfaceHeight * 0.1f;
            float hMid = (h00 + h10 + h01 + h11) / 4f;
            Vector3[] vertices = new Vector3[5];
            vertices[0] = new Vector3(0, h00, 0);
            vertices[1] = new Vector3(4, h10, 0);
            vertices[2] = new Vector3(0, h01, 4);
            vertices[3] = new Vector3(4, h11, 4);
            vertices[4] = new Vector3(2, hMid, 2);
            SurfaceHeightMesh.vertices = vertices;
            SurfaceHeightMesh.RecalculateNormals();
            SurfaceHeightMesh.RecalculateBounds();

            surfaceGridTile.Collider.sharedMesh = SurfaceHeightMesh;
            Ground.Collider.sharedMesh = SurfaceHeightMesh;
        }

        private void RefreshCaveMesh()
        {
            if (Edge)
            {
                return;
            }

            float h00 = CaveHeight * 0.1f;
            float h10 = Map.GetRelativeTile(this, 1, 0).CaveHeight * 0.1f;
            float h01 = Map.GetRelativeTile(this, 0, 1).CaveHeight * 0.1f;
            float h11 = Map.GetRelativeTile(this, 1, 1).CaveHeight * 0.1f;
            float hMid = (h00 + h10 + h01 + h11) / 4f;
            Vector3[] vertices = new Vector3[5];
            vertices[0] = new Vector3(0, h00, 0);
            vertices[1] = new Vector3(4, h10, 0);
            vertices[2] = new Vector3(0, h01, 4);
            vertices[3] = new Vector3(4, h11, 4);
            vertices[4] = new Vector3(2, hMid, 2);
            CaveHeightMesh.vertices = vertices;
            CaveHeightMesh.RecalculateNormals();
            CaveHeightMesh.RecalculateBounds();

            caveGridTile.Collider.sharedMesh = CaveHeightMesh;
        }

        private void UpdateSurfaceEntitiesPositions()
        {
            foreach (KeyValuePair<EntityData, TileEntity> pair in Entities)
            {
                EntityData data = pair.Key;
                if (data.Floor < 0)
                {
                    continue;
                }
                TileEntity tileEntity = pair.Value;
                UpdateEntityPosition(data, tileEntity);
            }
        }

        private void UpdateCaveEntitiesPositions()
        {
            foreach (KeyValuePair<EntityData, TileEntity> pair in Entities)
            {
                EntityData data = pair.Key;
                if (data.Floor >= 0)
                {
                    continue;
                }
                TileEntity tileEntity = pair.Value;
                UpdateEntityPosition(data, tileEntity);
            }
        }

        private void UpdateEntityPosition(EntityData data, TileEntity entity)
        {
            entity.transform.localPosition = new Vector3(X * 4, SurfaceHeight * 0.1f + data.Floor * 3f, Y * 4);
            if (data.Type == EntityType.Hfence || data.Type == EntityType.Hwall)
            {
                int slopeDifference = GetHeightForFloor(entity.Floor) - Map.GetRelativeTile(this, 1, 0).GetHeightForFloor(entity.Floor);
                Wall.Wall wall = (Wall.Wall) entity;
                wall.UpdateModel(slopeDifference, data.IsGroundFloor);
            }
            else if (data.Type == EntityType.Vfence || data.Type == EntityType.Vwall)
            {
                int slopeDifference = GetHeightForFloor(entity.Floor) - Map.GetRelativeTile(this, 0, 1).GetHeightForFloor(entity.Floor);
                Wall.Wall wall = (Wall.Wall) entity;
                wall.UpdateModel(slopeDifference, data.IsGroundFloor);
            }
        }

        private void DestroyEntity(EntityData key)
        {
            TileEntity entity = Entities[key];
            Entities.Remove(key);
            Destroy(entity.gameObject);
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
                    tile.UpdateSurfaceEntitiesPositions();
                }
                else
                {
                    tile.UpdateCaveEntitiesPositions();
                }
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
                
                tile.UpdateSurfaceEntitiesPositions();
                
                if (data.IsSurface)
                {
                    tile.UpdateSurfaceEntitiesPositions();
                }
                else
                {
                    tile.UpdateCaveEntitiesPositions();
                }
            }

            public void DisposeUndo()
            {
                Destroy(oldEntity.gameObject);
            }

            public void DisposeRedo()
            {
                Destroy(newEntity.gameObject);
            }
        }

    }
}
