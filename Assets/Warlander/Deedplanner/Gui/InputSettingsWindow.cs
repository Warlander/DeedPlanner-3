using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Warlander.Deedplanner.Gui
{
    public class InputSettingsWindow : MonoBehaviour
    {

        private TMP_InputField[] dummyInputs = new TMP_InputField[] { };

        private Dictionary<string, string> keyEvents = new Dictionary<string, string>(){ };


        private Transform inputContentHolder;
        /*
         * Move Forward R
         * Move Backwards F
         * Move Up E
         * Move Down Q
         */

        private void Start()
        {
            keyEvents.Add("negativeX", "Reverse");
            keyEvents.Add("negativeY", "Downward");
            keyEvents.Add("positiveX", "Forward");
            keyEvents.Add("positiveY", "Upward");

            inputContentHolder = GameObject.Find("inputSettingvGroup").transform;
            GameObject dummyInput = GameObject.Find("dummyInputSettingGroup");

            //clone dummy input to requried fields
            foreach (var key in keyEvents)
            {
                GameObject clone = Instantiate(dummyInput, inputContentHolder);

                clone.name = key.Key;

                Debug.Log(key.Value);
                Transform tr = clone.transform;
                var go = tr.Find("dummyText");
                TMP_Text t = go.GetComponent<TMP_Text>();
                t.text = key.Value;
            }

            var saveButton = inputContentHolder.Find("Save Button");

            //save button to bottom of hierarchy 
            saveButton.transform.SetSiblingIndex(1000);

            //destroy dummy inputs
            Destroy(dummyInput);

        }

        private void Awake()
        {
            //TMP_InputField duplicate = Instantiate(dummyField);




        }

        public void Update()
        {
            bool shiftDown = false;

            //clone dummy input to requried fields
            foreach (var key in keyEvents)
            {
                var text = "";

                Transform fieldGroup = inputContentHolder.Find(key.Key).transform;
                var i = fieldGroup.Find("InputField (TMP)");

                TMP_InputField tInput = i.GetComponent<TMP_InputField>();

                if (tInput.isFocused)
                {
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        shiftDown = true;
                    }
                    if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift))
                    {
                        shiftDown = false;
                    }

                    if (Input.anyKey && !Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2))//TODO: Add functionality to detect touch
                    {
                        if (Input.inputString != "") {
                            Debug.Log("Key was held down for " + key.Key + " " + Input.inputString);
                            if(shiftDown)
                            {
                                text = "Shift";
                            }
                            if(text != "")
                            {
                                text += " + " + Input.inputString;
                            } else
                            {
                                text = Input.inputString;
                            }
                        }
                    }

                }

                tInput.text = text;

            }
        }

        private void OnEnable()
        {
            //ResetState();
            //ApplyProperties();
        }


        /* 
         * Used to reset window state
         */
        private void ResetState()
        {
            
        }


        /*
         * used when save chaegs is made
         */
        private void ApplyProperties()
        {
            
        }

        public void OnGuiScaleChanged()
        {
        }

        public void OnSaveButton()
        {

        }

        private void SaveProperties()
        {
            
        }
    }
}
