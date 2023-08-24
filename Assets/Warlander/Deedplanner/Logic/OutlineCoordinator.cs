using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Logic.Cameras;
using Zenject;

namespace Warlander.Deedplanner.Logic
{
    public class OutlineCoordinator
    {
        [Inject] private CameraCoordinator _cameraCoordinator;

        private readonly Dictionary<DynamicModelBehaviour, OutlineType> _outlinesToUse =
            new Dictionary<DynamicModelBehaviour, OutlineType>();
        
        public void AddObject(DynamicModelBehaviour behaviour, OutlineType type)
        {
            _outlinesToUse[behaviour] = type;

            if (behaviour.Model != null)
            {
                foreach (MultiCamera camera in _cameraCoordinator.Cameras)
                {
                    camera.OutlineEffect.AddGameObject(behaviour.Model, (int)type);
                }
            }

            behaviour.ModelLoaded += OnModelLoaded;
        }

        private void OnModelLoaded(DynamicModelBehaviour rootObject, GameObject newModel)
        {
            OutlineType typeToUse = _outlinesToUse[rootObject];
            
            foreach (MultiCamera camera in _cameraCoordinator.Cameras)
            {
                camera.OutlineEffect.AddGameObject(newModel, (int)typeToUse);
            }
        }

        public void RemoveObject(DynamicModelBehaviour behaviour)
        {
            behaviour.ModelLoaded -= OnModelLoaded;
            _outlinesToUse.Remove(behaviour);
            
            foreach (MultiCamera camera in _cameraCoordinator.Cameras)
            {
                camera.OutlineEffect.RemoveGameObject(behaviour.Model);
            }
        }
    }
}