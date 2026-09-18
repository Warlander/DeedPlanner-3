using System;
using Warlander.Deedplanner.Bridges;

namespace Warlander.Deedplanner.Editing
{
    public interface IBridgePavingStroke
    {
        void ForEachSymmetricPart(BridgePart part, Action<BridgePart> action);
    }
}
