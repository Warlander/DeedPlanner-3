using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data
{
    public class CaveTile : BasicTile
    {

        private int size = 30;

        public Cave Cave { get; private set; }

        public int Size {
            get {
                return size;
            }
            set {
                size = value;
            }
        }

        public override void Initialize(Tile tile, GridTile gridTile)
        {
            base.Initialize(tile, gridTile);

            GameObject caveObject = new GameObject("Cave", typeof(Cave));
            caveObject.transform.SetParent(transform);
            caveObject.transform.localPosition = Vector3.zero;
            Cave = caveObject.GetComponent<Cave>();
            Cave.Initialize(Database.DefaultCaveData);
        }

    }
}
