using UnityEngine;

namespace Warlander.SpritePacker.Editor
{
    public class SpritePackerSettings
    {
        /// <summary>
        /// Can be null - in which case new atlas will be generated.
        /// </summary>
        public Texture2D Atlas { get; set; }
        
        public string OutputPath { get; set; }
        
        public Texture2D[] TexturesToPack { get; set; }
        
    }
}