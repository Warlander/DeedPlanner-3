using TMPro;
using UnityEngine;

namespace Warlander.Deedplanner.Utils
{
    [RequireComponent((typeof(TMP_InputField)))]
    public class BetterIntegerInputField : MonoBehaviour
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
                result = 0;
            }

            inputField.text = result.ToString();
        }
        
    }
}