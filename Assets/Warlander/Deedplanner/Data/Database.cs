using System.Collections.Generic;
using Warlander.Deedplanner.Data.Caves;
using Warlander.Deedplanner.Data.Decorations;
using Warlander.Deedplanner.Data.Floors;
using Warlander.Deedplanner.Data.Grounds;
using Warlander.Deedplanner.Data.Roofs;
using Warlander.Deedplanner.Data.Walls;

namespace Warlander.Deedplanner.Data
{
    public static class Database
    {

        public static readonly Dictionary<string, GroundData> Grounds = new Dictionary<string, GroundData>();
        public static readonly Dictionary<string, CaveData> Caves = new Dictionary<string, CaveData>();
        public static readonly Dictionary<string, FloorData> Floors = new Dictionary<string, FloorData>();
        public static readonly Dictionary<string, WallData> Walls = new Dictionary<string, WallData>();
        public static readonly Dictionary<string, RoofData> Roofs = new Dictionary<string, RoofData>();
        public static readonly Dictionary<string, DecorationData> Objects = new Dictionary<string, DecorationData>();

        public static CaveData DefaultCaveData => Caves["sw"];
    }
}
