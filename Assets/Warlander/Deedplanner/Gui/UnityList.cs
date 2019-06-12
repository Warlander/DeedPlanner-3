using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace Warlander.Deedplanner.Gui
{
    public delegate void UnityListValueChangedHandler(object sender, object value);

    public class UnityList : MonoBehaviour
    {

        public event UnityListValueChangedHandler ValueChanged;

        // we want these fields to be settable via inspector, but not via code
        [SerializeField]
        private ToggleGroup toggleGroup = null;
        [SerializeField]
        private RectTransform listElementsParent = null;
        [SerializeField]
        private UnityListElement listElementPrefab = null;

        public object SelectedValue { get; private set; }

        public UnityListElement SelectedElement => toggleGroup.ActiveToggles().FirstOrDefault().GetComponent<UnityListElement>();

        public object[] Values {
            get {
                return GetComponentsInChildren<UnityListElement>().Select((element) => element.Value).ToArray();
            }
        }

        /// <summary>
        /// See returns for notes on returned value
        /// </summary>
        /// <param name="value">value to add to the list</param>
        /// <returns>Created list element from prefab (useful if any further modification is needed)</returns>
        public UnityListElement Add(object value)
        {
            UnityListElement newElement = Instantiate(listElementPrefab);
            newElement.Toggle.group = toggleGroup;
            newElement.Value = value;
            newElement.Toggle.onValueChanged.AddListener(toggled =>
            {
                if (toggled)
                {
                    SelectedValue = newElement.Value;
                }
                if (toggled && ValueChanged != null)
                {
                    ValueChanged(this, newElement.Value);
                }
            });
            newElement.transform.SetParent(listElementsParent);

            if (GetComponentsInChildren<UnityListElement>().Length == 1)
            {
                SelectedValue = newElement.Value;
                newElement.Toggle.isOn = true;
            }

            return newElement;
        }

    }
}