using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class UnityListElement : MonoBehaviour
{

    public abstract Toggle Toggle { get; }
    public abstract object Value { get; set; }

}
