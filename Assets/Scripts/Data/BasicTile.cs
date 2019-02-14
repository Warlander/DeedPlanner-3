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

        public void Initialize(Tile tile, GridTile gridTile)
        {
            Vector3[] vertices = new Vector3[] { new Vector3(0, 0, 0), new Vector3(4, 0, 0), new Vector3(0, 0, 4), new Vector3(4, 0, 4) };
            Vector2[] uv = new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            int[] triangles = new int[] { 0, 2, 1, 2, 3, 1 };

            HeightMesh = new Mesh();
            HeightMesh.vertices = vertices;
            HeightMesh.uv = uv;
            HeightMesh.triangles = triangles;
            HeightMesh.RecalculateNormals();

            Entities = new Dictionary<EntityData, ITileEntity>();

            this.Tile = tile;
            this.gridTile = gridTile;
            gridTile.Initialize(HeightMesh);
        }

        protected void RefreshMesh()
        {
            Vector3[] vertices = new Vector3[4];
            vertices[0] = new Vector3(0, Height * 0.1f, 0);
            vertices[1] = new Vector3(4, Tile.Map.getRelativeTile(Tile, 1, 0).GetTileOfSameType(this).Height * 0.1f, 0);
            vertices[2] = new Vector3(0, Tile.Map.getRelativeTile(Tile, 0, 1).GetTileOfSameType(this).Height * 0.1f, 4);
            vertices[3] = new Vector3(4, Tile.Map.getRelativeTile(Tile, 1, 1).GetTileOfSameType(this).Height * 0.1f, 4);
            HeightMesh.vertices = vertices;
            HeightMesh.RecalculateNormals();
            HeightMesh.RecalculateBounds();
        }

    }
}
