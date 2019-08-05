using UnityEngine;
using Warlander.Deedplanner.Graphics;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Data
{
    public class PlaneLine
    {

        public Vector2Int TileCoords { get; private set; }
        public PlaneAlignment Alignment { get; private set; }
        private Projector projector;

        public PlaneLine(Vector2Int tileCoords, PlaneAlignment alignment)
        {
            TileCoords = tileCoords;
            Alignment = alignment;
            projector = Object.Instantiate(GraphicsManager.Instance.AxisProjectorPrefab);
        }

        public void UpdateProjector()
        {
            Map map = GameManager.Instance.Map;

            if (Alignment == PlaneAlignment.Horizontal)
            {
                projector.transform.localPosition = new Vector3(map.Width * 2f, 10000, TileCoords.y * 4);
                projector.orthographicSize = 0.25f;
                projector.aspectRatio = map.Width * 2 / projector.orthographicSize;
            }
            else if (Alignment == PlaneAlignment.Vertical)
            {
                projector.transform.localPosition = new Vector3(TileCoords.x * 4, 10000, map.Height * 2f);
                projector.orthographicSize = map.Height * 2;
                projector.aspectRatio = 0.25f / projector.orthographicSize;
            }
        }
    }
}