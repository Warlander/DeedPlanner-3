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
                if (_renderedLevel == value) return;
                _renderedLevel = value;
                UpdateLevelsRendering();
            }
        }

        public bool RenderEntireMap
        {
            get => _renderEntireMap;
            set
            {
                if (_renderEntireMap == value) return;
                _renderEntireMap = value;
                UpdateLevelsRendering();
            }
        }

        public bool RenderGrid
        {
            get => _renderGrid;
            set
            {
                if (_renderGrid == value) return;
                _renderGrid = value;
                UpdateLevelsRendering();
            }
        }

        public void SetActiveView(MapRenderView view)
        {
            _renderedLevel = view.Level;
            _renderEntireMap = view.RenderEntireMap;
            _renderGrid = view.RenderGrid;
            UpdateLevelsRendering();
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

            DynamicModelBehaviour dynamicModel = entity.GetComponent<DynamicModelBehaviour>();
            if (dynamicModel)
            {
                dynamicModel.SetRaycastLayer(LayerMasks.GetLayerForLevel(entity.layer, level));
            }
        }

        public void UpdateLevelsRendering()
        {
            if (_surfaceLevelRoots == null) return;

            _legacyRenderScope?.Dispose();
            _legacyRenderScope = PrepareForCamera(new MapRenderView(_renderedLevel, _renderEntireMap, _renderGrid));

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
            ApplyCaveShellView(view);
            ApplyBridges(scope, view);
            ApplyDocks(scope, view);

            if (view.IsUnderground)
                _caveGridRoot.localPosition = new Vector3(0, storeyIndex * 3, 0);
            else
                _surfaceGridRoot.localPosition = new Vector3(0, storeyIndex * 3 + 0.01f, 0);
        }

        private float GetOpacity(MapRenderView view, int relativeLevel)
        {
            return view.RenderEntireMap ? 1f : GetRelativeLevelOpacity(relativeLevel);
        }

        private void ApplyCaveShellView(MapRenderView view)
        {
            Renderer[] renderers = _caveShellRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                var propertyBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(ShaderPropertyIds.CaveOverview,
                    view.IsUnderground && !view.RenderEntireMap ? 1f : 0f);
                renderer.SetPropertyBlock(propertyBlock);
            }
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
            UpdateLevelsRendering();
        }

        private void ApplyBridges(RenderScope scope, MapRenderView view)
        {
            if (_getBridges == null) return;

            foreach (Bridge bridge in _getBridges())
            {
                foreach (BridgePart part in bridge.Parts)
                {
                    bool matchingRealm = part.Level < 0 == view.IsUnderground;
                    int partStorey = part.Level < 0 ? CaveLevel.GetStoreyIndex(part.Level) : part.Level;
                    bool hiddenInRock = part.Level < 0 && part.Tile.Cave.IsSolid;
                    float opacity = RenderBridges && matchingRealm && !hiddenInRock
                        ? GetOpacity(view, partStorey - view.StoreyIndex)
                        : 0f;
                    ApplyRoot(scope, part.transform, opacity);
                }
            }
        }

        public void UpdateDocksRendering()
        {
            if (_surfaceLevelRoots == null) return;
            UpdateLevelsRendering();
        }

        private void ApplyDocks(RenderScope scope, MapRenderView view)
        {
            if (_getDocks == null) return;

            foreach (Dock dock in _getDocks())
            {
                bool cave = dock.Realm == DockRealm.Cave;
                bool matchingRealm = cave == view.IsUnderground;
                int dockStorey = cave ? CaveLevel.GetStoreyIndex(dock.AnchorLevel) : dock.AnchorLevel;
                bool hiddenInRock = cave && dock.Tile.Cave.IsSolid;
                float opacity = matchingRealm && !hiddenInRock
                    ? GetOpacity(view, dockStorey - view.StoreyIndex)
                    : 0f;
                ApplyRoot(scope, dock.transform, opacity);
            }
        }

        private sealed class RenderScope : IDisposable
        {
            private readonly MapLevelRenderer _owner;
            private readonly List<RendererState> _renderers = new List<RendererState>();
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

            public void Restore()
            {
                for (int i = _renderers.Count - 1; i >= 0; i--)
                    _renderers[i].Restore();

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

        private sealed class EmptyScope : IDisposable
        {
            public static readonly EmptyScope Instance = new EmptyScope();

            public void Dispose()
            {
            }
        }
    }
}
