using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Gui.Widgets
{
    public abstract class UnityListElement : MonoBehaviour
    {
        public abstract Toggle Toggle { get; }
        public abstract object Value { get; set; }
    }
}