using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data
{
    public class FloorData : ScriptableObject
    {

        public string Name { get; private set; }
        public string ShortName { get; private set; }
        public Model Model { get; private set; }
        public bool Opening { get; private set; }
        public Materials Materials { get; private set; }

        public void Initialize(Model model, string name, string shortName, bool opening, Materials materials)
        {
            Model = model;
            Name = name;
            ShortName = shortName;
            Opening = opening;
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
