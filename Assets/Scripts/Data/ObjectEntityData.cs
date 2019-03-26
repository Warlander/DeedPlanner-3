using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Data
{
    public class ObjectEntityData : EntityData
    {

        public float X { get; private set; }
        public float Y { get; private set; }

        public ObjectEntityData(int floor, EntityType type, float x, float y) : base(floor, type)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object other)
        {
            ObjectEntityData data = other as ObjectEntityData;
            
            if (data == null)
            {
                return false;
            }

            return Floor == data.Floor && Type == data.Type && X == X && Y == Y;
        }

        public override int GetHashCode()
        {
            return (int)Type * 100 + Floor + (int)(X * 1000000) + (int)(Y * 100000000);
        }

        public override string ToString()
        {
            return "Entity floor " + Floor + " type " + Type + " X " + X + " Y " + Y;
        }

    }
}
