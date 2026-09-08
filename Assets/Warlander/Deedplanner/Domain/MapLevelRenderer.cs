using System;
using System.Collections.Generic;
using UnityEngine;
using Warlander.Deedplanner.Bridges;
using Warlander.Deedplanner.Docks;
using Warlander.Deedplanner;
using Warlander.Deedplanner.Caves;

namespace Warlander.Deedplanner.Domain
{
    public class MapLevelRenderer
    {
        private Transform[] _surfaceLevelRoots;
        private Transform[] _caveLevelRoots;
        private Transform _surfaceGridRoot;
        private Transform _caveGridRoot;
        private Transform _caveShellRoot;
        private Func<IEnumerable<Bridge>> _getBridges;
        private Func<IEnumerable<Dock>> _getDocks;
        private IDisposable _legacyRenderScope;
        private readonly Dictionary<Renderer, RendererBaseState> _baseRendererStates =
            new Dictionary<Renderer, RendererBaseState>();
        private readonly Dictionary<Collider, bool> _baseColliderStates = new Dictionary<Collider, bool>();
        private int _scopeDepth;

        private int _renderedLevel;
        private bool _renderEntireMap = true;
        private bool _renderGrid = true;

        public bool RenderBridges { get; set; } = true;

        public void Initialize(
            Transform[] surfaceLevelRoots,
            Transform[] caveLevelRoots,
            Transform surfaceGridRoot,
            Transform caveGridRoot,
            Transform caveShellRoot,
            Func<IEnumerable<Bridge>> getBridges,
            Func<IEnumerable<Dock>> getDocks)
        {
            _surfaceLevelRoots = surfaceLevelRoots;
            _caveLevelRoots = caveLevelRoots;
            _surfaceGridRoot = surfaceGridRoot;
            _caveGridRoot = caveGridRoot;
            _caveShellRoot = caveShellRoot;
            _getBridges = getBridges;
            _getDocks = getDocks;

            SetRootsActive(_surfaceLevelRoots);
            SetRootsActive(_caveLevelRoots);
            _surfaceGridRoot.gameObject.SetActive(true);
            _caveGridRoot.gameObject.SetActive(true);
            _caveShellRoot.gameObject.SetActive(true);
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
            int absoluteLevel = cave ? CaveLevel.GetStoreyIndex(level) : level;
            if (cave)
                entity.transform.SetParent(_caveLevelRoots[absoluteLevel]);
            else
                entity.transform.SetParent(_surfaceLevelRoots[absoluteLevel]);
        }

        public void UpdateLevelsRendering()
        {
            if (_surfaceLevelRoots == null) return;

            _legacyRenderScope?.Dispose();
            _legacyRenderScope = PrepareForCamera(new MapRenderView(_renderedLevel, _renderEntireMap, _renderGrid));

            RefreshBridgesRendering(_renderedLevel < 0
                ? CaveLevel.GetStoreyIndex(_renderedLevel)
                : _renderedLevel);
            RefreshDocksRendering(_renderedLevel < 0
                ? CaveLevel.GetStoreyIndex(_renderedLevel)
                : _renderedLevel, _renderedLevel < 0);
        }

        public IDisposable PrepareForCamera(MapRenderView view)
        {
            if (_surfaceLevelRoots == null)
            {
                return EmptyScope.Instance;
            }

            if (_scopeDepth == 0)
            {
                _baseRendererStates.Clear();
                _baseColliderStates.Clear();
            }

            _scopeDepth++;
            RenderScope scope = new RenderScope(this, _surfaceGridRoot.localPosition, _caveGridRoot.localPosition);
            ApplyView(scope, view);
            return scope;
        }

