using UnityEngine;

namespace Warlander.Deedplanner.Rendering.Projectors
{
    [CreateAssetMenu(fileName = "MapProjectorPrefabs", menuName = "DeedPlanner/MapProjectorPrefabs")]
    public class MapProjectorPrefabs : ScriptableObject, IMapProjectorPrefabs
    {
        [SerializeField] private MapProjector[] _projectorPrefabs = null;

        public IToggleableMapProjector GetPrefabForColor(ProjectorColor color)
        {
            foreach (MapProjector prefab in _projectorPrefabs)
            {
                if (prefab.Color == color)
                    return prefab;
            }

            return null;
        }
    }
}
