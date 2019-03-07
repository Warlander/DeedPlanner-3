using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warlander.Deedplanner.Data
{
    public static class Database
    {

        public static readonly Dictionary<string, GroundData> Grounds = new Dictionary<string, GroundData>();
        public static readonly Dictionary<string, FloorData> Floors = new Dictionary<string, FloorData>();
        public static readonly Dictionary<string, RoofData> Roofs = new Dictionary<string, RoofData>();

    }
}
