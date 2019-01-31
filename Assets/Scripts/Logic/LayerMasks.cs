using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warlander.Deedplanner.Logic
{
    public static class LayerMasks
    {

        // Unity layers for all distinct raytraceable entities
        public const int TileLayer = 9;
        public const int GroundLayer = 10;
        public const int FloorRoofLayer = 11;
        public const int WallLayer = 12;
        public const int ObjectLayer = 13;
        public const int BridgeLayer = 14;

        // Masks from layers to use in raytracer
        public const int TileMask = 1 << TileLayer;
        public const int GroundMask = 1 << GroundLayer;
        public const int FloorRoofMask = 1 << FloorRoofLayer;
        public const int WallMask = 1 << WallLayer;
        public const int ObjectMask = 1 << ObjectLayer;
        public const int BridgeMask = 1 << BridgeLayer;

        // Combined masks to toggle what is raytraced for given feature
        public const int GroundEditMask = GroundMask;
        public const int HeightEditMask = GroundMask;
        public const int FloorEditMask = TileMask | GroundMask | FloorRoofMask | WallMask | BridgeMask;
        public const int RoofEditMask = TileMask | GroundMask | FloorRoofMask | WallMask | BridgeMask;
        public const int WallEditMask = TileMask | GroundMask | FloorRoofMask | WallMask | BridgeMask;
        public const int ObjectEditMask = TileMask | GroundMask | FloorRoofMask | WallMask | ObjectMask | BridgeMask;
        public const int AnimalEditMask = TileMask | GroundMask | FloorRoofMask | WallMask | ObjectMask | BridgeMask;
        public const int LabelEditMask = GroundMask;
        public const int BorderEditMask = GroundMask;
        public const int BridgeEditMask = TileMask | GroundMask | FloorRoofMask | WallMask | BridgeMask;

    }
}
