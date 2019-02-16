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
    public class SurfaceTile : BasicTile
    {
        
        public Ground Ground { get; private set; }

        public override void Initialize(Tile tile, GridTile gridTile)
        {
            base.Initialize(tile, gridTile);

            GameObject groundObject = new GameObject("Ground", typeof(Ground));
            groundObject.transform.SetParent(transform);
            groundObject.transform.localPosition = Vector3.zero;
            Ground = groundObject.GetComponent<Ground>();
            Ground.Initialize(Data.Grounds["gr"], HeightMesh);
        }

        protected override void RefreshMesh()
        {
            base.RefreshMesh();
            Ground.Collider.sharedMesh = HeightMesh;
        }

    }
}
