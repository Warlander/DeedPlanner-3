using System.Xml;

namespace Warlander.Deedplanner.Domain
{
    public abstract class TileEntity : DynamicModelBehaviour
    {
        public Tile Tile { get; set; }
        public abstract Materials Materials { get; }
        public EntityType Type => Tile.FindTypeOfEntity(this);
        public bool Valid => Tile.ContainsEntity(this);
    }
}
