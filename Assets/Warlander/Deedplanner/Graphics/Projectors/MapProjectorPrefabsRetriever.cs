using UnityEngine;

namespace Warlander.Deedplanner.Graphics.Projectors
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
