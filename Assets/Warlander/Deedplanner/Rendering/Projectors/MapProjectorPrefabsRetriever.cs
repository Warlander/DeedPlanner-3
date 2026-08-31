using UnityEngine;

namespace Warlander.Deedplanner.Rendering.Projectors
{
    public class MapProjectorPrefabsRetriever : IMapProjectorPrefabsRetriever
    {
        private const string ResourcePath = "MapProjectorPrefabs";

        public IMapProjectorPrefabs RetrievePrefabs()
        {
            return Resources.Load<MapProjectorPrefabs>(ResourcePath);
        }
    }
}
