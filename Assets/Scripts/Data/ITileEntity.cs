using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data
{

    public interface ITileEntity : IXMLSerializable
    {

        Materials Materials { get; }

    }

}
