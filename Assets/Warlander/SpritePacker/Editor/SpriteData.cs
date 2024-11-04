using UnityEditor;

namespace Warlander.SpritePacker.Editor
{
    public class SpriteData
    {
        public GUID GUID { get; }
        public string Name { get; }

        public SpriteData(GUID guid, string name)
        {
            GUID = guid;
            Name = name;
        }
    }
}