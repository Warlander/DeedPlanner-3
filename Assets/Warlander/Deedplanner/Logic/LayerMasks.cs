using System;

namespace Warlander.Deedplanner.Logic
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

        // Masks from layers to use in raytracer
        public const int TileMask = 1 << TileLayer;
        public const int GroundMask = 1 << GroundLayer;
        public const int FloorRoofMask = 1 << FloorRoofLayer;
        public const int WallMask = 1 << WallLayer;
        public const int DecorationMask = 1 << DecorationLayer;
        public const int BridgeMask = 1 << BridgeLayer;

        // Combined masks to toggle what is raytraced for given feature
        public const int GroundEditMask = GroundMask;
        public const int HeightEditMask = GroundMask;
        public const int FloorEditMask = TileMask | GroundMask | FloorRoofMask | WallMask | BridgeMask;
        public const int RoofEditMask = TileMask | GroundMask | FloorRoofMask | WallMask | BridgeMask;
        public const int WallEditMask = TileMask | GroundMask | WallMask | BridgeMask;
        public const int DecorationEditMask = TileMask | GroundMask | FloorRoofMask | WallMask | DecorationMask | BridgeMask;
        public const int LabelEditMask = GroundMask;
        public const int BorderEditMask = GroundMask;
        public const int BridgeEditMask = TileMask | GroundMask | FloorRoofMask | WallMask | BridgeMask;
        public const int ToolsEditMask = TileMask;
        public const int MenuEditMask = GroundMask;

        public static int GetMaskForTab(Tab tab)
        {
            switch (tab)
            {
                case Tab.Ground:
                    return GroundEditMask;
                case Tab.Caves:
                    return GroundEditMask;
                case Tab.Height:
                    return HeightEditMask;
                case Tab.Floors:
                    return FloorEditMask;
                case Tab.Roofs:
                    return RoofEditMask;
                case Tab.Walls:
                    return WallEditMask;
                case Tab.Objects:
                    return DecorationEditMask;
                case Tab.Labels:
                    return LabelEditMask;
                case Tab.Borders:
                    return BorderEditMask;
                case Tab.Bridges:
                    return BridgeEditMask;
                case Tab.Tools:
                    return ToolsEditMask;
                case Tab.Menu:
                    return MenuEditMask;
                default:
                    throw new ArgumentException("Cannot find mask for tab " + tab, nameof(tab));
            }
        }
    }
}
