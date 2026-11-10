using TMPro;
using UnityEngine;

namespace Warlander.Deedplanner.Editing
{
    public class AreaSelectionBadgeView : MonoBehaviour
    {
        [SerializeField] private RectTransform _rect;
        [SerializeField] private TMP_Text _label;

        public void ShowDimensions(Vector2Int dimensions, Rect selection, Rect viewport)
        {
            _label.SetText("{0} × {1} tiles", dimensions.x, dimensions.y);
            float width = Mathf.Min(_label.preferredWidth + 24f, viewport.width - 12f);
            const float height = 30f;
            _rect.sizeDelta = new Vector2(width, height);
            float x = Mathf.Clamp(selection.xMin, viewport.xMin + 6f, viewport.xMax - width - 6f);
            float top = selection.yMin - 6f;
            if (top - height < viewport.yMin + 6f) top = selection.yMax + height + 6f;
            top = Mathf.Clamp(top, viewport.yMin + height + 6f, viewport.yMax - 6f);
            _rect.anchoredPosition = new Vector2(x, top);
        }
    }
}
