using System.Xml;

namespace Warlander.Deedplanner.Graphics
{
    public interface ITextureReferenceFactory
    {
        TextureReference GetTextureReference(string location);
        TextureReference GetTextureReference(XmlElement element);
    }
}
