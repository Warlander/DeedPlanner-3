using System;

namespace Warlander.Deedplanner.Caves
{
    public sealed class CaveRenderOptions : ICaveRenderOptions
    {
        public event Action Changed = delegate { };

        private bool _cullingEnabled = true;

        public bool IsCullingEnabled()
        {
            return _cullingEnabled;
        }

        public void SetCullingEnabled(bool enabled)
        {
            if (_cullingEnabled == enabled)
            {
                return;
            }

            _cullingEnabled = enabled;
            Changed();
        }
    }
}
