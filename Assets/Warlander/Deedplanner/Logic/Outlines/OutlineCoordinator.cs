using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Graphics.Outline;

namespace Warlander.Deedplanner.Logic.Outlines
{
    public class OutlineCoordinator : IOutlineCoordinator
    {
        private readonly Dictionary<DynamicModelBehaviour, OutlineType> _outlinesToUse = new Dictionary<DynamicModelBehaviour, OutlineType>();
        private readonly Dictionary<DynamicModelBehaviour, int> _outlinesPriority  = new Dictionary<DynamicModelBehaviour, int>();
        private readonly Dictionary<DynamicModelBehaviour, OutlineEntry> _activeOutlines = new Dictionary<DynamicModelBehaviour, OutlineEntry>();

        public bool HasOutlinedObjects => _activeOutlines.Count > 0;

        public void AddObject(DynamicModelBehaviour behaviour, OutlineType type, int priority)
        {
            if (_outlinesPriority.TryGetValue(behaviour, out int currentPriority))
            {
                if (currentPriority > priority) return;
            }

            _outlinesToUse[behaviour] = type;
            _outlinesPriority[behaviour] = priority;

            if (behaviour.Model != null)
                ApplyOutline(behaviour, type);

            behaviour.ModelLoaded += OnModelLoaded;
        }

        private void OnModelLoaded(DynamicModelBehaviour rootObject, GameObject newModel)
        {
            OutlineType typeToUse = _outlinesToUse[rootObject];
            RemoveOutline(rootObject);
            ApplyOutline(rootObject, typeToUse);
        }

        public void RemoveObject(DynamicModelBehaviour behaviour, int priority)
        {
            if (_outlinesPriority.TryGetValue(behaviour, out int currentPriority))
            {
                if (currentPriority > priority) return;
            }

            behaviour.ModelLoaded -= OnModelLoaded;
            _outlinesToUse.Remove(behaviour);
            _outlinesPriority.Remove(behaviour);
            RemoveOutline(behaviour);
        }

        private void ApplyOutline(DynamicModelBehaviour behaviour, OutlineType type)
        {
            GameObject modelRoot = behaviour.Model;
            if (modelRoot == null) return;

            if (_activeOutlines.TryGetValue(behaviour, out OutlineEntry existing))
            {
                _activeOutlines[behaviour] = new OutlineEntry(existing.Renderers, type);
                return;
            }

            Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>();
            _activeOutlines[behaviour] = new OutlineEntry(renderers, type);
        }

        private void RemoveOutline(DynamicModelBehaviour behaviour)
        {
            _activeOutlines.Remove(behaviour);
        }

        public List<OutlineEntry> GetOutlinedObjectsSnapshot()
        {
            var result = new List<OutlineEntry>(_activeOutlines.Count);
            foreach (var kvp in _activeOutlines)
                result.Add(kvp.Value);
            return result;
        }
    }
}
