using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Domain;

namespace Warlander.Deedplanner.Rendering.Outline
{
    public class OutlineCoordinator : IOutlineCoordinator
    {
        private readonly Dictionary<DynamicModelBehaviour, OutlineRegistration> _outlines =
            new Dictionary<DynamicModelBehaviour, OutlineRegistration>();

        public bool HasOutlinedObjects
        {
            get
            {
                foreach (OutlineRegistration outline in _outlines.Values)
                {
                    if (outline.Renderers != null) return true;
                }
                return false;
            }
        }

        public bool RenderingSuspended { get; set; }

        public void AddObject(DynamicModelBehaviour behaviour, OutlineType type, int priority)
        {
            if (_outlines.TryGetValue(behaviour, out OutlineRegistration outline))
            {
                if (outline.Priority > priority) return;
            }
            else
            {
                outline = new OutlineRegistration();
                _outlines.Add(behaviour, outline);
                behaviour.ModelLoaded += OnModelLoaded;
            }

            outline.Type = type;
            outline.Priority = priority;
            if (outline.Renderers == null && behaviour.Model != null)
            {
                outline.Renderers = behaviour.Model.GetComponentsInChildren<Renderer>();
            }
        }

        private void OnModelLoaded(DynamicModelBehaviour rootObject, GameObject newModel)
        {
            if (_outlines.TryGetValue(rootObject, out OutlineRegistration outline))
            {
                outline.Renderers = newModel != null ? newModel.GetComponentsInChildren<Renderer>() : null;
            }
        }

        public void RemoveObject(DynamicModelBehaviour behaviour, int priority)
        {
            if (!_outlines.TryGetValue(behaviour, out OutlineRegistration outline)
                || outline.Priority > priority) return;

            behaviour.ModelLoaded -= OnModelLoaded;
            _outlines.Remove(behaviour);
        }

        public List<OutlineEntry> GetOutlinedObjectsSnapshot()
        {
            var result = new List<OutlineEntry>(_outlines.Count);
            foreach (OutlineRegistration outline in _outlines.Values)
            {
                if (outline.Renderers != null)
                {
                    result.Add(new OutlineEntry(outline.Renderers, outline.Type));
                }
            }
            return result;
        }

        private sealed class OutlineRegistration
        {
            public OutlineType Type;
            public int Priority;
            public Renderer[] Renderers;
        }
    }
}
