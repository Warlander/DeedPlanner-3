using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Warlander.Deedplanner.Graphics.Projectors
{
    public class MapProjectorManager : MonoBehaviour
    {
        [Inject] private IInstantiator _instantiator;

        [SerializeField] private MapProjector[] ProjectorPrefabs = null;

        private Dictionary<ProjectorColor, Stack<MapProjector>> freeProjectors = new Dictionary<ProjectorColor, Stack<MapProjector>>();

        public MapProjector RequestProjector(ProjectorColor color)
        {
            ValidateList(color);

            if (freeProjectors[color].Count > 0)
            {
                MapProjector projector = freeProjectors[color].Pop();
                projector.gameObject.SetActive(true);
                projector.MarkRenderWithAllCameras();
                return projector;
            }

            return CreateProjectorFromDatabase(color);
        }

        public void FreeProjector(MapProjector projector)
        {
            ValidateList(projector.Color);
            
            projector.gameObject.SetActive(false);
            freeProjectors[projector.Color].Push(projector);
        }

        private void ValidateList(ProjectorColor color)
        {
            if (!freeProjectors.ContainsKey(color))
            {
                freeProjectors[color] = new Stack<MapProjector>();
            }
        }

        private MapProjector CreateProjectorFromDatabase(ProjectorColor color)
        {
            foreach (MapProjector prefab in ProjectorPrefabs)
            {
                if (prefab.Color == color)
                {
                    return _instantiator.InstantiatePrefabForComponent<MapProjector>(prefab, transform);
                }
            }

            return null;
        }
    }
}