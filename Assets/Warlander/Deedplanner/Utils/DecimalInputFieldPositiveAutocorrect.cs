using TMPro;
using UnityEngine;

namespace Warlander.Deedplanner.Utils
{
    [RequireComponent((typeof(TMP_InputField)))]
    public class DecimalInputFieldPositiveAutocorrect : MonoBehaviour
    {
        
        private TMP_InputField inputField;
        
        private void Awake()
        {
            inputField = GetComponent<TMP_InputField>();
            inputField.onValueChanged.AddListener(OnTextChanged);
        }
        
        private void OnTextChanged(string content)
        {
            string text = content.Replace("-", "");
            
            inputField.text = text;
        }
    }
}