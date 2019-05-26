using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data
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
