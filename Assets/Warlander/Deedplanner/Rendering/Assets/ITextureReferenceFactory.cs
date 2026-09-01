using System.Xml;

namespace Warlander.Deedplanner.Rendering.Assets
{
    public interface ITextureReferenceFactory
    {
        TextureReference GetTextureReference(string location);
        TextureReference GetTextureReference(XmlElement element);
    }
}
