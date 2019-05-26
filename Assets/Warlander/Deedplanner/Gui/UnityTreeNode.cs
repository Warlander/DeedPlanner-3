using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Gui
{
    public abstract class UnityTreeNode : MonoBehaviour
    {

        public abstract string Value { get; set; }
        public abstract List<UnityListElement> Leaves { get; }
        public abstract List<UnityTreeNode> Branches { get; }

    }
}
