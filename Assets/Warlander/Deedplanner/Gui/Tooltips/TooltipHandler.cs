using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Warlander.Deedplanner.Gui.Widgets;
using Warlander.UI.Windows;
using Zenject;

namespace Warlander.Deedplanner.Gui.Tooltips
{
    public class TooltipHandler : IInitializable, ILateTickable
    {
        private readonly WindowCoordinator _windowCoordinator;

        [Inject]
        public TooltipHandler(WindowCoordinator windowCoordinator)
        {
            _windowCoordinator = windowCoordinator;
        }
        
        private readonly List<TooltipText> _scheduledTooltipTexts = new List<TooltipText>();

        private Tooltip _tooltip;
        
        void IInitializable.Initialize()
        {
            _tooltip = _windowCoordinator.CreateWindow<Tooltip>(WindowNames.TooltipWindow);
        }
        
        /// <summary>
        /// Shows tooltip text next frame.
        /// </summary>
        public void ShowTooltipText(string text, int priority = 0)
        {
            _scheduledTooltipTexts.Add(new TooltipText(priority, text));
        }

        void ILateTickable.LateTick()
        {
            if (_scheduledTooltipTexts.Count == 0)
            {
                _tooltip.Value = "";
            }
            else
            {
                StringBuilder finalTooltip = new StringBuilder();
                _scheduledTooltipTexts.Sort((t1, t2) => t1.Priority - t2.Priority);
                for (int i = 0; i < _scheduledTooltipTexts.Count; i++)
                {
                    TooltipText tooltipText = _scheduledTooltipTexts[i];
                    finalTooltip.Append(tooltipText.Text);

                    if (i != _scheduledTooltipTexts.Count - 1)
                    {
                        finalTooltip.Append("\n");
                    }
                }

                _tooltip.Value = finalTooltip.ToString();
                _scheduledTooltipTexts.Clear();
            }
        }

        private struct TooltipText
        {
            private int _priority;
            private string _text;

            public int Priority => _priority;
            public string Text => _text;

            public TooltipText(int priority, string text)
            {
                _priority = priority;
                _text = text;
            }
        }
    }
}