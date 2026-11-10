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
            if (_segments.Count == 0) return;
            Vector2 center = Vector2.zero;
            foreach (Vector2 point in _segments) center += point;
            center /= _segments.Count;
            Color fill = color;
            fill.a = 0.1f;
            vertices.AddVert(center, fill, Vector2.zero);
            for (int i = 0; i < _segments.Count; i += 2)
            {
                int index = vertices.currentVertCount;
                vertices.AddVert(_segments[i], fill, Vector2.zero);
                vertices.AddVert(_segments[i + 1], fill, Vector2.zero);
                vertices.AddTriangle(0, index, index + 1);
            }
            DrawBorder(vertices, 2f, new Color(0.12f, 0.09f, 0.04f, 0.75f));
            DrawBorder(vertices, 1f, color);
        }

        private void DrawBorder(VertexHelper vertices, float halfWidth, Color tint)
        {
            for (int i = 0; i < _segments.Count; i += 2)
            {
                Vector2 start = _segments[i];
                Vector2 end = _segments[i + 1];
                Vector2 direction = (end - start).normalized;
                Vector2 offset = new Vector2(-direction.y, direction.x) * halfWidth;
                int index = vertices.currentVertCount;
                vertices.AddVert(start - offset, tint, Vector2.zero);
                vertices.AddVert(start + offset, tint, Vector2.zero);
                vertices.AddVert(end + offset, tint, Vector2.zero);
                vertices.AddVert(end - offset, tint, Vector2.zero);
                vertices.AddTriangle(index, index + 1, index + 2);
                vertices.AddTriangle(index, index + 2, index + 3);
            }
        }
    }
}
