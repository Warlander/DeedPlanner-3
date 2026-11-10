using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Ui.Widgets
{
    public class UnityList : MonoBehaviour, IEntryTooltipView
    {

        public event Action<object> ValueChanged;
        public event Action<string> TooltipRequested;

        // we want these fields to be settable via inspector, but not via code
        [SerializeField] private ToggleGroup toggleGroup = null;
        [SerializeField] private RectTransform listElementsParent = null;
        [SerializeField] private UnityListElement listElementPrefab = null;

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
            if (newElement is IconUnityListElement iconElement)
            {
                iconElement.TooltipRequested += OnTooltipRequested;
            }
            newElement.Toggle.onValueChanged.AddListener(toggled =>
            {
                if (toggled)
                {
                    SelectedValue = newElement.Value;
                    ValueChanged?.Invoke(newElement.Value);
                }
            });

            if (GetComponentsInChildren<UnityListElement>().Length == 1)
            {
                SelectedValue = newElement.Value;
                newElement.Toggle.isOn = true;
            }

            return newElement;
        }

        private void OnTooltipRequested(string text)
        {
            TooltipRequested?.Invoke(text);
        }

        public void Clear()
        {
            for (int i = listElementsParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(listElementsParent.GetChild(i).gameObject);
            }
        }

    }
}
