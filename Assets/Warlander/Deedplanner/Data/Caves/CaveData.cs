using UnityEngine;
using Warlander.Deedplanner.Graphics;

namespace Warlander.Deedplanner.Data.Caves
{
    public class CaveData
    {
        public string Name { get; }
        public string ShortName { get; }
        public string[][] Categories { get; }

        public TextureReference Texture { get; }

        public bool Wall { get; }
        public bool Show { get; }
        public bool Entrance { get; }

        public CaveData(TextureReference texture, string name, string shortName, string[][] categories, bool wall, bool show, bool entrance)
        {
            Texture = texture;
            Name = name;
            ShortName = shortName;
            Categories = categories;
            Wall = wall;
            Show = show;
            Entrance = entrance;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
