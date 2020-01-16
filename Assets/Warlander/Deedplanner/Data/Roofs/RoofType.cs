using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Graphics;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Data.Roofs
{
    public sealed class RoofType
    {
        public static RoofType[] RoofTypes { get; private set; }

        static RoofType()
        {
            List<RoofType> roofTypesList = new List<RoofType>();

            roofTypesList.Add(new RoofType("Special/side.wom",
                              new int[,] {{ 1, 1, 1},
                                                    { 0, 0, 0},
                                                    {-1,-2,-1}}));

            roofTypesList.Add(new RoofType("Special/sideCorner.wom",
                              new int[,] {{-1,-2,-1},
                                                    { 0, 0,-2},
                                                    { 1, 0,-1}}));

            roofTypesList.Add(new RoofType("Special/sideCut.wom",
                              new int[,] {{ 1, 1, 1},
                                                    { 0, 0, 1},
                                                    {-2, 0, 1}}));

            roofTypesList.Add(new RoofType("Special/spine.wom",
                              new int[,] {{-1,-2,-1},
                                                    { 0, 0, 0},
                                                    {-1,-2,-1}}));

            roofTypesList.Add(new RoofType("Special/spineEnd.wom",
                              new int[,] {{-1,-2,-1},
                                                    { 0, 0,-2},
                                                    {-1,-2,-1}}));

            roofTypesList.Add(new RoofType("Special/spineEndUp.wom",
                              new int[,] {{-1,-2,-1},
                                                    { 0, 0, 3},
                                                    { 1, 0,-1}}));

            roofTypesList.Add(new RoofType("Special/spineEndUp.wom", new Vector3(-1, 1, 1),
                              new int[,] {{-1,-2,-1},
                                                    { 3, 0, 0},
                                                    {-1, 0, 1}}));

            roofTypesList.Add(new RoofType("Special/spineCorner.wom",
                              new int[,] {{-1,-2,-1},
                                                    {-2, 0, 0},
                                                    {-1, 0,-2}}));

            roofTypesList.Add(new RoofType("Special/spineCornerUp.wom",
                              new int[,] {{-2, 0,-2},
                                                    { 0, 0, 0},
                                                    { 1, 0,-2}}));

            roofTypesList.Add(new RoofType("Special/spineCross.wom",
                              new int[,] {{-2, 0,-2},
                                                    { 0, 0, 0},
                                                    {-2, 0,-2}}));

            roofTypesList.Add(new RoofType("Special/spineTCross.wom",
                              new int[,] {{-1, 0,-2},
                                                    {-2, 0, 0},
                                                    {-1, 0,-2}}));

            roofTypesList.Add(new RoofType("Special/spineTip.wom",
                              new int[,] {{-1,-2,-1},
                                                    {-2, 0,-2},
                                                    {-1,-2,-1}}));
            
            roofTypesList.Add(new RoofType("Special/spineUp.wom",
                              new int[,] {{ 1, 0,-2},
                                                    { 1, 0, 0},
                                                    { 1, 0,-2}}));
            
            roofTypesList.Add(new RoofType("Special/sideToSpine.wom",
                              new int[,] {{ 1, 0,-2},
                                                    { 1, 0, 0},
                                                    { 1, 0,-2}}));

            roofTypesList.Add(new RoofType("Special/levelsCross.wom",
                              new int[,] {{-2, 0, 1},
                                                    { 0, 0, 0},
                                                    { 1, 0,-2}}));

            RoofTypes = roofTypesList.ToArray();
        }

        private readonly Dictionary<RoofData, Model> models;
        private readonly int[,] conditions;

        private RoofType(string modelLocation, int[,] conditions) : this(modelLocation, new Vector3(1, 1, 1), conditions) {}

        private RoofType(string modelLocation, Vector3 scale, int[,] conditions)
        {
            models = new Dictionary<RoofData, Model>();
            this.conditions = conditions;

            foreach (RoofData data in Database.Roofs.Values)
            {
                Model model = new Model(modelLocation, scale, LayerMasks.FloorRoofLayer);
                model.AddTextureOverride("*", data.Texture.Location);
                models[data] = model;
            }
        }

        public int CheckMatch(Tile tile, int height)
        {
            if (CheckCase(tile, height, conditions))
            {
                return 0;
            }
            else if (CheckCase(tile, height, Rotate(conditions, 3)))
            {
                return 1;
            }
            else if (CheckCase(tile, height, Rotate(conditions, 2)))
            {
                return 2;
            }
            else if (CheckCase(tile, height, Rotate(conditions, 1)))
            {
                return 3;
            }
            return -1;
        }

        private bool CheckCase(Tile tile, int height, int[,] check)
        {
            int cX = 0;
            int cY = 0;
            int roof;

            Map map = tile.Map;

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (!map.GetRelativeTile(tile, x, y))
                    {
                        roof = -1;
                    }
                    else if (!map.GetRelativeTile(tile, x, y).GetTileContent(height) || map.GetRelativeTile(tile, x, y).GetTileContent(height).GetType() != typeof(Roof))
                    {
                        roof = -1;
                    }
                    else
                    {
                        roof = ((Roof)map.GetRelativeTile(tile, x, y).GetTileContent(height)).RoofLevel;
                    }
                    switch (check[cX, cY])
                    {
                        case -2:
                            if (((Roof)tile.GetTileContent(height)).RoofLevel <= roof)
                            {
                                return false;
                            }
                            break;
                        case -1:
                            if (((Roof)tile.GetTileContent(height)).RoofLevel < roof)
                            {
                                return false;
                            }
                            break;
                        case 0:
                            if (((Roof)tile.GetTileContent(height)).RoofLevel != roof)
                            {
                                return false;
                            }
                            break;
                        case 1:
                            if (((Roof)tile.GetTileContent(height)).RoofLevel > roof)
                            {
                                return false;
                            }
                            break;
                        case 2:
                            if (((Roof)tile.GetTileContent(height)).RoofLevel >= roof)
                            {
                                return false;
                            }
                            break;
                    }
                    cY++;
                }
                cX++;
                cY=0;
            }
            return true;
        }

        private int[,] Rotate(int[,] input, int r)
        {
            int[,] output = new int[3, 3];
            int i = 0;
            int i2 = 0;

            switch (r)
            {
                case 0:
                    return input;
                case 1:
                    for (int x = 0; x <= 2; x++)
                    {
                        for (int y = 2; y >= 0; y--)
                        {
                            output[x, y] = input[i, i2];
                            i++;
                        }
                        i2++;
                        i=0;
                    }
                    return output;
                case 2:
                    for (int x = 2; x >= 0; x--)
                    {
                        for (int y = 2; y >= 0; y--)
                        {
                            output[x, y] = input[i, i2];
                        i2++;
                        }
                        i2 = 0;
                        i++;
                    }
                    return output;
                case 3:
                    for (int x = 2; x >= 0; x--)
                    {
                        for (int y = 0; y <= 2; y++)
                        {
                            output[x, y] = input[i, i2];
                            i++;
                        }
                        i2++;
                        i=0;
                    }
                    return output;
            }
            return output;
        }

        public Model GetModelForData(RoofData data)
        {
            return models[data];
        }
    }
}
