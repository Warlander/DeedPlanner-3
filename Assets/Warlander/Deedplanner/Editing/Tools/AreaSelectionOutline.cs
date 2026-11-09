using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Editing
{
    public class AreaSelectionOutline : MaskableGraphic
    {
        private readonly List<Vector2> _segments = new List<Vector2>();

        public void SetSegments(IReadOnlyList<Vector2> segments)
        {
            _segments.Clear();
            for (int i = 0; i < segments.Count; i++) _segments.Add(segments[i]);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            for (int i = 0; i < _segments.Count; i += 2)
            {
                Vector2 start = _segments[i];
                Vector2 end = _segments[i + 1];
                Vector2 direction = (end - start).normalized;
                Vector2 offset = new Vector2(-direction.y, direction.x);
                int index = vertices.currentVertCount;
                vertices.AddVert(start - offset, color, Vector2.zero);
                vertices.AddVert(start + offset, color, Vector2.zero);
                vertices.AddVert(end + offset, color, Vector2.zero);
                vertices.AddVert(end - offset, color, Vector2.zero);
                vertices.AddTriangle(index, index + 1, index + 2);
                vertices.AddTriangle(index, index + 2, index + 3);
            }
        }
    }
}
