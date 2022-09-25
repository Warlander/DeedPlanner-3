using UnityEngine;
using Warlander.Deedplanner.Graphics;

namespace Warlander.Deedplanner.Data.Floors
{
    public class FloorData
    {
        public string Name { get; }
        public string ShortName { get; }
        public Model Model { get; }
        public bool Opening { get; }
        public Materials Materials { get; }

        public FloorData(Model model, string name, string shortName, bool opening, Materials materials)
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
