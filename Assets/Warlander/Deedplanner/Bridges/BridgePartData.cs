using System;
using Warlander.Deedplanner.Domain;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Graphics;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Bridges
{
    public class BridgePartData
    {
        private readonly Dictionary<BridgePartSide, ModelHandle> models = new Dictionary<BridgePartSide, ModelHandle>();
        private readonly TextureReference _uiSpriteReference;
        private readonly Materials materials;

        public BridgePartType PartType { get; }

        public BridgePartData(IWurmAssetFacade assetFacade, XmlElement element)
        {
            string typeString = element.GetAttribute("type");
            bool typeParseSuccess = Enum.TryParse(typeString, true, out BridgePartType type);
            if (!typeParseSuccess)
            {
                throw new ArgumentException($"Invalid bridge part type: {PartType}");
            }

            PartType = type;
            
            TextureReference uiTex = null;
            
            foreach (XmlElement child in element)
            {
                switch (child.LocalName)
                {
                    case "model":
                        string sideString = child.GetAttribute("tag");
                        bool sideParseSuccess = Enum.TryParse(sideString, true, out BridgePartSide side);

                        if (sideString.Equals("side", StringComparison.OrdinalIgnoreCase))
                        {
                            models.Add(BridgePartSide.LEFT, assetFacade.GetModel(child, LayerMasks.BridgeLayer));
                            models.Add(BridgePartSide.RIGHT, assetFacade.GetModel(child, LayerMasks.BridgeLayer));
                        }
                        else {
                            models.Add(side, assetFacade.GetModel(child, LayerMasks.BridgeLayer));
                        }
                        break;
                    case "tex":
                        uiTex = assetFacade.GetTextureReference(child);
                        break;
                    case "materials":
                        materials = new Materials(child);
                        break;
                }
            }

            _uiSpriteReference = uiTex;
        }

        public ModelHandle GetModel(BridgePartSide side)
        {
            return models[side];
        }

        public TextureReference GetUISprite()
        {
            return _uiSpriteReference;
        }
        
        public void AddCost(Materials existingMaterials)
        {
            existingMaterials.Add(materials);
        }
    }
}
