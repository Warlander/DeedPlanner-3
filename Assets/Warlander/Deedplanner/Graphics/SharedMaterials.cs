using UnityEngine;

namespace Warlander.Deedplanner.Graphics
{
    [CreateAssetMenu(fileName = "SharedMaterials", menuName = "DeedPlanner/SharedMaterials")]
    public class SharedMaterials : ScriptableObject, ISharedMaterials
    {
        [SerializeField] private Material _simpleDrawingMaterial;
        [SerializeField] private Material _simpleSubtleDrawingMaterial;
        [SerializeField] private Material _terrainMaterial;
        [SerializeField] private Material _ghostMaterial;

        public Material SimpleDrawingMaterial => _simpleDrawingMaterial;
        public Material SimpleSubtleDrawingMaterial => _simpleSubtleDrawingMaterial;
        public Material TerrainMaterial => _terrainMaterial;
        public Material GhostMaterial => _ghostMaterial;
    }
}
