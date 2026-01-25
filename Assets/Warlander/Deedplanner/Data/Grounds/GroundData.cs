using UnityEngine;
using Warlander.Deedplanner.Graphics;

namespace Warlander.Deedplanner.Data.Grounds
{
    public class GroundData
    {
        public string Name { get; }
        public string ShortName { get; }
        public string[][] Categories { get; }

        public TextureReference Tex2d { get; }
        public TextureReference Tex3d { get; }

        public bool Diagonal { get; }
        public bool IsCaveDoor { get; private set; } = false;

        public GroundData(string name, string shortName, string[][] categories, TextureReference tex2d, TextureReference tex3d, bool diagonal)
        {
            Name = name;
            ShortName = shortName;
            Categories = categories;
            Tex2d = tex2d;
            Tex3d = tex3d;
            Diagonal = diagonal;
            IsCaveDoor = ShortName is "wcaDoor" or "gcaDoor" or "scaDoor" or "mcaDoor";
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
