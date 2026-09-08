using Warlander.Deedplanner.Editing;
using System;

namespace Warlander.Deedplanner.Domain
{
    public static class LayerMasks
    {
        // Unity layers for all distinct raytraceable entities
        public const int TileLayer = 9;
        public const int GroundLayer = 10;
        public const int FloorRoofLayer = 11;
        public const int WallLayer = 12;
        public const int DecorationLayer = 13;
        public const int BridgeLayer = 14;
        public const int CaveLayer = 15;
        public const int CaveFloorRoofLayer = 16;
        public const int CaveWallLayer = 17;
        public const int CaveDecorationLayer = 18;
        public const int CaveBridgeLayer = 19;

        // Masks from layers to use in raytracer
        public const int TileMask = 1 << TileLayer;
        public const int GroundMask = 1 << GroundLayer;
        public const int FloorRoofMask = 1 << FloorRoofLayer;
        public const int WallMask = 1 << WallLayer;
        public const int DecorationMask = 1 << DecorationLayer;
        public const int BridgeMask = 1 << BridgeLayer;
        public const int CaveMask = 1 << CaveLayer;
        public const int CaveFloorRoofMask = 1 << CaveFloorRoofLayer;
        public const int CaveWallMask = 1 << CaveWallLayer;
        public const int CaveDecorationMask = 1 << CaveDecorationLayer;
        public const int CaveBridgeMask = 1 << CaveBridgeLayer;

        // Combined masks to toggle what is raytraced for given feature
        public const int GroundEditMask = GroundMask;
        public const int HeightEditMask = GroundMask;
        public const int FloorEditMask = TileMask | GroundMask | FloorRoofMask | WallMask;
        public const int RoofEditMask = TileMask | GroundMask | FloorRoofMask | WallMask | BridgeMask;
        public const int WallEditMask = TileMask | GroundMask | WallMask;
        public const int DecorationEditMask = TileMask | GroundMask | FloorRoofMask | WallMask | DecorationMask | BridgeMask;
        public const int LabelEditMask = GroundMask;
        public const int BorderEditMask = GroundMask;
        public const int BridgeEditMask = TileMask | GroundMask | FloorRoofMask | BridgeMask;
        public const int ToolsEditMask = TileMask;
        public const int MenuEditMask = GroundMask;

        public static int GetMaskForTab(Tab tab)
        {
            return GetMaskForTab(tab, 0);
        }

        public static int GetMaskForTab(Tab tab, int level)
        {
            bool cave = level < 0;
            switch (tab)
            {
                case Tab.Ground:
                    return GroundEditMask;
                case Tab.Caves:
                    return CaveMask;
                case Tab.Height:
                    return level < 0 ? CaveMask : HeightEditMask;
                case Tab.Floors:
                    return cave ? CaveMask | CaveFloorRoofMask | CaveWallMask : FloorEditMask;
                case Tab.Roofs:
                    return cave ? CaveMask | CaveFloorRoofMask | CaveWallMask | CaveBridgeMask : RoofEditMask;
                case Tab.Walls:
                    return cave ? CaveMask | CaveWallMask : WallEditMask;
                case Tab.Objects:
                    return cave
                        ? CaveMask | CaveFloorRoofMask | CaveWallMask | CaveDecorationMask | CaveBridgeMask
                        : DecorationEditMask;
                case Tab.Labels:
                    return cave ? CaveMask : LabelEditMask;
                case Tab.Borders:
                    return cave ? CaveMask : BorderEditMask;
                case Tab.Bridges:
                    return cave ? CaveMask | CaveFloorRoofMask | CaveBridgeMask : BridgeEditMask;
                case Tab.Tools:
                    return cave ? CaveMask | CaveFloorRoofMask : ToolsEditMask;
                case Tab.Menu:
                    return MenuEditMask;
                default:
                    throw new ArgumentException("Cannot find mask for tab " + tab, nameof(tab));
            }
        }

        public static int GetLayerForLevel(int surfaceLayer, int level)
        {
            if (level >= 0)
            {
                return surfaceLayer;
            }

            switch (surfaceLayer)
            {
                case FloorRoofLayer:
                    return CaveFloorRoofLayer;
                case WallLayer:
                    return CaveWallLayer;
                case DecorationLayer:
                    return CaveDecorationLayer;
                case BridgeLayer:
                    return CaveBridgeLayer;
                default:
                    return surfaceLayer;
            }
        }
    }
}
