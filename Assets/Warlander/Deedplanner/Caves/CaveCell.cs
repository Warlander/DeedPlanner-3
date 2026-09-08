using System;
using System.Globalization;
using System.Xml;
using Warlander.Deedplanner.Domain.Entities.Caves;

namespace Warlander.Deedplanner.Caves
{
    public sealed class CaveCell
    {
        public const int DefaultFloorHeight = 5;
        public const int DefaultClearance = 30;

        public CaveData Terrain { get; private set; }
        public int FloorHeight { get; private set; }
        public int Clearance { get; private set; }
        public bool IsSolid => Terrain.Wall;

        public CaveCell(CaveData terrain)
        {
            Terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            FloorHeight = DefaultFloorHeight;
            Clearance = DefaultClearance;
        }

        public void Initialize(CaveData terrain, int floorHeight, int clearance)
        {
            Terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            FloorHeight = floorHeight;
            Clearance = clearance;
        }

        public void InitializeTerrain(CaveData terrain)
        {
            Terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
        }

        public void InitializeHeights(int floorHeight, int clearance)
        {
            FloorHeight = floorHeight;
            Clearance = clearance;
        }

        public void Serialize(XmlDocument document, XmlElement tileElement, ICaveDataResolver dataResolver)
        {
            if (dataResolver == null)
            {
                throw new ArgumentNullException(nameof(dataResolver));
            }

            tileElement.SetAttribute("caveHeight", FloorHeight.ToString(CultureInfo.InvariantCulture));
            tileElement.SetAttribute("caveSize", Clearance.ToString(CultureInfo.InvariantCulture));

            XmlElement caveElement = document.CreateElement("cave");
            caveElement.SetAttribute("id", dataResolver.Resolve(Terrain.ShortName).ShortName);
            tileElement.AppendChild(caveElement);
        }

        public void Deserialize(XmlElement tileElement, ICaveDataResolver dataResolver)
        {
            if (dataResolver == null)
            {
                throw new ArgumentNullException(nameof(dataResolver));
            }

            int floorHeight = DefaultFloorHeight;
            int clearance = DefaultClearance;
            if (tileElement.HasAttribute("caveHeight"))
            {
                floorHeight = (int)Convert.ToSingle(tileElement.GetAttribute("caveHeight"), CultureInfo.InvariantCulture);
            }
            if (tileElement.HasAttribute("caveSize"))
            {
                clearance = (int)Convert.ToSingle(tileElement.GetAttribute("caveSize"), CultureInfo.InvariantCulture);
            }

            CaveData terrain = dataResolver.Resolve(null);
            bool hasCaveElement = false;
            foreach (XmlNode childNode in tileElement.ChildNodes)
            {
                if (childNode is XmlElement childElement && childElement.Name == "cave")
                {
                    hasCaveElement = true;
                    terrain = dataResolver.Resolve(childElement.GetAttribute("id"));
                    break;
                }
            }

            if (!hasCaveElement && clearance == 0)
            {
                clearance = DefaultClearance;
            }

            Initialize(terrain, floorHeight, clearance);
        }

        internal void SetFloorHeight(int floorHeight)
        {
            FloorHeight = floorHeight;
        }

        internal void SetTerrain(CaveData terrain)
        {
            Terrain = terrain;
        }

        internal void SetClearance(int clearance)
        {
            Clearance = clearance;
        }
    }
}
