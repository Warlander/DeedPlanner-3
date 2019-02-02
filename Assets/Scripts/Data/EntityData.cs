using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warlander.Deedplanner.Data
{
    public class EntityData
    {

        public readonly int Floor;
        private readonly EntityType Type;

        public EntityData(int floor, EntityType type)
        {
            Floor = floor;
            Type = type;
        }

    }
}
