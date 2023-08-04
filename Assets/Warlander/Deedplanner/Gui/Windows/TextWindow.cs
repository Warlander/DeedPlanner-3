using TMPro;
using UnityEngine;

namespace Warlander.Deedplanner.Gui.Windows
{
    public class TextWindow : MonoBehaviour
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_InputField _inputField;

        public void ShowText(string title, string text)
        {
            _titleText.text = title;
            _inputField.text = text;
        }
    }
}