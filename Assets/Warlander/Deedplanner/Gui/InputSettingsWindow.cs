using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Warlander.Deedplanner.Gui
{
    public class InputSettingsWindow : MonoBehaviour
    {

        private TMP_InputField[] dummyInputs = new TMP_InputField[] { };
        [SerializeField] private TMP_InputField dummyField = null;

        private Dictionary<string, string> keyEvents = new Dictionary<string, string>(){ };

        /*
         * Move Forward R
         * Move Backwards F
         * Move Up E
         * Move Down Q
         */

        private void Awake()
        {
            keyEvents.Add("negativeX", "Reverse");
            keyEvents.Add("negativeY", "Downwards");
            keyEvents.Add("positiveX", "Forward");
            keyEvents.Add("positiveY", "Upwards");

            foreach(TMP_InputField input in dummyInputs)
            {
                
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
            //SaveProperties();
            //gameObject.SetActive(false);
        }

        private void SaveProperties()
        {
            
        }
    }
}
