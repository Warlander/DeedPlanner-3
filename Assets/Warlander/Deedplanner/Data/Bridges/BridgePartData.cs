using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Graphics;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Data.Bridges
{
    public class BridgePartData
    {
        private readonly Dictionary<BridgePartSide, Model> models = new Dictionary<BridgePartSide, Model>();
        private readonly TextureReference _uiSpriteReference;
        private readonly Materials materials;
        
        public BridgePartType PartType { get; }

        public BridgePartData(XmlElement element)
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
                            models.Add(BridgePartSide.LEFT, new Model(child, LayerMasks.BridgeLayer));
                            models.Add(BridgePartSide.RIGHT, new Model(child, new Vector3(-1, 1, 1), LayerMasks.BridgeLayer));
                        }
                        else if (sideParseSuccess)
                        {
                            models.Add(side, new Model(child, LayerMasks.BridgeLayer));
                        }
                        else
                        {
                            Debug.LogError($"Bridge side enum parsing fail: {sideString}");
                        }
                        break;
                    case "tex":
                        uiTex = TextureReference.GetTextureReference(child);
                        break;
                    case "materials":
                        materials = new Materials(child);
                        break;
                }
            }

            _uiSpriteReference = uiTex;
        }

        public Model GetModel(BridgePartSide side)
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