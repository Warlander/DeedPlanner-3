using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class UnityList : MonoBehaviour
{

    // we want these fields to be settable via inspector, but not via code
    [SerializeField]
    private ToggleGroup toggleGroup;
    [SerializeField]
    private RectTransform listElementsParent;
    [SerializeField]
    private UnityListElement listElementPrefab;

    public object SelectedValue {
        get {
            Toggle activeToggle = toggleGroup.ActiveToggles().FirstOrDefault();
            if (!activeToggle)
            {
                return default;
            }

            return activeToggle.GetComponent<UnityListElement>().Value;
        }
    }

    public UnityListElement SelectedElement {
        get {
            return toggleGroup.ActiveToggles().FirstOrDefault().GetComponent<UnityListElement>();
        }
    }

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
        newElement.transform.SetParent(listElementsParent);

        if (GetComponentsInChildren<UnityListElement>().Length == 1)
        {
            newElement.Toggle.isOn = true;
        }

        return newElement;
    }
}
