using System.Xml;
using UnityEngine;

namespace Warlander.Deedplanner.Rendering.Assets
{
    public interface IWurmAssetFacade
    {
        ModelHandle GetModel(XmlElement element, int layer = int.MaxValue);
        ModelHandle GetModel(XmlElement element, Vector3 scale, int layer = int.MaxValue);
        ModelHandle GetModel(string location, int layer = int.MaxValue);
        ModelHandle GetModel(string location, Vector3 scale, int layer = int.MaxValue);

        TextureReference GetTextureReference(XmlElement element);
        TextureReference GetTextureReference(string location);
    }
}
