using System.Xml;

namespace Warlander.Deedplanner.Utils
{
    public interface IXMLSerializable
    {

        void Serialize(XmlDocument document, XmlElement localRoot);

    }
}
