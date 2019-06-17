using UnityEngine;

namespace Warlander.Deedplanner.Gui
{
    public enum TileSelectionMode
    {
        Nothing, Tiles, TilesAndCorners, TilesAndBorders, Borders, Everything
    }

    public enum TileSelectionTarget
    {
        Nothing, Tile, InnerTile, Corner, LeftBorder, BottomBorder
    }

    public static class TileSelection
    {

        public const float BorderThickness = 0.2f;

        public static TileSelectionHit PositionToTileSelectionHit(Vector3 position, TileSelectionMode tileSelectionMode)
        {
            if (tileSelectionMode == TileSelectionMode.Nothing)
            {
                return default;
            }

            int tileX = (int) position.x / 4;
            int tileY = (int) position.z / 4;
            float partX = (position.x % 4f) / 4f;
            float partY = (position.z % 4f) / 4f;
            bool leftBorder = partX < BorderThickness;
            bool rightBorder = partX > (1f - BorderThickness);
            bool bottomBorder = partY < BorderThickness;
            bool topBorder = partY > (1f - BorderThickness);
            bool c00 = leftBorder && bottomBorder;
            bool c10 = rightBorder && bottomBorder;
            bool c01 = leftBorder && topBorder;
            bool c11 = rightBorder && topBorder;
            bool corner = c00 || c10 || c01 || c11;
            bool border = !corner && (leftBorder || rightBorder || bottomBorder || topBorder);

            if (tileSelectionMode == TileSelectionMode.Tiles)
            {
                return new TileSelectionHit(TileSelectionTarget.Tile, tileX, tileY);
            }
            if ((!corner && !border) && (tileSelectionMode == TileSelectionMode.TilesAndBorders || tileSelectionMode == TileSelectionMode.TilesAndCorners || tileSelectionMode == TileSelectionMode.Everything))
            {
                return new TileSelectionHit(TileSelectionTarget.InnerTile, tileX, tileY);
            }
            if (corner && (tileSelectionMode == TileSelectionMode.TilesAndCorners || tileSelectionMode == TileSelectionMode.Everything))
            {
                if (c10 || c11)
                {
                    tileX++;
                }
                if (c01 || c11)
                {
                    tileY++;
                }
                return new TileSelectionHit(TileSelectionTarget.Corner, tileX, tileY);
            }
            if (border && (tileSelectionMode == TileSelectionMode.Borders || tileSelectionMode == TileSelectionMode.TilesAndBorders || tileSelectionMode == TileSelectionMode.Everything))
            {
                if (rightBorder)
                {
                    tileX++;
                }
                if (topBorder)
                {
                    tileY++;
                }

                if (leftBorder || rightBorder)
                {
                    return new TileSelectionHit(TileSelectionTarget.LeftBorder, tileX, tileY);
                }
                else if (bottomBorder || topBorder)
                {
                    return new TileSelectionHit(TileSelectionTarget.BottomBorder, tileX, tileY);
                }
            }

            return default;
        }
    }

    public struct TileSelectionHit
    {

        public TileSelectionTarget Target { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }

        public TileSelectionHit(TileSelectionTarget target, int x, int y)
        {
            Target = target;
            X = x;
            Y = y;
        }
        
    }
}
