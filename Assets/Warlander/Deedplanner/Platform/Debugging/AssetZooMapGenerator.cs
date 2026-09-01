using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Decorations;
using Warlander.Deedplanner.Data.Floors;
using Warlander.Deedplanner.Data.Grounds;
using Warlander.Deedplanner.Data.Roofs;
using Warlander.Deedplanner.Data.Walls;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Platform.Debugging
{
    public class AssetZooMapGenerator
    {
        private const int MapHeight = 25;
        private const int AssetsPerColumn = 12;
        private const int RoofFloor = 1;
        private const int RoofPatternWidth = 67;
        private const int RoofInstancePitch = RoofPatternWidth + 1;
        private const int RoofSectionWidth = RoofInstancePitch * 2 - 1;

        // Verified layout that triggers all 14 reachable roof models; top row first.
        private static readonly string[] RoofPattern =
        {
            "...###.............................................................",
            "...###...............................###...........................",
            "...###...............................###........###................",
            "...###.....#####........#####........###.......#####...............",
            "...###.....###########..#####........###.......#####...............",
            "#########..###########..###########..#######...#####...##..##....#.",
            "#########..###########..###########..#######....###...###..###...##",
            "#########..#####........###########..#######...........#....##...##"
        };

        private readonly MapHandler _mapHandler;
        private readonly IDataCatalog _dataCatalog;

        public AssetZooMapGenerator(MapHandler mapHandler, IDataCatalog dataCatalog)
        {
            _mapHandler = mapHandler;
            _dataCatalog = dataCatalog;
        }

        public void Generate()
        {
            int wallDisplays = _dataCatalog.GetAllWalls().Count * 2;
            int width = 1
                        + SectionWidth(_dataCatalog.GetAllGrounds().Count) + 3
                        + SectionWidth(_dataCatalog.GetAllFloors().Count) + 3
                        + SectionWidth(wallDisplays) + 3
                        + RoofSectionWidth + 3
                        + SectionWidth(_dataCatalog.GetAllDecorations().Count) + 1;

            _mapHandler.CreateNewMap(width, MapHeight);
            Map map = _mapHandler.Map;

            int x = 1;
            PlaceGrounds(map, x);
            x += SectionWidth(_dataCatalog.GetAllGrounds().Count);
            PlaceSeparator(map, x);
            x += 3;
            PlaceFloors(map, x);
            x += SectionWidth(_dataCatalog.GetAllFloors().Count);
            PlaceSeparator(map, x);
            x += 3;
            PlaceWalls(map, x);
            x += SectionWidth(wallDisplays);
            PlaceSeparator(map, x);
            x += 3;
            PlaceRoofs(map, x);
            x += RoofSectionWidth;
            PlaceSeparator(map, x);
            x += 3;
            PlaceDecorations(map, x);

            map.RecalculateRoofs();
            map.CommandManager.ForgetAction();
            map.ClearDirty();
        }

        private static int SectionWidth(int assetCount)
        {
            int columns = Mathf.CeilToInt(assetCount / (float) AssetsPerColumn);
            return (columns - 1) * 2 + 1;
        }

        private static Tile DisplayTile(Map map, int startX, int index)
        {
            int column = index / AssetsPerColumn;
            int row = index % AssetsPerColumn;
            return map[startX + column * 2, 1 + row * 2];
        }

        private void PlaceGrounds(Map map, int startX)
        {
            int i = 0;
            foreach (GroundData data in _dataCatalog.GetAllGrounds())
            {
                DisplayTile(map, startX, i).Ground.Data = data;
                i++;
            }
        }

        private void PlaceFloors(Map map, int startX)
        {
            int i = 0;
            foreach (FloorData data in _dataCatalog.GetAllFloors())
            {
                int level = data.Opening ? 1 : 0;
                DisplayTile(map, startX, i).SetFloor(data, EntityOrientation.Up, level);
                i++;
            }
        }

        private void PlaceWalls(Map map, int startX)
        {
            int i = 0;
            foreach (WallData data in _dataCatalog.GetAllWalls())
            {
                for (int orientation = 0; orientation < 2; orientation++)
                {
                    bool reversed = orientation == 1;
                    Tile target = DisplayTile(map, startX, i);
                    target.SetHorizontalWall(data, reversed, 0);
                    target.SetVerticalWall(data, reversed, 0);
                    map[target.X + 1, target.Y].SetVerticalWall(data, reversed, 0);
                    map[target.X, target.Y + 1].SetHorizontalWall(data, reversed, 0);
                    i++;
                }
            }
        }

        private void PlaceRoofs(Map map, int startX)
        {
            List<RoofData> materials = new List<RoofData>(_dataCatalog.GetAllRoofs());
            int instances = Mathf.Min(4, materials.Count);
            for (int instance = 0; instance < instances; instance++)
            {
                int originX = startX + (instance % 2) * RoofInstancePitch;
                int originY = 1 + (instance / 2) * (RoofPattern.Length + 1);
                for (int line = 0; line < RoofPattern.Length; line++)
                {
                    string row = RoofPattern[line];
                    int y = originY + (RoofPattern.Length - 1 - line);
                    for (int col = 0; col < row.Length; col++)
                    {
                        if (row[col] == '#')
                        {
                            map[originX + col, y].SetRoof(materials[instance], RoofFloor);
                        }
                    }
                }
            }
        }

        private void PlaceDecorations(Map map, int startX)
        {
            int i = 0;
            foreach (DecorationData data in _dataCatalog.GetAllDecorations())
            {
                Vector2 position = data.CornerOnly ? Vector2.zero : new Vector2(2f, 2f);
                DisplayTile(map, startX, i).SetDecoration(data, position, 0f, 0, data.Floating);
                i++;
            }
        }

        private void PlaceSeparator(Map map, int startX)
        {
            GroundData slab = _dataCatalog.GetGround("sl");
            for (int y = 0; y < MapHeight; y++)
            {
                map[startX + 1, y].Ground.Data = slab;
            }
        }
    }
}
