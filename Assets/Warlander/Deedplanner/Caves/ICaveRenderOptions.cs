using System;

namespace Warlander.Deedplanner.Caves
{
    public interface ICaveRenderOptions
    {
        event Action Changed;
        bool IsCullingEnabled();
        void SetCullingEnabled(bool enabled);
    }
}
