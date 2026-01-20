using System;

namespace Warlander.Deedplanner.Settings
{
    public class MapRenderSettings : IMapRenderSettingsRetriever, IMapRenderSettingsSetter
    {
        public event Action Changed = delegate { };
        
        private bool renderDecorations = true;
        private bool renderTrees = true;
        private bool renderBushes = true;
        private bool renderShips = true;

        public bool RenderDecorations
        {
            get => renderDecorations;
            set
            {
                renderDecorations = value;
                Changed.Invoke();
            }
        }

        public bool RenderTrees
        {
            get => renderTrees;
            set
            {
                renderTrees = value;
                Changed.Invoke();
            }
        }

        public bool RenderBushes
        {
            get => renderBushes;
            set
            {
                renderBushes = value;
                Changed.Invoke();
            }
        }

        public bool RenderShips
        {
            get => renderShips;
            set
            {
                renderShips = value;
                Changed.Invoke();
            }
        }
    }
}