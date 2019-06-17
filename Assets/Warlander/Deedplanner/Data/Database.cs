using System.Collections.Generic;
using Warlander.Deedplanner.Data.Cave;
using Warlander.Deedplanner.Data.Floor;
using Warlander.Deedplanner.Data.Ground;
using Warlander.Deedplanner.Data.Object;
using Warlander.Deedplanner.Data.Roof;
using Warlander.Deedplanner.Data.Wall;

namespace Warlander.Deedplanner.Data
{
    public static class Database
    {

        public static readonly Dictionary<string, GroundData> Grounds = new Dictionary<string, GroundData>();
        public static readonly Dictionary<string, CaveData> Caves = new Dictionary<string, CaveData>();
        public static readonly Dictionary<string, FloorData> Floors = new Dictionary<string, FloorData>();
        public static readonly Dictionary<string, WallData> Walls = new Dictionary<string, WallData>();
        public static readonly Dictionary<string, RoofData> Roofs = new Dictionary<string, RoofData>();
        public static readonly Dictionary<string, GameObjectData> Objects = new Dictionary<string, GameObjectData>();

        public static CaveData DefaultCaveData => Caves["sw"];
    }
}
