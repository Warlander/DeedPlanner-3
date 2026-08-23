using Warlander.Deedplanner.Graphics;

namespace Warlander.Deedplanner.Data.Docks
{
    public class DockSupportData
    {
        public string Name { get; }
        public string ShortName { get; }
        public Model BaseModel { get; }
        public Model ExtensionModel { get; }

        public bool HasExtension => ExtensionModel != null;

        public DockSupportData(string name, string shortName, Model baseModel, Model extensionModel)
        {
            Name = name;
            ShortName = shortName;
            BaseModel = baseModel;
            ExtensionModel = extensionModel;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
