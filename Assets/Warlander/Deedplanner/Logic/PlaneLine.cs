using UnityEngine;
using Warlander.Deedplanner.Data;

namespace Warlander.Deedplanner.Logic
{
    public class PlaneLine : MonoBehaviour
    {

        public Vector2Int TileCoords { get; set; }
        public PlaneAlignment Alignment { get; set; }
        [SerializeField] private Projector projector;

        private void Awake()
        {
            Map map = GameManager.Instance.Map;
            transform.SetParent(map.PlaneLineRoot);
        }

        private void Update()
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