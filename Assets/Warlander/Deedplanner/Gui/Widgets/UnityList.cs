using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Gui.Widgets
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
                return listElementsParent.GetComponentsInChildren<UnityListElement>().Select(element => element.Value).ToArray();
            }
        }

        /// <summary>
        /// See returns for notes on returned value
        /// </summary>
        /// <param name="value">value to add to the list</param>
        /// <returns>Created list element from prefab (useful if any further modification is needed)</returns>
        public UnityListElement Add(object value)
        {
            UnityListElement newElement = Instantiate(listElementPrefab, listElementsParent);
            newElement.Toggle.group = toggleGroup;
            newElement.Value = value;
            newElement.Toggle.onValueChanged.AddListener(toggled =>
            {
                if (toggled)
                {
                    SelectedValue = newElement.Value;
                    ValueChanged?.Invoke(this, newElement.Value);
                }
            });

            if (GetComponentsInChildren<UnityListElement>().Length == 1)
            {
                SelectedValue = newElement.Value;
                newElement.Toggle.isOn = true;
            }

            return newElement;
        }

        public void Clear()
        {
            foreach (Transform childTransform in listElementsParent.transform)
            {
                DestroyImmediate(childTransform.gameObject);
            }
        }

    }
}