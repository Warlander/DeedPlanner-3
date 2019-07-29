using TMPro;
using UnityEngine;

namespace Warlander.Deedplanner.Utils
{
    [RequireComponent(typeof(TMP_InputField))]
    public class JavaScriptPrompt : MonoBehaviour
    {

        [SerializeField] private string message = "Paste the content";
        [SerializeField] private string defaultInput = "";

        private TMP_InputField inputField;
        
        private void Awake()
        {
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                Destroy(this);
                return;
            }
            
            inputField = GetComponent<TMP_InputField>();
            inputField.onSelect.AddListener(OnInputSelected);
        }

        private void OnInputSelected(string content)
        {
            string result = JavaScriptUtils.PromptNative(message, defaultInput);
            inputField.text = result;
        }

    }
}