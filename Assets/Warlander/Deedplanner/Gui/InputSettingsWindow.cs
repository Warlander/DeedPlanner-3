using TMPro;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Warlander.Deedplanner.Gui
{
    public class InputSettingsWindow : MonoBehaviour
    {

        private TMP_InputField[] dummyInputs = new TMP_InputField[] { };

        private Transform inputContentHolder;
        /*
         * Move Forward R
         * Move Backwards F
         * Move Up E
         * Move Down Q
         */

        private void Start()
        {

            inputContentHolder = GameObject.Find("inputSettingvGroup").transform;
            GameObject dummyInput = GameObject.Find("dummyInputSettingGroup");

            //clone dummy input to requried fields
            foreach (var key in Enum.GetNames(typeof(Inputs)))
            {
                GameObject clone = Instantiate(dummyInput, inputContentHolder);

                clone.name = key;

                Transform tr = clone.transform;

                //var iField = tr.Find("InputField (TMP)");
                //iField.GetComponent<TMP_InputField>().OnChangeEvent.AddListener((string input) => { checkInputChange(); });

                var go = tr.Find("dummyText");
                TMP_Text t = go.GetComponent<TMP_Text>();
                t.text = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(key.ToLower());
            }

            var saveButton = inputContentHolder.Find("Save Button");

            //save button to bottom of hierarchy 
            saveButton.transform.SetSiblingIndex(1000);

            //destroy dummy inputs
            Destroy(dummyInput);

        }

        private void Awake()
        {
        }

        private Dictionary<string, bool> keysDown = new Dictionary<string, bool>(){ };
        private Dictionary<string, string> currentValues = new Dictionary<string, string>(){ };


        //TODO: there is a problem setting text to files with update..perhaps values should be saved to a dict and set at the end of method
        public void Update()
        {


            //TODO: Add functionality to detect touch

            keysDown.Clear();

            foreach (KeyCode kcode in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(kcode))
                {
                    keysDown.Add(kcode.ToString(), true);
                }
            }

            foreach (var key in Enum.GetNames(typeof(Inputs)))
            {
                
                Transform fieldGroup = inputContentHolder.Find(key).transform;
                var i = fieldGroup.Find("InputField (TMP)");

                TMP_InputField tInput = i.GetComponent<TMP_InputField>();

                tInput.text = currentValues.ContainsKey(key) ? currentValues[key] : "UNSET";
                //tInput.textComponent.SetText(currentValues.ContainsKey(key) ? currentValues[key] : "UNSET");

                if (tInput.isFocused)
                {

                    if(Input.GetKey(KeyCode.CapsLock) || Input.GetKey(KeyCode.Numlock) && !Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2))
                    {
                        return;
                    }

                    //clear field
                    if (Input.GetKeyDown(KeyCode.Escape))
                    {
                        currentValues.Remove(key);
                        tInput.text = "";
                        return;
                    }

                    //delete character
                    if (Input.GetKeyDown(KeyCode.Backspace))
                    {
                        keysDown.Remove(keysDown.Keys.Last());
                        return;
                    }

                    string newVal = String.Join("+", keysDown.Keys);

                    if (currentValues.ContainsKey(key))
                    {
                        if (currentValues[key] != newVal)
                        {
                            currentValues[key] = newVal;
                        }
                    }
                    else
                    {
                        currentValues.Add(key, String.Join("+", keysDown.Keys));
                    }
                }
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
