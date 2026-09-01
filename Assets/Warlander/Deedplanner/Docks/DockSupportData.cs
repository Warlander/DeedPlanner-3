using Warlander.Deedplanner.Domain;
using Warlander.Deedplanner.Graphics;

namespace Warlander.Deedplanner.Docks
{
    public class DockSupportData
    {
        public string Name { get; }
        public string ShortName { get; }
        public DockSupportType Type { get; }
        public ModelHandle BaseModel { get; }
        public ModelHandle ExtensionModel { get; }
        public Materials Materials { get; }

        public bool HasExtension => ExtensionModel != null;

        public DockSupportData(string name, string shortName, DockSupportType type, ModelHandle baseModel, ModelHandle extensionModel,
            Materials materials)
        {
            Name = name;
            ShortName = shortName;
            Type = type;
            BaseModel = baseModel;
            ExtensionModel = extensionModel;
            Materials = materials;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
