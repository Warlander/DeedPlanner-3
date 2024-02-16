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
        [Inject] private GameManager _gameManager;

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
            Map map = _gameManager.Map;
            originalWidthText.text = "Original Width<br><b>" + map.Width + "</b>";
            originalHeightText.text = "Original Height<br><b>" + map.Height + "</b>";
            leftInput.text = "";
            rightInput.text = "";
            bottomInput.text = "";
            topInput.text = "";
            newWidthText.text = "";
            newHeightText.text = "";
            
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
            
            _gameManager.ResizeMap(left, right, bottom, top);
            _window.Close();
        }

        private void InputOnValueChanged(string input)
        {
            OnInputsUpdate();
        }
        
        private void OnInputsUpdate()
        {
            Map map = _gameManager.Map;
            
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

            if(widthChange != 0)
            {
                originalWidthText.text = "Original Width<br><b><color=#821010>" + map.Width + "</color></b>";
                newWidthText.text = "New Width<br><b><color=#0F6C18>" + newWidth + "</color></b>";
            }
            else
            {
                originalWidthText.text = "Original Width<br><b>" + map.Width + "</b>";
                newWidthText.text = "";
            }

            if(heightChange != 0)
            {
                originalHeightText.text = "Original Height<br><b><color=#821010>" + map.Height + "</color></b>";
                newHeightText.text = "New Height<br><b><color=#0F6C18>" + newHeight + "</color></b>";
            }
            else
            {
                originalHeightText.text = "Original Height<br><b>" + map.Height + "</b>";
                newHeightText.text = "";
            }
        }
    }
}