using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Warlander.Deedplanner.Graphics;

namespace Warlander.Deedplanner.Data.Bridges
{
    public class BridgeData
    {
        public string Name { get; }
        public int MaxWidth { get; }
        public int SupportHeight { get; }

        private readonly BridgePartData[] partsData;
        private readonly BridgeType[] allowedTypes;
        private Dictionary<BridgePartSide, Materials> sidesCost;
        
        public BridgeData(string name, int maxWidth, int supportHeight, 
            BridgePartData[] partsData, BridgeType[] allowedTypes, Dictionary<BridgePartSide, Materials> sidesCost)
        {
            Name = name;
            MaxWidth = maxWidth;
            SupportHeight = supportHeight;

            this.partsData = partsData;
            this.allowedTypes = allowedTypes;
            this.sidesCost = sidesCost;
        }

        public Model GetModelForPart(BridgePartType type, BridgePartSide side)
        {
            return GetDataForType(type).GetModel(side);
        }

        public TextureReference GetUISpriteForPart(BridgePartType type)
        {
            return GetDataForType(type).GetUISprite();
        }
        
        public Materials GetMaterialsForPart(BridgePartType type, BridgePartSide side)
        {
            BridgePartData data = GetDataForType(type);

            Materials materials = new Materials();
            data.AddCost(materials);
            materials.Add(sidesCost[side]);

            return materials;
        }

        public bool IsTypeAllowed(BridgeType type)
        {
            return allowedTypes.Contains(type);
        }

        private BridgePartData GetDataForType(BridgePartType type)
        {
            return partsData.First(part => part.PartType == type);
        }
    }
}