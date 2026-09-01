using UnityEngine;
using Warlander.Deedplanner.Graphics;

namespace Warlander.Deedplanner.Domain.Entities.Floors
{
    public class FloorData
    {
        public string Name { get; }
        public string ShortName { get; }
        public string[][] Categories { get; }
        public ModelHandle Model { get; }
        public bool Opening { get; }
        public bool SupportsDock { get; }
        public Materials Materials { get; }

        public FloorData(ModelHandle model, string name, string shortName, string[][] categories, bool opening, bool supportsDock, Materials materials)
        {
            Model = model;
            Name = name;
            ShortName = shortName;
            Categories = categories;
            Opening = opening;
            SupportsDock = supportsDock;
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
