using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Logic.Cameras;
using Zenject;

namespace Warlander.Deedplanner.Logic
{
    public class OutlineCoordinator : IOutlineCoordinator
    {
        private readonly CameraCoordinator _cameraCoordinator;

        [Inject]
        public OutlineCoordinator(CameraCoordinator cameraCoordinator)
        {
            _cameraCoordinator = cameraCoordinator;
        }

        private readonly Dictionary<DynamicModelBehaviour, OutlineType> _outlinesToUse =
            new Dictionary<DynamicModelBehaviour, OutlineType>();

        private readonly Dictionary<DynamicModelBehaviour, int> _outlinesPriority =
            new Dictionary<DynamicModelBehaviour, int>();
        
        public void AddObject(DynamicModelBehaviour behaviour, OutlineType type, int priority)
        {
            if (_outlinesPriority.TryGetValue(behaviour, out int currentPriority))
            {
                if (currentPriority > priority)
                {
                    return;
                }
            }
            
            _outlinesToUse[behaviour] = type;
            _outlinesPriority[behaviour] = priority;

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

        public void RemoveObject(DynamicModelBehaviour behaviour, int priority)
        {
            if (_outlinesPriority.TryGetValue(behaviour, out int currentPriority))
            {
                if (currentPriority > priority)
                {
                    return;
                }
            }
            
            behaviour.ModelLoaded -= OnModelLoaded;
            _outlinesToUse.Remove(behaviour);
            _outlinesPriority.Remove(behaviour);
            
            foreach (MultiCamera camera in _cameraCoordinator.Cameras)
            {
                camera.OutlineEffect.RemoveGameObject(behaviour.Model);
            }
        }
    }
}