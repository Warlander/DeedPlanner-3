using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;

namespace Warlander.Deedplanner.Utils
{
    public class TextureReference
    {

        private static Dictionary<string, TextureReference> references = new Dictionary<string, TextureReference>();

        public static TextureReference GetTextureReference(string location)
        {
            TextureReference reference;

            if (references.ContainsKey(location))
            {
                reference = references[location];
                return reference;
            }

            reference = new TextureReference(location);
            references[location] = reference;
            return reference;
        }

        public static TextureReference GetTextureReference(XmlElement element)
        {
            string location = element.GetAttribute("location");
            return GetTextureReference(location);
        }

        private Texture2D texture;

        public string Location { get; private set; }

        public Texture2D Texture {
            get {
                if (texture)
                {
                    return texture;
                }

                texture = WomModelLoader.LoadTexture(Application.streamingAssetsPath + "/" + Location);
                return texture;
            }
        }

        private TextureReference(string location)
        {
            Location = location;
        }

    }
}
