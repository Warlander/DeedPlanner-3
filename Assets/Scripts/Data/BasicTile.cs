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
    public abstract class BasicTile : MonoBehaviour, IXMLSerializable
    {

        private int height = 0;

        protected Tile Tile { get; private set; }
        protected Mesh HeightMesh { get; private set; }
        protected Dictionary<EntityData, TileEntity> Entities { get; private set; }
        private GridTile gridTile;

        public int X {
            get {
                return Tile.X;
            }
        }

        public int Y {
            get {
                return Tile.Y;
            }
        }

        public int Height {
            get {
                return height;
            }
            set {
                height = value;
                RefreshMesh();
                UpdateEntitiesHeights();
                Tile.Map.getRelativeTile(Tile, -1, 0)?.GetTileOfSameType(this).RefreshMesh();
                Tile.Map.getRelativeTile(Tile, -1, 0)?.GetTileOfSameType(this).UpdateEntitiesHeights();
                Tile.Map.getRelativeTile(Tile, 0, -1)?.GetTileOfSameType(this).RefreshMesh();
                Tile.Map.getRelativeTile(Tile, 0, -1)?.GetTileOfSameType(this).UpdateEntitiesHeights();
                Tile.Map.getRelativeTile(Tile, -1, -1)?.GetTileOfSameType(this).RefreshMesh();
                Tile.Map.getRelativeTile(Tile, -1, -1)?.GetTileOfSameType(this).UpdateEntitiesHeights();

                Tile.Map.RecalculateHeights();
            }
        }

        public virtual void Initialize(Tile tile, GridTile gridTile)
        {
            Vector3[] vertices = new Vector3[] { new Vector3(0, 0, 0), new Vector3(4, 0, 0), new Vector3(0, 0, 4), new Vector3(4, 0, 4), new Vector3(2, 0, 2) };
            Vector2[] uv = new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 0.5f) };
            int[] triangles = new int[] { 0, 2, 4, 2, 3, 4, 3, 1, 4, 1, 0, 4 };

            HeightMesh = new Mesh();
            HeightMesh.vertices = vertices;
            HeightMesh.uv = uv;
            HeightMesh.triangles = triangles;
            HeightMesh.RecalculateNormals();
            HeightMesh.RecalculateBounds();

            Entities = new Dictionary<EntityData, TileEntity>();

            this.Tile = tile;
            this.gridTile = gridTile;
            gridTile.Initialize(HeightMesh, Tile.X, Tile.Y);
        }

        public Floor SetFloor(FloorData data, EntityOrientation orientation, int level)
        {
            EntityData entityData = new EntityData(level, EntityType.FLOORROOF);
            TileEntity tileEntity;
            Entities.TryGetValue(entityData, out tileEntity);
            if (tileEntity == null)
            {
                return CreateNewFloor(entityData, data, orientation, level);
            }
            else if (tileEntity.GetType() == typeof(Floor))
            {
                Floor currentFloor = (Floor) tileEntity;
                if (currentFloor.Data != data || currentFloor.Orientation != orientation)
                {
                    Destroy(currentFloor.gameObject);
                }

                return CreateNewFloor(entityData, data, orientation, level);
            }
            // TODO: add behaviour if roof is there

            return null;
        }

        private Floor CreateNewFloor(EntityData entity, FloorData data, EntityOrientation orientation, int level)
        {
            GameObject floorObject = new GameObject("Floor " + level, typeof(Floor));
            Floor floor = floorObject.GetComponent<Floor>();
            floor.Initialize(data, orientation);

            Entities[entity] = floor;
            floorObject.transform.SetParent(transform);
            UpdateEntitiesHeights();

            return floor;
        }

        public void Serialize(XmlDocument document, XmlElement localRoot)
        {
            
        }

        protected virtual void RefreshMesh()
        {
            float h00 = Height * 0.1f;
            float h10 = Tile.Map.getRelativeTile(Tile, 1, 0).GetTileOfSameType(this).Height * 0.1f;
            float h01 = Tile.Map.getRelativeTile(Tile, 0, 1).GetTileOfSameType(this).Height * 0.1f;
            float h11 = Tile.Map.getRelativeTile(Tile, 1, 1).GetTileOfSameType(this).Height * 0.1f;
            float hMid = (h00 + h10 + h01 + h11) / 4f;
            Vector3[] vertices = new Vector3[5];
            vertices[0] = new Vector3(0, h00, 0);
            vertices[1] = new Vector3(4, h10, 0);
            vertices[2] = new Vector3(0, h01, 4);
            vertices[3] = new Vector3(4, h11, 4);
            vertices[4] = new Vector3(2, hMid, 2);
            HeightMesh.vertices = vertices;
            HeightMesh.RecalculateNormals();
            HeightMesh.RecalculateBounds();
            gridTile.Collider.sharedMesh = HeightMesh;
        }

        private void UpdateEntitiesHeights()
        {
            foreach (KeyValuePair<EntityData, TileEntity> pair in Entities)
            {
                EntityData data = pair.Key;
                TileEntity tileEntity = pair.Value;
                tileEntity.transform.localPosition = new Vector3(0, Height * 0.1f + data.Floor * 3f, 0);
            }
        }

    }
}
