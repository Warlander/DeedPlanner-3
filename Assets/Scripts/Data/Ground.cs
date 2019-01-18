using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warlander.Deedplanner.Data
{
    public class Ground
    {

        public GroundData Data { get; private set; }
        public RoadDirection RoadDirection { get; private set; }

        public Ground(GroundData data)
        {
            Data = data;
            RoadDirection = RoadDirection.Center;
        }

    }
}
