using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Gui
{
    public class ResizeWindow : MonoBehaviour
    {

        [SerializeField] private TMP_InputField leftInput = null;
        [SerializeField] private TMP_InputField rightInput = null;
        [SerializeField] private TMP_InputField bottomInput = null;
        [SerializeField] private TMP_InputField topInput = null;
        
        [SerializeField] private TMP_Text originalWidthText = null;
        [SerializeField] private TMP_Text originalHeightText = null;
        [SerializeField] private TMP_Text newWidthText = null;
        [SerializeField] private TMP_Text newHeightText = null;

        private void OnEnable()
        {
            Map map = GameManager.Instance.Map;
            originalWidthText.text = "Original Width<br>" + map.Width;
            originalHeightText.text = "Original Height<br>" + map.Height;
            leftInput.text = "0";
            rightInput.text = "0";
            bottomInput.text = "0";
            topInput.text = "0";
            
            OnInputsUpdate();
        }

        public void OnInputsUpdate()
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
        
        public void OnAccept()
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
            gameObject.SetActive(false);
        }
        
    }
}