using UnityEngine;
using Warlander.Deedplanner.Graphics;

namespace Warlander.Deedplanner.Data.Decorations
{
    public class DecorationData : ScriptableObject
    {

        public Model Model { get; private set; }
        public string Name { get; private set; }
        public string ShortName { get; private set; }
        public string Type { get; private set; }

        public bool CenterOnly { get; private set; }
        public bool CornerOnly { get; private set; }
        public bool Floating { get; private set; }
        public bool Tree { get; private set; }
        public bool Bush { get; private set; }

        public Materials Materials { get; private set; }

        public void Initialize(Model model, string name, string shortName, string type, bool centerOnly, bool cornerOnly, bool floating, bool tree, bool bush, Materials materials)
        {
            Model = model;
            Name = name;
            ShortName = shortName;
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
