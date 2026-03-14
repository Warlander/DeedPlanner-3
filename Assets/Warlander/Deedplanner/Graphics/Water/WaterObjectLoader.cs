using UnityEngine;

namespace Warlander.Deedplanner.Graphics.Water
{
    /// <summary>
    /// Stores resource paths for water prefabs and instantiates them on request.
    /// </summary>
    public class WaterObjectLoader
    {
        private const string ComplexWaterPath = "Water/ComplexWater";
        private const string SimpleWaterPath = "Water/Simple Water";

        public GameObject InstantiateComplexWater(Transform parent)
        {
            return UnityEngine.Object.Instantiate(Resources.Load<GameObject>(ComplexWaterPath), parent);
        }

        public GameObject InstantiateSimpleWater(Transform parent)
        {
            return UnityEngine.Object.Instantiate(Resources.Load<GameObject>(SimpleWaterPath), parent);
        }
    }
}
