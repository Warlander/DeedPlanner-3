using TMPro;
using UnityEngine;

namespace Warlander.Deedplanner.Utils
{
    [RequireComponent(typeof(TMP_InputField))]
    public class IntegerInputFieldAutocorrect : MonoBehaviour
    {

        private TMP_InputField inputField;
        
        private void Awake()
        {
            inputField = GetComponent<TMP_InputField>();
            inputField.onValueChanged.AddListener(OnTextChanged);
        }

        private void OnTextChanged(string content)
        {
            int result;
            bool parsed = int.TryParse(inputField.text, out result);

            if (!parsed)
            {
                if (inputField.text == "-")
                {
                    inputField.text = "-0";
                    inputField.caretPosition = 1;
                    inputField.selectionAnchorPosition = 1;
                    inputField.selectionFocusPosition = 2;
                }
                else
                {
                    inputField.text = "0";
                }
            }
            else if (inputField.text != "-0")
            {
                inputField.text = result.ToString();
            }
        }
        
    }
}