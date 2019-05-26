using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Data
{
    public class Ground : TileEntity
    {

        private Tile tile;
        private GroundData data;
        private RoadDirection roadDirection;

        private MeshRenderer meshRenderer;
        public MeshCollider Collider { get; private set; }

        public override Tile Tile {
            get {
                return tile;
            }
        }

        public GroundData Data {
            get {
                return data;
            }
            set {
                data = value;
                RoadDirection = roadDirection;
            }
        }

        public RoadDirection RoadDirection {
            get {
                return RoadDirection;
            }
            set {
                roadDirection = value;

                Material[] materials = new Material[4];
                if (roadDirection == RoadDirection.Center)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        materials[i] = Data.Tex3d.Material;
                    }
                }
                else
                {
                    Material matW = GameManager.Instance.Map.GetRelativeTile(tile, -1, 0)?.Ground.Data.Tex3d.Material;
                    Material matE = GameManager.Instance.Map.GetRelativeTile(tile, 1, 0)?.Ground.Data.Tex3d.Material;
                    Material matS = GameManager.Instance.Map.GetRelativeTile(tile, 0, -1)?.Ground.Data.Tex3d.Material;
                    Material matN = GameManager.Instance.Map.GetRelativeTile(tile, 0, 1)?.Ground.Data.Tex3d.Material;

                    if (roadDirection == RoadDirection.NW || roadDirection == RoadDirection.SW || !matW)
                    {
                        matW = Data.Tex3d.Material;
                    }
                    if (roadDirection == RoadDirection.NE || roadDirection == RoadDirection.SE || !matE)
                    {
                        matE = Data.Tex3d.Material;
                    }
                    if (roadDirection == RoadDirection.SW || roadDirection == RoadDirection.SE || !matS)
                    {
                        matS = Data.Tex3d.Material;
                    }
                    if (roadDirection == RoadDirection.NW || roadDirection == RoadDirection.NE || !matN)
                    {
                        matN = Data.Tex3d.Material;
                    }

                    materials[0] = matW;
                    materials[1] = matN;
                    materials[2] = matE;
                    materials[3] = matS;
                }

                meshRenderer.materials = materials;
            }
        }

        public override Materials Materials { get { return null; } }

        public void Initialize(Tile tile, GroundData data, Mesh mesh)
        {
            this.tile = tile;

            gameObject.layer = LayerMasks.GroundLayer;
            if (!meshRenderer)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }
            if (!GetComponent<MeshFilter>())
            {
                gameObject.AddComponent<MeshFilter>();
            }

            MeshFilter meshFilter = GetComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            if (!Collider)
            {
                Collider = gameObject.AddComponent<MeshCollider>();
            }
            Collider.sharedMesh = mesh;

            Data = data;
            RoadDirection = RoadDirection.Center;
            Material[] materials = new Material[4];
            for (int i = 0; i < 4; i++)
            {
                materials[i] = Data.Tex3d.Material;
            }
            meshRenderer.sharedMaterials = materials;
        }

        public override void Serialize(XmlDocument document, XmlElement localRoot)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(XmlElement element)
        {
            string id = element.GetAttribute("id");
            string dir = element.GetAttribute("dir");

            Data = Database.Grounds[id];
            switch (dir)
            {
                case "NW":
                    RoadDirection = RoadDirection.NW;
                    break;
                case "NE":
                    RoadDirection = RoadDirection.NE;
                    break;
                case "SW":
                    RoadDirection = RoadDirection.SW;
                    break;
                case "SE":
                    RoadDirection = RoadDirection.SE;
                    break;
                default:
                    RoadDirection = RoadDirection.Center;
                    break;
            }
        }

    }
}
