using TMPro;
using UnityEngine;

namespace Warlander.Deedplanner.Gui.Tooltips
{
    public class TooltipTextBlock : TooltipContentBlock
    {
        [SerializeField] private TMP_Text label;

        public void SetText(string text)
        {
            label.text = text;
        }
    }
}
