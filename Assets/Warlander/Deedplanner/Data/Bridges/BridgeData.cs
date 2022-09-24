using System.Collections.Generic;

namespace Warlander.Deedplanner.Data.Bridges
{
    public class BridgeData
    {
        public string Name { get; }
        public string ShortName { get; }
        public int MaxWidth { get; }
        public int SupportHeight { get; }

        private readonly BridgePartData[] partsData;
        private readonly BridgeType[] allowedTypes;
        private Dictionary<BridgePartSide, Materials> sidesCost;
        
        public BridgeData(string name, string shortName, int maxWidth, int supportHeight, 
            BridgePartData[] partsData, BridgeType[] allowedTypes, Dictionary<BridgePartSide, Materials> sidesCost)
        {
            Name = name;
            ShortName = shortName;
            MaxWidth = maxWidth;
            SupportHeight = supportHeight;

            this.partsData = partsData;
            this.allowedTypes = allowedTypes;
            this.sidesCost = sidesCost;
        }
    }
}