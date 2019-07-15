using System.Text;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Data.Walls
{
    public class Wall : TileEntity
    {

        private Tile tile;

        public WallData Data { get; private set; }
        public bool Reversed { get; private set; }
        public override Materials Materials => Data.Materials;

        public GameObject Model { get; private set; }
        private MeshCollider meshCollider;

        public override Tile Tile => tile;

        public void Initialize(Tile tile, WallData data, bool reversed, bool firstFloor, int slopeDifference)
        {
            this.tile = tile;
            gameObject.layer = LayerMasks.WallLayer;

            if (!GetComponent<MeshCollider>())
            {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }

            Data = data;
            Reversed = reversed;
            UpdateModel(slopeDifference, firstFloor);
        }

        public void UpdateModel(int slopeDifference, bool firstFloor)
        {
            if (Model)
            {
                Destroy(Model);
            }

            if (firstFloor)
            {
                Model = Data.BottomModel.CreateOrGetModel(slopeDifference, Reversed);
            }
            else
            {
                Model = Data.NormalModel.CreateOrGetModel(slopeDifference, Reversed);
            }
            Model.transform.SetParent(transform, false);

            Bounds bounds = Data.NormalModel.Bounds;
                
            Mesh boundsMesh = new Mesh();
            Vector3[] vectors = new Vector3[8];
            float padding = 1.01f;
            vectors[0] = (bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y - slopeDifference * 0.1f, -bounds.extents.z) * padding);
            vectors[1] = (bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y - slopeDifference * 0.1f, bounds.extents.z) * padding);
            vectors[2] = (bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, bounds.extents.z) * padding);
            vectors[3] = (bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, -bounds.extents.z) * padding);
            vectors[4] = (bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y - slopeDifference * 0.1f, -bounds.extents.z) * padding);
            vectors[5] = (bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y - slopeDifference * 0.1f, bounds.extents.z) * padding);
            vectors[6] = (bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, bounds.extents.z) * padding);
            vectors[7] = (bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, -bounds.extents.z) * padding);
            int[] triangles = new int[36];

            // bottom
            triangles[0] = 0;
            triangles[1] = 1;
            triangles[2] = 2;
            triangles[3] = 2;
            triangles[4] = 3;
            triangles[5] = 0;

            // top
            triangles[6] = 4;
            triangles[7] = 5;
            triangles[8] = 6;
            triangles[9] = 6;
            triangles[10] = 7;
            triangles[11] = 4;

            // left
            triangles[12] = 0;
            triangles[13] = 1;
            triangles[14] = 4;
            triangles[15] = 1;
            triangles[16] = 5;
            triangles[17] = 4;

            // right
            triangles[18] = 2;
            triangles[19] = 3;
            triangles[20] = 6;
            triangles[21] = 3;
            triangles[22] = 7;
            triangles[23] = 6;

            //up
            triangles[24] = 4;
            triangles[25] = 3;
            triangles[26] = 0;
            triangles[27] = 4;
            triangles[28] = 7;
            triangles[29] = 3;

            //down
            triangles[30] = 1;
            triangles[31] = 2;
            triangles[32] = 5;
            triangles[33] = 2;
            triangles[34] = 6;
            triangles[35] = 5;

            boundsMesh.vertices = vectors;
            boundsMesh.triangles = triangles;
            meshCollider.sharedMesh = boundsMesh;
        }

        public override void Serialize(XmlDocument document, XmlElement localRoot)
        {
            localRoot.SetAttribute("id", Data.ShortName);
            if (Data.HouseWall) {
                localRoot.SetAttribute("reversed", Reversed.ToString());
            }
        }
        
        public override string ToString()
        {
            StringBuilder build = new StringBuilder();

            build.Append("X: ").Append(tile.X).Append(" Y: ").Append(tile.Y).AppendLine();
            build.Append(Data.Name);
            
            return build.ToString();
        }
    }
}
