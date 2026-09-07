using UnityEngine;

namespace Warlander.Deedplanner.Rendering.Assets
{
    public interface ISharedMaterials
    {
        Material SimpleDrawingMaterial { get; }
        Material SimpleSubtleDrawingMaterial { get; }
        Material TerrainMaterial { get; }
        Material CaveMaterial { get; }
        Material GhostMaterial { get; }
    }
}
