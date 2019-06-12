using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Data
{
    public class EntityData
    {

        public int Floor { get; private set; }
        public EntityType Type { get; private set; }

        public bool IsGroundFloor => Floor == 0 || Floor == -1;

        public EntityData(int floor, EntityType type)
        {
            Floor = floor;
            Type = type;
        }

        public override bool Equals(object other)
        {
            EntityData data = other as EntityData;
            
            if (data == null)
            {
                return false;
            }

            return Floor == data.Floor && Type == data.Type;
        }

        public override int GetHashCode()
        {
            return (int)Type * 100 + Floor;
        }

        public override string ToString()
        {
            return "Entity floor " + Floor + " type " + Type;
        }

    }
}
