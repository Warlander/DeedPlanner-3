using System;
using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Data.Bridges;
using Warlander.Deedplanner;

namespace Warlander.Deedplanner.Data
{
    public class MapLevelRenderer
    {
        private Transform[] _surfaceLevelRoots;
        private Transform[] _caveLevelRoots;
        private Transform _surfaceGridRoot;
        private Transform _caveGridRoot;
        private Func<IEnumerable<Bridge>> _getBridges;

        private int _renderedLevel;
        private bool _renderEntireMap = true;
        private bool _renderGrid = true;

        public void Initialize(
            Transform[] surfaceLevelRoots,
            Transform[] caveLevelRoots,
            Transform surfaceGridRoot,
            Transform caveGridRoot,
            Func<IEnumerable<Bridge>> getBridges)
        {
            _surfaceLevelRoots = surfaceLevelRoots;
            _caveLevelRoots = caveLevelRoots;
            _surfaceGridRoot = surfaceGridRoot;
            _caveGridRoot = caveGridRoot;
            _getBridges = getBridges;
        }

        public int RenderedLevel
        {
            get => _renderedLevel;
            set
            {
                _renderedLevel = value;
                UpdateLevelsRendering();
            }
        }

        public bool RenderEntireMap
        {
            get => _renderEntireMap;
            set
            {
                _renderEntireMap = value;
                UpdateLevelsRendering();
            }
        }

        public bool RenderGrid
        {
            get => _renderGrid;
            set
            {
                _renderGrid = value;
                UpdateLevelsRendering();
            }
        }

        public float GetRelativeLevelOpacity(int relativeLevel)
        {
            if (relativeLevel == 0) return 1f;
            if (relativeLevel == -1) return 0.6f;
            if (relativeLevel == -2) return 0.25f;
            return 0f;
        }

        public void AddEntityToMap(GameObject entity, int level)
        {
            bool cave = level < 0;
            int absoluteLevel = cave ? -level - 1 : level;
            if (cave)
                entity.transform.SetParent(_caveLevelRoots[absoluteLevel]);
            else
                entity.transform.SetParent(_surfaceLevelRoots[absoluteLevel]);
        }

        public void UpdateLevelsRendering()
        {
            if (_surfaceLevelRoots == null) return;

            bool underground = _renderedLevel < 0;
            int absoluteLevel = underground ? -_renderedLevel + 1 : _renderedLevel;

            if (underground)
            {
                foreach (Transform root in _surfaceLevelRoots)
                    root.gameObject.SetActive(false);

                for (int i = 0; i < _caveLevelRoots.Length; i++)
                    RefreshLevelRendering(_caveLevelRoots[i], i - absoluteLevel);

                _surfaceGridRoot.gameObject.SetActive(false);
                _caveGridRoot.gameObject.SetActive(_renderGrid);
                _caveGridRoot.localPosition = new Vector3(0, absoluteLevel * 3, 0);
            }
            else
            {
                foreach (Transform root in _caveLevelRoots)
                    root.gameObject.SetActive(false);

                for (int i = 0; i < _surfaceLevelRoots.Length; i++)
                    RefreshLevelRendering(_surfaceLevelRoots[i], i - absoluteLevel);

                _surfaceGridRoot.gameObject.SetActive(_renderGrid);
                _caveGridRoot.gameObject.SetActive(false);
                _surfaceGridRoot.localPosition = new Vector3(0, absoluteLevel * 3 + 0.01f, 0);
            }

            RefreshBridgesRendering(absoluteLevel);
        }

        private void RefreshLevelRendering(Transform root, int relativeLevel)
        {
            float opacity = _renderEntireMap ? 1f : GetRelativeLevelOpacity(relativeLevel);
            bool renderLevel = opacity > 0;
            root.gameObject.SetActive(renderLevel);
            if (renderLevel)
            {
                var propertyBlock = new MaterialPropertyBlock();
                propertyBlock.SetColor(ShaderPropertyIds.Color, new Color(opacity, opacity, opacity));
                foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
                    r.SetPropertyBlock(propertyBlock);
            }
        }

        private void RefreshBridgesRendering(int absoluteLevel)
        {
            if (_getBridges == null) return;

            if (_renderEntireMap)
            {
                foreach (Bridge bridge in _getBridges())
                    bridge.SetVisible(true);
                return;
            }

            foreach (Bridge bridge in _getBridges())
            {
                int lowerLevel = bridge.LowerLevel;
                int higherLevel = bridge.HigherLevel;

                float opacity;
                if (higherLevel > absoluteLevel)
                    opacity = 0f;
                else if (higherLevel < absoluteLevel && lowerLevel > absoluteLevel)
                    opacity = 1f;
                else
                    opacity = GetRelativeLevelOpacity(higherLevel - absoluteLevel);

                bool renderBridge = opacity > 0;
                bridge.SetVisible(renderBridge);
                if (renderBridge)
                {
                    var propertyBlock = new MaterialPropertyBlock();
                    propertyBlock.SetColor(ShaderPropertyIds.Color, new Color(opacity, opacity, opacity));
                    bridge.SetPropertyBlock(propertyBlock);
                }
            }
        }
    }
}
