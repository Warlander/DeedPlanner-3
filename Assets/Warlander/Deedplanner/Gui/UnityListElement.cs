using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Gui
{
    public abstract class UnityListElement : MonoBehaviour
    {
        public abstract Toggle Toggle { get; }
        public abstract object Value { get; set; }
    }
}