        private void ApplyView(RenderScope scope, MapRenderView view)
        {
            int storeyIndex = view.StoreyIndex;
            for (int i = 0; i < _surfaceLevelRoots.Length; i++)
            {
                float opacity = view.IsUnderground ? 0f : GetOpacity(view, i - storeyIndex);
                ApplyRoot(scope, _surfaceLevelRoots[i], opacity);
            }

            for (int i = 0; i < _caveLevelRoots.Length; i++)
            {
                float opacity = view.IsUnderground ? GetOpacity(view, i - storeyIndex) : 0f;
                ApplyRoot(scope, _caveLevelRoots[i], opacity);
            }

            ApplyRoot(scope, _surfaceGridRoot, !view.IsUnderground && view.RenderGrid ? 1f : 0f);
            ApplyRoot(scope, _caveGridRoot, view.IsUnderground && view.RenderGrid ? 1f : 0f);
            ApplyRoot(scope, _caveShellRoot,
                view.IsUnderground ? GetOpacity(view, -storeyIndex) : 0f);

            if (view.IsUnderground)
                _caveGridRoot.localPosition = new Vector3(0, storeyIndex * 3, 0);
            else
                _surfaceGridRoot.localPosition = new Vector3(0, storeyIndex * 3 + 0.01f, 0);
        }

        private float GetOpacity(MapRenderView view, int relativeLevel)
        {
            return view.RenderEntireMap ? 1f : GetRelativeLevelOpacity(relativeLevel);
        }

