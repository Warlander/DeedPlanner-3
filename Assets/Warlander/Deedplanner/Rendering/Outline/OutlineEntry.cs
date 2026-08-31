using UnityEngine;

namespace Warlander.Deedplanner.Rendering.Outline
{
    public readonly struct OutlineEntry
    {
        public readonly Renderer[] Renderers;
        public readonly OutlineType Type;

        public OutlineEntry(Renderer[] renderers, OutlineType type)
        {
            Renderers = renderers;
            Type = type;
        }
    }
}
