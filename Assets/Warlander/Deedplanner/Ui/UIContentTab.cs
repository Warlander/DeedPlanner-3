using UnityEngine;
using Warlander.Deedplanner.Editing;

namespace Warlander.Deedplanner.Ui
{
    [RequireComponent(typeof(CanvasGroup))]
    public class UIContentTab : MonoBehaviour
    {
        [SerializeField] private Tab tab = default;

        public Tab Tab => tab;

        private CanvasGroup fadeGroup;
        
        public CanvasGroup FadeGroup
        {
            get
            {
                if (!fadeGroup)
                {
                    fadeGroup = GetComponent<CanvasGroup>();
                }

                return fadeGroup;
            }
        }
    }
}
