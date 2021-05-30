using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SunSlider : MonoBehaviour
{
    [SerializeField] private Slider sunSlider;
    [SerializeField] private Light sunSource;

    Vector3 angle;

    void Start()
    {
        angle = sunSource.transform.localEulerAngles;

        sunSlider.onValueChanged.AddListener((value) => {
            sunSource.transform.localEulerAngles = new Vector3(angle.x, value, angle.z);
        });
    }
}
