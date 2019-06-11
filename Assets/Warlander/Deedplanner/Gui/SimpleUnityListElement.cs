using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Gui
{
    public class SimpleUnityListElement : UnityListElement
    {

        [SerializeField]
        private TextMeshProUGUI text = null;
        [SerializeField]
        private Toggle toggle = null;

        private object value;

        public override object Value {
            get {
                return value;
            }
            set {
                this.value = value;
                text.SetText(value.ToString());
            }
        }

        public override Toggle Toggle {
            get {
                return toggle;
            }
        }
    }
}
