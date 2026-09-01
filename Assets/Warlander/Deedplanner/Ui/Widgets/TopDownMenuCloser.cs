using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Ui;
using Warlander.Deedplanner.Logging;
using VContainer;

namespace Warlander.Deedplanner.Ui.Widgets
{
    [RequireComponent(typeof(Button))]
    public class TopDownMenuCloser : MonoBehaviour
    {
        [Inject] private UiLog _uiLog;

        private ICategoryLogger Logger => _uiLog.Logger;

        private void Start()
        {
            TopDownMenu parentMenu = GetComponentInParent<TopDownMenu>();
            if (!parentMenu)
            {
                Logger.Warning("Top down menu button not child of top down menu");
                return;
            }
            
            Button button = GetComponent<Button>();
            button.onClick.AddListener(() => parentMenu.HideMenu());
        }
    }
}
