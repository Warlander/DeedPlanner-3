using UnityEngine;
using Warlander.Deedplanner.Graphics;

namespace Warlander.Deedplanner.Data.Walls
{
    public class WallData
    {
        public Model BottomModel { get; }
        public Model NormalModel { get; }
        public string Name { get; }
        public string ShortName { get; }
        public Color Color { get; }
        public float Scale { get; }
        public bool HouseWall { get; }
        public bool Arch { get; }
        public bool ArchBuildable { get;}
    
        public TextureReference Icon { get; }

        public Materials Materials { get; }

        public WallData(Model bottomModel, Model normalModel, string name, string shortName, Color color, float scale,
            bool houseWall, bool arch, bool archBuildable, Materials materials, TextureReference icon)
        {
            BottomModel = bottomModel;
            NormalModel = normalModel;
            Name = name;
            ShortName = shortName;
            Color = color;
            Scale = scale;
            HouseWall = houseWall;
            Arch = arch;
            ArchBuildable = archBuildable;
            Icon = icon;
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
