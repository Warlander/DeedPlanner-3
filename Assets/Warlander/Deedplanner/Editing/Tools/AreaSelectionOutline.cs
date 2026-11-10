using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Editing
{
    public class AreaSelectionOutline : MaskableGraphic
    {
        private static readonly float[] _strokeOffsets = { -2f, -1.25f, -0.75f, 0.75f, 1.25f, 2f };
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
            DrawBorder(vertices);
        }

        private void DrawBorder(VertexHelper vertices)
        {
            float unitsPerPixel = canvas ? 1f / canvas.scaleFactor : 1f;
            Color dark = new Color(0.12f, 0.09f, 0.04f, 0.75f);
            Color clear = new Color(dark.r, dark.g, dark.b, 0);
            for (int i = 0; i < _segments.Count; i += 2)
            {
                Vector2 start = _segments[i];
                Vector2 end = _segments[i + 1];
                Vector2 direction = (end - start).normalized;
                if (direction == Vector2.zero) continue;
                Vector2 normal = new Vector2(-direction.y, direction.x);
                Vector2 startJoin = GetJoin(i, true, normal) * unitsPerPixel;
                Vector2 endJoin = GetJoin(i, false, normal) * unitsPerPixel;
                int index = vertices.currentVertCount;
                for (int ring = 0; ring < _strokeOffsets.Length; ring++)
                {
                    Color tint = ring == 0 || ring == 5 ? clear : ring == 1 || ring == 4 ? dark : color;
                    vertices.AddVert(start + startJoin * _strokeOffsets[ring], tint, Vector2.zero);
                    vertices.AddVert(end + endJoin * _strokeOffsets[ring], tint, Vector2.zero);
                }
                for (int ring = 0; ring < _strokeOffsets.Length - 1; ring++)
                {
                    int a = index + ring * 2;
                    vertices.AddTriangle(a, a + 2, a + 1);
                    vertices.AddTriangle(a + 1, a + 2, a + 3);
                }
            }
        }

        private Vector2 GetJoin(int segment, bool start, Vector2 normal)
        {
            int adjacent = (segment + (start ? _segments.Count - 2 : 2)) % _segments.Count;
            Vector2 point = _segments[segment + (start ? 0 : 1)];
            Vector2 connection = _segments[adjacent + (start ? 1 : 0)];
            if ((point - connection).sqrMagnitude > 0.000001f) return normal;
            Vector2 direction = (_segments[adjacent + 1] - _segments[adjacent]).normalized;
            Vector2 miter = (normal + new Vector2(-direction.y, direction.x)).normalized;
            float alignment = Vector2.Dot(miter, normal);
            return alignment > 0.001f ? miter * Mathf.Min(4f, 1f / alignment) : normal;
        }
    }
}
