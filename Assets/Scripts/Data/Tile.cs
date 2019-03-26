using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data
{
    public class Tile : ScriptableObject, IXMLSerializable
    {

        private int surfaceHeight = 0;
        private int caveHeight = 0;

        public Map Map { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public bool Edge { get; private set; }
        protected Mesh SurfaceHeightMesh { get; private set; }
        protected Mesh CaveHeightMesh { get; private set; }
        protected Dictionary<EntityData, TileEntity> Entities { get; private set; }
        private GridTile surfaceGridTile;
        private GridTile caveGridTile;

        public Ground Ground { get; private set; }
        public Cave Cave { get; private set; }

        public int SurfaceHeight {
            get {
                return surfaceHeight;
            }
            set {
                surfaceHeight = value;
                RefreshSurfaceMesh();
                UpdateSurfaceEntitiesPositions();
                Map.getRelativeTile(this, -1, 0)?.RefreshSurfaceMesh();
                Map.getRelativeTile(this, -1, 0)?.UpdateSurfaceEntitiesPositions();
                Map.getRelativeTile(this, 0, -1)?.RefreshSurfaceMesh();
                Map.getRelativeTile(this, 0, -1)?.UpdateSurfaceEntitiesPositions();
                Map.getRelativeTile(this, -1, -1)?.RefreshSurfaceMesh();
                Map.getRelativeTile(this, -1, -1)?.UpdateSurfaceEntitiesPositions();

                Map.RecalculateHeights();
            }
        }

        public int CaveHeight {
            get {
                return caveHeight;
            }
            set {
                caveHeight = value;
                RefreshCaveMesh();
                UpdateCaveEntitiesPositions();
                Map.getRelativeTile(this, -1, 0)?.RefreshCaveMesh();
                Map.getRelativeTile(this, -1, 0)?.UpdateCaveEntitiesPositions();
                Map.getRelativeTile(this, 0, -1)?.RefreshCaveMesh();
                Map.getRelativeTile(this, 0, -1)?.UpdateCaveEntitiesPositions();
                Map.getRelativeTile(this, -1, -1)?.RefreshCaveMesh();
                Map.getRelativeTile(this, -1, -1)?.UpdateCaveEntitiesPositions();

                Map.RecalculateHeights();
            }
        }

        public virtual void Initialize(Map map, GridTile surfaceGridTile, GridTile caveGridTile, int x, int y, bool edge)
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

            GameObject groundObject = new GameObject("Ground", typeof(Ground));
            groundObject.transform.localPosition = new Vector3(X * 4, 0, Y * 4);
            Ground = groundObject.GetComponent<Ground>();
            Map.AddEntityToMap(groundObject, 0);
            Ground.Initialize(this, Database.Grounds["gr"], SurfaceHeightMesh);

            GameObject caveObject = new GameObject("Cave", typeof(Cave));
            caveObject.transform.localPosition = new Vector3(X * 4, 0, Y * 4);
            Cave = caveObject.GetComponent<Cave>();
            Map.AddEntityToMap(caveObject, -1);
            Cave.Initialize(Database.DefaultCaveData);
        }

        private Mesh InitializeHeightMesh()
        {
            Vector3[] vertices = new Vector3[] { new Vector3(0, 0, 0), new Vector3(4, 0, 0), new Vector3(0, 0, 4), new Vector3(4, 0, 4), new Vector3(2, 0, 2) };
            Vector2[] uv = new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 0.5f) };

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

        public Floor SetFloor(FloorData data, EntityOrientation orientation, int level)
        {
            EntityData entityData = new EntityData(level, EntityType.FLOORROOF);
            TileEntity tileEntity;
            Entities.TryGetValue(entityData, out tileEntity);
            Floor currentFloor = tileEntity as Floor;
            if (!currentFloor && data)
            {
                return CreateNewFloor(entityData, data, orientation, level);
            }
            else if (!data && currentFloor)
            {
                Destroy(currentFloor.gameObject);
                return null;
            }
            else if (currentFloor && (currentFloor.Data != data || currentFloor.Orientation != orientation))
            {
                Destroy(currentFloor.gameObject);
                return CreateNewFloor(entityData, data, orientation, level);
            }
            // TODO: add behaviour if roof is there

            return null;
        }

        private Floor CreateNewFloor(EntityData entity, FloorData data, EntityOrientation orientation, int level)
        {
            GameObject floorObject = new GameObject("Floor " + level, typeof(Floor));
            Floor floor = floorObject.GetComponent<Floor>();
            floor.Initialize(this, data, orientation);

            Entities[entity] = floor;
            Map.AddEntityToMap(floorObject, level);
            UpdateSurfaceEntitiesPositions();

            return floor;
        }

        public Wall SetHorizontalWall(WallData data, bool reversed, int level)
        {
            EntityType entityType = data.HouseWall ? EntityType.HWALL : EntityType.HFENCE;
            EntityData entityData = new EntityData(level, entityType);
            TileEntity tileEntity;
            Entities.TryGetValue(entityData, out tileEntity);
            Wall currentWall = tileEntity as Wall;
            if (!currentWall && data)
            {
                return CreateNewHorizontalWall(entityData, data, reversed, level);
            }
            else if (!data && currentWall)
            {
                Destroy(currentWall.gameObject);
                return null;
            }
            else if (currentWall && (currentWall.Data != data || currentWall.Reversed != reversed))
            {
                Destroy(currentWall.gameObject);
                return CreateNewHorizontalWall(entityData, data, reversed, level);
            }
            // TODO: add fences in walls

            return null;
        }

        private Wall CreateNewHorizontalWall(EntityData entity, WallData data, bool reversed, int level)
        {
            GameObject wallObject = new GameObject("Horizontal Wall " + level, typeof(Wall));
            Wall wall = wallObject.GetComponent<Wall>();
            wall.Initialize(this, data, reversed, (level == 0 || level == -1));

            Entities[entity] = wall;
            Map.AddEntityToMap(wallObject, level);
            UpdateSurfaceEntitiesPositions();

            return wall;
        }

        public void Serialize(XmlDocument document, XmlElement localRoot)
        {
            
        }

        private void RefreshSurfaceMesh()
        {
            float h00 = SurfaceHeight * 0.1f;
            float h10 = Map.getRelativeTile(this, 1, 0).SurfaceHeight * 0.1f;
            float h01 = Map.getRelativeTile(this, 0, 1).SurfaceHeight * 0.1f;
            float h11 = Map.getRelativeTile(this, 1, 1).SurfaceHeight * 0.1f;
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
            float h00 = CaveHeight * 0.1f;
            float h10 = Map.getRelativeTile(this, 1, 0).CaveHeight * 0.1f;
            float h01 = Map.getRelativeTile(this, 0, 1).CaveHeight * 0.1f;
            float h11 = Map.getRelativeTile(this, 1, 1).CaveHeight * 0.1f;
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
                tileEntity.transform.localPosition = new Vector3(X * 4, SurfaceHeight * 0.1f + data.Floor * 3f, Y * 4);
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
                tileEntity.transform.localPosition = new Vector3(X * 4, SurfaceHeight * 0.1f + data.Floor * 3f, Y * 4);
            }
        }

    }
}
