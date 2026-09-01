using UnityEngine;
using Warlander.Deedplanner.Rendering.Assets;

namespace Warlander.Deedplanner.Domain.Entities.Walls
{
    public class WallData
    {
        public ModelHandle BottomModel { get; }
        public ModelHandle NormalModel { get; }
        public string Name { get; }
        public string ShortName { get; }
        public string[][] Categories { get; }
        public Color Color { get; }
        public float Scale { get; }
        public bool HouseWall { get; }
        public bool Arch { get; }
        public bool ArchBuildable { get;}
    
        public TextureReference Icon { get; }

        public Materials Materials { get; }

        public WallData(ModelHandle bottomModel, ModelHandle normalModel, string name, string shortName, string[][] categories,
            Color color, float scale, bool houseWall, bool arch, bool archBuildable, Materials materials,
            TextureReference icon)
        {
            BottomModel = bottomModel;
            NormalModel = normalModel;
            Name = name;
            ShortName = shortName;
            Categories = categories;
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
