using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Data
{
    public abstract class BasicTile : MonoBehaviour
    {

        private int height = 0;

        protected Tile Tile { get; private set; }
        protected Mesh HeightMesh { get; private set; }
        protected Dictionary<EntityData, ITileEntity> Entities { get; private set; }
        private GridTile gridTile;

        public int Height {
            get {
                return height;
            }
            set {
                height = value;
                RefreshMesh();
                Tile.Map.getRelativeTile(Tile, -1, 0)?.GetTileOfSameType(this).RefreshMesh();
                Tile.Map.getRelativeTile(Tile, 0, -1)?.GetTileOfSameType(this).RefreshMesh();
                Tile.Map.getRelativeTile(Tile, -1, -1)?.GetTileOfSameType(this).RefreshMesh();

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

            Entities = new Dictionary<EntityData, ITileEntity>();

            this.Tile = tile;
            this.gridTile = gridTile;
            gridTile.Initialize(HeightMesh);
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

    }
}
