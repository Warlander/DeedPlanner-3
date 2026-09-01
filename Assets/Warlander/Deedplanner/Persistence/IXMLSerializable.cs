using System.Xml;

namespace Warlander.Deedplanner.Persistence
{
    public interface IXmlSerializable
    {
        void Serialize(XmlDocument document, XmlElement localRoot);
    }
}
