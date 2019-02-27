using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warlander.Deedplanner.Utils
{

    public delegate void GenericEventArgs<T>(T arg0);
    public delegate void GenericEventArgs<T0, T1>(T0 arg0, T1 arg1);
    public delegate void GenericEventArgs<T0, T1, T2>(T0 arg0, T1 arg1, T2 ar2);

}
