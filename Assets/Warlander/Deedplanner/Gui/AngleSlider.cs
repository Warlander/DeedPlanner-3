using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AngleSlider : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private Light source;

    Vector3 angle;

    void Start()
    {
        angle = source.transform.localEulerAngles;

        slider.onValueChanged.AddListener((value) => {
            source.transform.localEulerAngles = new Vector3(angle.x, value, angle.z);
        });
    }
}
