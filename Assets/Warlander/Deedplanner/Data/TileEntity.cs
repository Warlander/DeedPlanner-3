using System.Xml;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data
{
    public abstract class TileEntity : DynamicModelBehaviour
    {
        public Tile Tile { get; set; }
        public abstract Materials Materials { get; }
        public EntityType Type => Tile.FindTypeOfEntity(this);
        public bool Valid => Tile.ContainsEntity(this);
    }
}