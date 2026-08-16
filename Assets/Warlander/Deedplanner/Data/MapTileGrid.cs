using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Warlander.Deedplanner.Data
{
    public class MapTileGrid : IEnumerable<Tile>
    {
        private readonly Tile[,] _tiles;

        public int Width { get; }
        public int Height { get; }
        public int VisibleTilesCount => Width * Height;
        public int AllTilesCount => (Width + 1) * (Height + 1);

        public MapTileGrid(int width, int height)
        {
            Width = width;
            Height = height;
            _tiles = new Tile[width + 1, height + 1];
        }

        public Tile this[int x, int y]
        {
            get
            {
                if (x < 0 || y < 0 || x > Width || y > Height)
                {
                    return null;
                }

                return _tiles[x, y];
            }
        }

        public Tile this[Vector2Int v] => this[v.x, v.y];

        public void SetTile(int x, int y, Tile tile)
        {
            _tiles[x, y] = tile;
        }

        public int CoordinateToIndex(int x, int y)
        {
            return x * (Height + 1) + y;
        }

        public Tile GetRelativeTile(Tile tile, int relativeX, int relativeY)
        {
            int x = tile.X + relativeX;
            int y = tile.Y + relativeY;

            if (x < 0 || x > Width || y < 0 || y > Height)
            {
                return null;
            }

            return this[x, y];
        }

        public IEnumerator<Tile> GetEnumerator()
        {
            return _tiles.Cast<Tile>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
