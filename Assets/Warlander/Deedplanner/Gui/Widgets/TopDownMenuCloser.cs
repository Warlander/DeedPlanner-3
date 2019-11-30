using System;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Gui.Widgets
{
    [RequireComponent(typeof(Button))]
    public class TopDownMenuCloser : MonoBehaviour
    {
        private void Awake()
        {
            TopDownMenu parentMenu = GetComponentInParent<TopDownMenu>();
            if (!parentMenu)
            {
                Debug.LogWarning("Top down menu button not child of top down menu");
                return;
            }
            
            Button button = GetComponent<Button>();
            button.onClick.AddListener(() => parentMenu.HideMenu());
        }
    }
}