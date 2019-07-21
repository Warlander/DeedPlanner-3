using System.Collections.Generic;
using UnityEngine;

namespace Warlander.Deedplanner.Gui.Widgets
{
    public abstract class UnityTreeNode : MonoBehaviour
    {

        public abstract string Value { get; set; }
        public abstract List<UnityListElement> Leaves { get; }
        public abstract List<UnityTreeNode> Branches { get; }

    }
}
