using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Warlander.Deedplanner.Utils
{
    public interface IXMLSerializable
    {

        void Serialize(XmlDocument document, XmlElement localRoot);

    }
}
