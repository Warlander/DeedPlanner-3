using UnityEngine;
using Warlander.Deedplanner.Graphics;

namespace Warlander.Deedplanner.Data.Decorations
{
    public class DecorationData
    {
        public Model Model { get; }
        public string Name { get; }
        public string ShortName { get; }
        public string[][] Categories { get; }
        public string Type { get; }

        public bool CenterOnly { get; }
        public bool CornerOnly { get; }
        public bool Floating { get; }
        public bool Tree { get; }
        public bool Bush { get; }

        public Materials Materials { get; }

        public DecorationData(Model model, string name, string shortName, string[][] categories, string type,
            bool centerOnly, bool cornerOnly, bool floating, bool tree, bool bush, Materials materials)
        {
            Model = model;
            Name = name;
            ShortName = shortName;
            Categories = categories;
            Type = type;
            CenterOnly = centerOnly;
            CornerOnly = cornerOnly;
            Floating = floating;
            Tree = tree;
            Bush = bush;

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
