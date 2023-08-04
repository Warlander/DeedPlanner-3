using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Logic;
using Warlander.UI.Windows;
using Zenject;

namespace Warlander.Deedplanner.Gui.Windows
{
    public class ResizeWindow : MonoBehaviour
    {
        [Inject] private Window _window;

        [SerializeField] private Button _acceptButton;
        
        [SerializeField] private TMP_InputField leftInput;
        [SerializeField] private TMP_InputField rightInput;
        [SerializeField] private TMP_InputField bottomInput;
        [SerializeField] private TMP_InputField topInput;
        
        [SerializeField] private TMP_Text originalWidthText;
        [SerializeField] private TMP_Text originalHeightText;
        [SerializeField] private TMP_Text newWidthText;
        [SerializeField] private TMP_Text newHeightText;

        private void Start()
        {
            Map map = GameManager.Instance.Map;
            originalWidthText.text = "Original Width<br>" + map.Width;
            originalHeightText.text = "Original Height<br>" + map.Height;
            leftInput.text = "0";
            rightInput.text = "0";
            bottomInput.text = "0";
            topInput.text = "0";
            
            OnInputsUpdate();
            
            _acceptButton.onClick.AddListener(AcceptButtonOnClick);
            leftInput.onValueChanged.AddListener(InputOnValueChanged);
            rightInput.onValueChanged.AddListener(InputOnValueChanged);
            bottomInput.onValueChanged.AddListener(InputOnValueChanged);
            topInput.onValueChanged.AddListener(InputOnValueChanged);
        }

        private void AcceptButtonOnClick()
        {
            int left = 0;
            int.TryParse(leftInput.text, out left);
            int right = 0;
            int.TryParse(rightInput.text, out right);
            int bottom = 0;
            int.TryParse(bottomInput.text, out bottom);
            int top = 0;
            int.TryParse(topInput.text, out top);
            
            GameManager.Instance.ResizeMap(left, right, bottom, top);
            _window.Close();
        }

        private void InputOnValueChanged(string input)
        {
            OnInputsUpdate();
        }
        
        private void OnInputsUpdate()
        {
            Map map = GameManager.Instance.Map;
            
            int left = 0;
            int.TryParse(leftInput.text, out left);
            int right = 0;
            int.TryParse(rightInput.text, out right);
            int bottom = 0;
            int.TryParse(bottomInput.text, out bottom);
            int top = 0;
            int.TryParse(topInput.text, out top);

            int widthChange = left + right;
            int heightChange = bottom + top;
            
            int newWidth = map.Width + widthChange;
            int newHeight = map.Height + heightChange;
            
            newWidthText.text = "New Width<br>" + newWidth;
            newHeightText.text = "New Height<br>" + newHeight;
        }
    }
}