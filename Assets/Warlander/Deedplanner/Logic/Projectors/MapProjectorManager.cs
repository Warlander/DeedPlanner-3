using System;
using System.Collections.Generic;
using UnityEngine;

namespace Warlander.Deedplanner.Logic.Projectors
{
    public class MapProjectorManager : MonoBehaviour
    {
        private static MapProjectorManager instance;

        public static MapProjectorManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<MapProjectorManager>();
                    if (!instance)
                    {
                        Debug.LogError($"Unable to find {nameof(MapProjectorManager)} in scene.");
                    }
                }

                return instance;
            }
        }
        
        [SerializeField]
        private MapProjector[] ProjectorPrefabs = null;

        private Dictionary<ProjectorColor, Stack<MapProjector>> freeProjectors = new Dictionary<ProjectorColor, Stack<MapProjector>>();

        private void Awake()
        {
            if (!instance)
            {
                instance = this;
            }
            else
            {
                Debug.LogError($"More than one instance of {nameof(MapProjectorManager)} exists.");
            }
        }
        
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
                    return Instantiate(prefab, transform);
                }
            }

            return null;
        }
    }
}