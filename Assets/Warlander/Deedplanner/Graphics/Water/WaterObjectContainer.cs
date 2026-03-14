using System;
using UnityEngine;

namespace Warlander.Deedplanner.Graphics.Water
{
    public class WaterObjectContainer : IDisposable
    {
        public GameObject ComplexWater { get; }
        public Renderer ComplexWaterRenderer { get; }
        public GameObject SimpleWater { get; }

        private readonly GameObject _root;

        public WaterObjectContainer(WaterObjectLoader loader)
        {
            _root = new GameObject("Water Tables");
            _root.hideFlags = HideFlags.HideInHierarchy;

            ComplexWater = loader.InstantiateComplexWater(_root.transform);
            SimpleWater = loader.InstantiateSimpleWater(_root.transform);
            ComplexWaterRenderer = ComplexWater.GetComponent<Renderer>();

            ComplexWater.SetActive(false);
            SimpleWater.SetActive(false);
        }

        public void Dispose()
        {
            if (_root)
            {
                UnityEngine.Object.Destroy(_root);
            }
        }
    }
}
