using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Gui
{
    public class EditorAreaLayouterView : MonoBehaviour, IEditorAreaLayouterView
    {
        [SerializeField] private RectTransform horizontalBottomIndicatorHolder = null;
        [SerializeField] private RawImage[] screens = new RawImage[4];
        [SerializeField] private RectTransform horizontalBottomScreenHolder = null;
        [SerializeField] private RectTransform[] splits = new RectTransform[5];

        public void SetScreenVisible(int index, bool visible)
        {
            screens[index].gameObject.SetActive(visible);
        }

        public void SetBottomRowVisible(bool visible)
        {
            horizontalBottomIndicatorHolder.gameObject.SetActive(visible);
            horizontalBottomScreenHolder.gameObject.SetActive(visible);
        }

        public void SetSplitVisible(int index, bool visible)
        {
            splits[index].gameObject.SetActive(visible);
        }
    }
}
