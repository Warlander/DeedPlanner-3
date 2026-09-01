using System;
using System.Collections.Generic;
using System.Linq;
using Warlander.Deedplanner.Ui.Widgets;
using Warlander.UI;
using Warlander.UI.Windows;
using VContainer;
using VContainer.Unity;

namespace Warlander.Deedplanner.Ui.Tooltips
{
    public class TooltipHandler : IInitializable, ILateTickable
    {
        private readonly WindowCoordinator _windowCoordinator;
        private readonly IInterfaceVisibility _interfaceVisibility;

        [Inject]
        public TooltipHandler(WindowCoordinator windowCoordinator, IInterfaceVisibility interfaceVisibility)
        {
            _windowCoordinator = windowCoordinator;
            _interfaceVisibility = interfaceVisibility;
        }

        private readonly List<ScheduledContent> _scheduledContents = new List<ScheduledContent>();
        private readonly List<ITooltipContent> _sortedContents = new List<ITooltipContent>();

        private Tooltip _tooltip;

        void IInitializable.Initialize()
        {
            _tooltip = _windowCoordinator.CreateWindow<Tooltip>(WindowNames.TooltipWindow);
        }

        /// <summary>
        /// Shows tooltip text next frame. Blocks are laid out top to bottom by ascending priority.
        /// </summary>
        public void ShowTooltipText(string text, int priority = 0)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            TooltipTextBlock block = _tooltip.ClaimTextBlock();
            block.SetText(text);
            ShowTooltipContent(block, priority);
        }

        /// <summary>
        /// Returns the shared tooltip content block of the given type (e.g. SlopeGridView).
        /// Producers cache it, update its data and schedule it every frame while hovered.
        /// </summary>
        public T GetContent<T>() where T : TooltipContentBlock
        {
            return _tooltip.GetContent<T>();
        }

        /// <summary>
        /// Shows a tooltip content block next frame. Blocks are laid out top to bottom by ascending priority.
        /// </summary>
        public void ShowTooltipContent(ITooltipContent content, int priority = 0)
        {
            _scheduledContents.Add(new ScheduledContent(priority, content));
        }

        void ILateTickable.LateTick()
        {
            if (_interfaceVisibility.Visible == false)
            {
                _scheduledContents.Clear();
                _tooltip.SetContents(null);
                return;
            }

            if (_scheduledContents.Count == 0)
            {
                _tooltip.SetContents(null);
            }
            else
            {
                _sortedContents.AddRange(_scheduledContents
                    .OrderBy(scheduled => scheduled.Priority)
                    .Select(scheduled => scheduled.Content));
                _tooltip.SetContents(_sortedContents);
                _scheduledContents.Clear();
                _sortedContents.Clear();
            }
        }

        private struct ScheduledContent
        {
            private readonly int _priority;
            private readonly ITooltipContent _content;

            public int Priority => _priority;
            public ITooltipContent Content => _content;

            public ScheduledContent(int priority, ITooltipContent content)
            {
                _priority = priority;
                _content = content;
            }
        }
    }
}
