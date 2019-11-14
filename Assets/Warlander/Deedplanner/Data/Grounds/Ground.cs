using System.Text;
using System.Xml;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data.Grounds
{
    public class Ground : IXmlSerializable
    {
        
        private GroundData data;
        private RoadDirection roadDirection = RoadDirection.Center;

        public Tile Tile { get; }

        public GroundData Data {
            get => data;
            set => Tile.Map.CommandManager.AddToActionAndExecute(new GroundDataChangeCommand(this, data, value));
        }

        public RoadDirection RoadDirection {
            get => roadDirection;
            set => Tile.Map.CommandManager.AddToActionAndExecute(new RoadDirectionChangeCommand(this, roadDirection, value));
        }

        public Ground(Tile tile, GroundData data)
        {
            Tile = tile;
            Data = data;
            RoadDirection = RoadDirection.Center;
        }

        public void Serialize(XmlDocument document, XmlElement localRoot)
        {
            localRoot.SetAttribute("id", data.ShortName);
            if (roadDirection != RoadDirection.Center) {
                localRoot.SetAttribute("dir", roadDirection.ToString().ToUpperInvariant());
            }
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

        public override string ToString()
        {
            StringBuilder build = new StringBuilder();

            build.Append("X: ").Append(Tile.X).Append(" Y: ").Append(Tile.Y).AppendLine();
            build.Append(data.Name);
            
            return build.ToString();
        }

        private class GroundDataChangeCommand : IReversibleCommand
        {

            private Ground ground;
            private GroundData oldData;
            private GroundData newData;
            
            public GroundDataChangeCommand(Ground ground, GroundData oldData, GroundData newData)
            {
                this.ground = ground;
                this.oldData = oldData;
                this.newData = newData;
            }
            
            public void Execute()
            {
                if (newData)
                {
                    ground.data = newData;
                    ground.Tile.Map.Ground.SetGroundData(ground.Tile.X, ground.Tile.Y, ground.data, ground.RoadDirection);
                }
            }

            public void Undo()
            {
                if (oldData)
                {
                    ground.data = oldData;
                    ground.Tile.Map.Ground.SetGroundData(ground.Tile.X, ground.Tile.Y, ground.data, ground.RoadDirection);
                }
            }

            public void DisposeUndo()
            {
                // no operation needed
            }

            public void DisposeRedo()
            {
                // no operation needed
            }
        }
        
        private class RoadDirectionChangeCommand : IReversibleCommand
        {

            private Ground ground;
            private RoadDirection oldDirection;
            private RoadDirection newDirection;
            
            public RoadDirectionChangeCommand(Ground ground, RoadDirection oldDirection, RoadDirection newDirection)
            {
                this.ground = ground;
                this.oldDirection = oldDirection;
                this.newDirection = newDirection;
            }
            
            public void Execute()
            {
                ground.roadDirection = newDirection;
                ground.Tile.Map.Ground.SetGroundData(ground.Tile.X, ground.Tile.Y, ground.data, ground.RoadDirection);
            }

            public void Undo()
            {
                ground.roadDirection = oldDirection;
                ground.Tile.Map.Ground.SetGroundData(ground.Tile.X, ground.Tile.Y, ground.data, ground.RoadDirection);
            }

            public void DisposeUndo()
            {
                // no operation needed
            }

            public void DisposeRedo()
            {
                // no operation needed
            }
        }

    }
}
