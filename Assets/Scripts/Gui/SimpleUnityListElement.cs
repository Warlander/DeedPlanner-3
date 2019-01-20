using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimpleUnityListElement : UnityListElement
{

    [SerializeField]
    private TextMeshProUGUI text;
    [SerializeField]
    private Toggle toggle;

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
