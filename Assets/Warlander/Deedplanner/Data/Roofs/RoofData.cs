using UnityEngine;
using Warlander.Deedplanner.Graphics;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data.Roofs
{
    public class RoofData : ScriptableObject
    {

        public string Name { get; private set; }
        public string ShortName { get; private set; }
        public TextureReference Texture { get; private set; }
        public Materials Materials { get; private set; }

        public void Initialize(TextureReference texture, string name, string shortName, Materials materials)
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
