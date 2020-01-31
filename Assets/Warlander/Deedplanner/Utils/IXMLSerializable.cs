using System.Xml;

namespace Warlander.Deedplanner.Utils
{
    public interface IXmlSerializable
    {
        void Serialize(XmlDocument document, XmlElement localRoot);
    }
}