        private void ApplyRoot(RenderScope scope, Transform root, float opacity)
        {
            bool visible = opacity > 0f;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                RendererState state = RendererState.Capture(renderer);
                scope.Add(state);
                if (!_baseRendererStates.TryGetValue(renderer, out RendererBaseState baseState))
                {
                    baseState = new RendererBaseState(state.ForceRenderingOff);
                    _baseRendererStates.Add(renderer, baseState);
                }

                renderer.forceRenderingOff = baseState.ForceRenderingOff || !visible;
                if (visible && opacity < 1f)
                {
                    MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(propertyBlock);
                    Color baseColor = GetBaseColor(renderer, propertyBlock);
                    propertyBlock.SetColor(ShaderPropertyIds.BaseColor,
                        new Color(baseColor.r * opacity, baseColor.g * opacity, baseColor.b * opacity, baseColor.a));
                    renderer.SetPropertyBlock(propertyBlock);
                }
            }

            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in colliders)
            {
                scope.Add(new ColliderState(collider, collider.enabled));
                if (!_baseColliderStates.TryGetValue(collider, out bool baseEnabled))
                {
                    baseEnabled = collider.enabled;
                    _baseColliderStates.Add(collider, baseEnabled);
                }

                collider.enabled = baseEnabled && visible;
            }
        }

        private static Color GetBaseColor(Renderer renderer, MaterialPropertyBlock propertyBlock)
        {
            if (propertyBlock.HasColor(ShaderPropertyIds.BaseColor))
            {
                return propertyBlock.GetColor(ShaderPropertyIds.BaseColor);
            }

            Material material = renderer.sharedMaterial;
            return material && material.HasProperty(ShaderPropertyIds.BaseColor)
                ? material.GetColor(ShaderPropertyIds.BaseColor)
                : Color.white;
        }

        private void CompleteScope(RenderScope scope)
        {
            scope.Restore();
            _scopeDepth--;
            if (_scopeDepth == 0)
            {
                _baseRendererStates.Clear();
                _baseColliderStates.Clear();
            }
        }

        private static void SetRootsActive(IEnumerable<Transform> roots)
        {
            foreach (Transform root in roots)
                root.gameObject.SetActive(true);
        }

        public void UpdateBridgesRendering()
        {
            if (_surfaceLevelRoots == null) return;

            bool underground = _renderedLevel < 0;
            int absoluteLevel = underground ? CaveLevel.GetStoreyIndex(_renderedLevel) : _renderedLevel;
            RefreshBridgesRendering(absoluteLevel);
        }

        private void RefreshBridgesRendering(int absoluteLevel)
        {
            if (_getBridges == null) return;

            if (!RenderBridges)
            {
                foreach (Bridge bridge in _getBridges())
                    bridge.SetVisible(false);
                return;
            }

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
                    propertyBlock.SetColor(ShaderPropertyIds.BaseColor, new Color(opacity, opacity, opacity));
                    bridge.SetPropertyBlock(propertyBlock);
                }
            }
        }

        public void UpdateDocksRendering()
        {
            if (_surfaceLevelRoots == null) return;

            bool underground = _renderedLevel < 0;
            int absoluteLevel = underground ? CaveLevel.GetStoreyIndex(_renderedLevel) : _renderedLevel;
            RefreshDocksRendering(absoluteLevel, underground);
        }

        private void RefreshDocksRendering(int absoluteLevel, bool underground)
        {
            if (_getDocks == null) return;

            foreach (Dock dock in _getDocks())
            {
                float opacity;
                if (underground || dock.GetEffectiveLevel() < 0)
                {
                    opacity = 0f;
                }
                else if (_renderEntireMap)
                {
                    opacity = 1f;
                }
                else
                {
                    opacity = GetRelativeLevelOpacity(dock.GetEffectiveLevel() - absoluteLevel);
                }

                dock.ApplyLevelRendering(opacity);
            }
        }

        private sealed class RenderScope : IDisposable
        {
            private readonly MapLevelRenderer _owner;
            private readonly List<RendererState> _renderers = new List<RendererState>();
            private readonly List<ColliderState> _colliders = new List<ColliderState>();
            private readonly Vector3 _surfaceGridPosition;
            private readonly Vector3 _caveGridPosition;
            private bool _disposed;

            public RenderScope(MapLevelRenderer owner, Vector3 surfaceGridPosition, Vector3 caveGridPosition)
            {
                _owner = owner;
                _surfaceGridPosition = surfaceGridPosition;
                _caveGridPosition = caveGridPosition;
            }

            public void Add(RendererState state)
            {
                _renderers.Add(state);
            }

            public void Add(ColliderState state)
            {
                _colliders.Add(state);
            }

            public void Restore()
            {
                for (int i = _renderers.Count - 1; i >= 0; i--)
                    _renderers[i].Restore();
                for (int i = _colliders.Count - 1; i >= 0; i--)
                    _colliders[i].Restore();

                _owner._surfaceGridRoot.localPosition = _surfaceGridPosition;
                _owner._caveGridRoot.localPosition = _caveGridPosition;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _owner.CompleteScope(this);
            }
        }

        private readonly struct RendererBaseState
        {
            public bool ForceRenderingOff { get; }

            public RendererBaseState(bool forceRenderingOff)
            {
                ForceRenderingOff = forceRenderingOff;
            }
        }

        private readonly struct RendererState
        {
            public Renderer Renderer { get; }
            public bool ForceRenderingOff { get; }
            public MaterialPropertyBlock PropertyBlock { get; }

            private RendererState(Renderer renderer, bool forceRenderingOff, MaterialPropertyBlock propertyBlock)
            {
                Renderer = renderer;
                ForceRenderingOff = forceRenderingOff;
                PropertyBlock = propertyBlock;
            }

            public static RendererState Capture(Renderer renderer)
            {
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propertyBlock);
                return new RendererState(renderer, renderer.forceRenderingOff, propertyBlock);
            }

            public void Restore()
            {
                if (!Renderer) return;
                Renderer.forceRenderingOff = ForceRenderingOff;
                Renderer.SetPropertyBlock(PropertyBlock);
            }
        }

        private readonly struct ColliderState
        {
            private Collider Collider { get; }
            private bool Enabled { get; }

            public ColliderState(Collider collider, bool enabled)
            {
                Collider = collider;
                Enabled = enabled;
            }

            public void Restore()
            {
                if (Collider)
                    Collider.enabled = Enabled;
            }
        }

        private sealed class EmptyScope : IDisposable
        {
            public static readonly EmptyScope Instance = new EmptyScope();

            public void Dispose()
            {
            }
        }
    }
}
