using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    public interface ISharedMaterials
    {
        Material SimpleDrawingMaterial { get; }
        Material SimpleSubtleDrawingMaterial { get; }
        Material TerrainMaterial { get; }
        Material GhostMaterial { get; }
    }
}
