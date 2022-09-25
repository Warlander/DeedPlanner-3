using UnityEngine;
using Warlander.Deedplanner.Graphics;

namespace Warlander.Deedplanner.Data.Roofs
{
    public class RoofData
    {
        public string Name { get; }
        public string ShortName { get; }
        public TextureReference Texture { get; }
        public Materials Materials { get; }

        public RoofData(TextureReference texture, string name, string shortName, Materials materials)
        {
            Texture = texture;
            Name = name;
            ShortName = shortName;
            if (materials != null)
            {
                Materials = materials;
            }
            else
            {
                Materials = new Materials();
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
