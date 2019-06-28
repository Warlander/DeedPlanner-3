using UnityEngine;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data.Grounds
{
    public class GroundData : ScriptableObject
    {

        public string Name { get; private set; }
        public string ShortName { get; private set; }

        public TextureReference Tex2d { get; private set; }
        public TextureReference Tex3d { get; private set; }

        public bool Diagonal { get; private set; }

        public void Initialize(string name, string shortName, TextureReference tex2d, TextureReference tex3d, bool diagonal)
        {
            Name = name;
            ShortName = shortName;
            Tex2d = tex2d;
            Tex3d = tex3d;
            Diagonal = diagonal;
        }

        public override string ToString()
        {
            return Name;
        }

    }
}
