using System.Xml;

namespace Warlander.Deedplanner.Rendering.Assets
{
    public interface ITextureReferenceFactory
    {
        void LoadTextureDefinitions(XmlDocument document);

        TextureReference GetTextureReference(string location);
        TextureReference GetTextureReference(XmlElement element);
    }
}
