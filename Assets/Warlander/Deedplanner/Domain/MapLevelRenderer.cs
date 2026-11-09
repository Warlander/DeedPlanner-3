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
        private readonly Dictionary<Renderer, RendererBaseState> _baseRendererStates =
            new Dictionary<Renderer, RendererBaseState>();
        private readonly HashSet<Renderer> _ownedNullBlocks = new HashSet<Renderer>();
        private readonly HashSet<Renderer> _scopedPropertyBlocks = new HashSet<Renderer>();
        private readonly MaterialPropertyBlock _emptyPropertyBlock = new MaterialPropertyBlock();
        private readonly MaterialPropertyBlock _workingPropertyBlock = new MaterialPropertyBlock();
        private readonly Stack<MaterialPropertyBlock> _propertyBlocks = new Stack<MaterialPropertyBlock>();
        private readonly List<Renderer> _rendererBuffer = new List<Renderer>();
        private readonly List<Material> _materialBuffer = new List<Material>();
        private readonly List<Renderer> _modelBatchingCandidates = new List<Renderer>();
        private readonly Dictionary<Material, ModelBatching> _modelMaterials = new Dictionary<Material, ModelBatching>();
        private bool _modelDefaultsCompatible;
        private readonly Stack<List<RendererState>> _rendererStates = new Stack<List<RendererState>>();
        private int _scopeDepth;
        private readonly Shader _modelShader = Shader.Find("Warlander/ModelShader");

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

        public int RenderedLevel { get; set; }
        public bool RenderEntireMap { get; set; } = true;
        public bool RenderGrid { get; set; } = true;

        public void SetActiveView(MapRenderView view)
        {
            RenderedLevel = view.Level;
            RenderEntireMap = view.RenderEntireMap;
            RenderGrid = view.RenderGrid;
        }

        private static float GetRelativeLevelOpacity(int relativeLevel)
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

        public IDisposable PrepareForCamera(MapRenderView view)
        {
            if (_surfaceLevelRoots == null)
            {
                return EmptyScope.Instance;
            }

            if (_scopeDepth == 0)
            {
                if (view.PropertyBlockRetention == ModelPropertyBlockRetention.Disabled
                    || view.PropertyBlockRetention == ModelPropertyBlockRetention.Enabled && view.IsUnderground)
                    ReleaseOwnedNullBlocks();
                if (_ownedNullBlocks.Count > 0) _ownedNullBlocks.RemoveWhere(renderer => !renderer);
                _baseRendererStates.Clear();
            }

            _scopeDepth++;
            RenderScope scope = new RenderScope(this, _surfaceGridRoot.localPosition, _caveGridRoot.localPosition,
                view.RenderEntireMap && _scopeDepth == 1 && _modelShader);
            if (scope.AllowModelBatching) _modelDefaultsCompatible = true;
            ApplyView(scope, view);
            if (scope.AllowModelBatching)
            {
                bool retain = view.PropertyBlockRetention == ModelPropertyBlockRetention.Enabled && !view.IsUnderground;
                if (!_modelDefaultsCompatible)
                {
                    if (_ownedNullBlocks.Count > 0)
                        foreach (Renderer renderer in _modelBatchingCandidates)
                            if (_ownedNullBlocks.Remove(renderer) && !renderer.HasPropertyBlock())
                                renderer.SetPropertyBlock(_emptyPropertyBlock);
                }
                else if (!retain && _ownedNullBlocks.Count == 0)
                {
                    foreach (Renderer renderer in _modelBatchingCandidates)
                    {
                        renderer.GetPropertyBlock(_workingPropertyBlock);
                        if (_workingPropertyBlock.isEmpty)
                        {
                            scope.Add(RendererState.ClearedEmptyBlock(renderer));
                            renderer.SetPropertyBlock(null);
                        }
                    }
                }
                else
                {
                    foreach (Renderer renderer in _modelBatchingCandidates)
                    {
                        if (_ownedNullBlocks.Contains(renderer)) continue;
                        renderer.GetPropertyBlock(_workingPropertyBlock);
                        if (!_workingPropertyBlock.isEmpty) continue;
                        scope.Add(RendererState.ClearedEmptyBlock(renderer));
                        renderer.SetPropertyBlock(null);
                        if (retain && !renderer.HasPropertyBlock()) _ownedNullBlocks.Add(renderer);
                    }
                }
                _modelBatchingCandidates.Clear();
                _modelMaterials.Clear();
            }
            _workingPropertyBlock.Clear();
            _rendererBuffer.Clear();
            return scope;
        }

        private void ReleaseOwnedNullBlocks()
        {
            foreach (Renderer renderer in _ownedNullBlocks)
                if (renderer && !renderer.HasPropertyBlock()) renderer.SetPropertyBlock(_emptyPropertyBlock);
            _ownedNullBlocks.Clear();
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
            _caveShellRoot.GetComponentsInChildren(true, _rendererBuffer);
            foreach (Renderer renderer in _rendererBuffer)
            {
                MaterialPropertyBlock propertyBlock = _workingPropertyBlock;
                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(ShaderPropertyIds.CaveOverview,
                    view.IsUnderground && !view.RenderEntireMap ? 1f : 0f);
                renderer.SetPropertyBlock(propertyBlock);
                if (_ownedNullBlocks.Count > 0 && _ownedNullBlocks.Contains(renderer)) _scopedPropertyBlocks.Add(renderer);
            }
        }

        private void ApplyRoot(RenderScope scope, Transform root, float opacity)
        {
            if (_ownedNullBlocks.Count == 0) ApplyRootLegacy(scope, root, opacity);
            else ApplyRootOwned(scope, root, opacity);
        }

        private void ApplyRootLegacy(RenderScope scope, Transform root, float opacity)
        {
            bool visible = opacity > 0f;
            bool changeColor = visible && (opacity < 1f || _scopeDepth > 1);
            bool capturePropertyBlock = changeColor || root == _caveShellRoot;
            root.GetComponentsInChildren(true, _rendererBuffer);
            foreach (Renderer renderer in _rendererBuffer)
            {
                bool hasBaseState = _baseRendererStates.TryGetValue(renderer, out RendererBaseState baseState);
                bool currentForceRenderingOff = renderer.forceRenderingOff;
                bool forceRenderingOff = (hasBaseState ? baseState.ForceRenderingOff : currentForceRenderingOff) || !visible;
                bool canBatchModel = scope.AllowModelBatching && !forceRenderingOff && CanBatchModel(renderer);
                if (!capturePropertyBlock && currentForceRenderingOff == forceRenderingOff && renderer.HasPropertyBlock())
                {
                    if (canBatchModel) _modelBatchingCandidates.Add(renderer);
                    continue;
                }

                MaterialPropertyBlock snapshot = capturePropertyBlock
                    ? (_propertyBlocks.Count > 0 ? _propertyBlocks.Pop() : new MaterialPropertyBlock())
                    : null;
                RendererState state = RendererState.Capture(renderer, currentForceRenderingOff, snapshot);
                scope.Add(state);
                if (!hasBaseState)
                {
                    baseState = new RendererBaseState(state.ForceRenderingOff, null);
                    _baseRendererStates.Add(renderer, baseState);
                }

                renderer.forceRenderingOff = forceRenderingOff;
                if (changeColor)
                {
                    if (!baseState.BaseColor.HasValue)
                    {
                        baseState = new RendererBaseState(baseState.ForceRenderingOff,
                            GetBaseColor(renderer, state.PropertyBlock));
                        _baseRendererStates[renderer] = baseState;
                    }
                    MaterialPropertyBlock propertyBlock = _workingPropertyBlock;
                    renderer.GetPropertyBlock(propertyBlock);
                    Color baseColor = baseState.BaseColor.Value;
                    propertyBlock.SetColor(ShaderPropertyIds.BaseColor,
                        new Color(baseColor.r * opacity, baseColor.g * opacity, baseColor.b * opacity, baseColor.a));
                    renderer.SetPropertyBlock(propertyBlock);
                }
            }

        }

        private void ApplyRootOwned(RenderScope scope, Transform root, float opacity)
        {
            bool visible = opacity > 0f;
            bool changeColor = visible && (opacity < 1f || _scopeDepth > 1);
            bool capturePropertyBlock = changeColor || root == _caveShellRoot;
            root.GetComponentsInChildren(true, _rendererBuffer);
            foreach (Renderer renderer in _rendererBuffer)
            {
                bool ownedNull = _ownedNullBlocks.Contains(renderer);
                bool hasOwnedBlock = ownedNull && renderer.HasPropertyBlock();
                if (hasOwnedBlock && !_scopedPropertyBlocks.Contains(renderer))
                {
                    _ownedNullBlocks.Remove(renderer);
                    ownedNull = false;
                }
                bool hasBaseState = _baseRendererStates.TryGetValue(renderer, out RendererBaseState baseState);
                bool currentForceRenderingOff = renderer.forceRenderingOff;
                bool forceRenderingOff = (hasBaseState ? baseState.ForceRenderingOff : currentForceRenderingOff) || !visible;
                bool canBatchModel = scope.AllowModelBatching && !forceRenderingOff && CanBatchModel(renderer);
                if (ownedNull && !forceRenderingOff && _scopeDepth == 1 && scope.AllowModelBatching && !canBatchModel)
                {
                    _ownedNullBlocks.Remove(renderer);
                    ownedNull = false;
                    renderer.SetPropertyBlock(_emptyPropertyBlock);
                }
                bool rehydrate = ownedNull && !hasOwnedBlock && !forceRenderingOff && _scopeDepth == 1 && !canBatchModel;
                bool captureRendererBlock = capturePropertyBlock || rehydrate;
                if (!captureRendererBlock && currentForceRenderingOff == forceRenderingOff
                    && (ownedNull || renderer.HasPropertyBlock()))
                {
                    if (canBatchModel) _modelBatchingCandidates.Add(renderer);
                    continue;
                }

                MaterialPropertyBlock snapshot = captureRendererBlock
                    ? (_propertyBlocks.Count > 0 ? _propertyBlocks.Pop() : new MaterialPropertyBlock())
                    : null;
                RendererState state = RendererState.Capture(renderer, currentForceRenderingOff, snapshot,
                    ownedNull && !hasOwnedBlock, _scopedPropertyBlocks.Contains(renderer));
                scope.Add(state);
                if (!hasBaseState)
                {
                    baseState = new RendererBaseState(state.ForceRenderingOff, null);
                    _baseRendererStates.Add(renderer, baseState);
                }

                renderer.forceRenderingOff = forceRenderingOff;
                if (rehydrate && !changeColor)
                {
                    renderer.SetPropertyBlock(_emptyPropertyBlock);
                    _scopedPropertyBlocks.Add(renderer);
                }
                if (changeColor)
                {
                    if (!baseState.BaseColor.HasValue)
                    {
                        baseState = new RendererBaseState(baseState.ForceRenderingOff,
                            GetBaseColor(renderer, state.PropertyBlock));
                        _baseRendererStates[renderer] = baseState;
                    }
                    MaterialPropertyBlock propertyBlock = _workingPropertyBlock;
                    renderer.GetPropertyBlock(propertyBlock);
                    Color baseColor = baseState.BaseColor.Value;
                    propertyBlock.SetColor(ShaderPropertyIds.BaseColor,
                        new Color(baseColor.r * opacity, baseColor.g * opacity, baseColor.b * opacity, baseColor.a));
                    renderer.SetPropertyBlock(propertyBlock);
                    if (_ownedNullBlocks.Contains(renderer)) _scopedPropertyBlocks.Add(renderer);
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

        private bool CanBatchModel(Renderer renderer)
        {
            renderer.GetSharedMaterials(_materialBuffer);
            bool matches = _materialBuffer.Count == 1;
            foreach (Material material in _materialBuffer)
            {
                if (!material)
                {
                    matches = false;
                    continue;
                }
                if (!_modelMaterials.TryGetValue(material, out ModelBatching policy))
                {
                    policy = _modelShader && material.shader == _modelShader
                        ? (material.GetVector(ShaderPropertyIds.ShearY).Equals(Vector4.zero)
                            ? ModelBatching.Compatible : ModelBatching.Incompatible)
                        : ModelBatching.Other;
                    _modelMaterials.Add(material, policy);
                }
                // Material-default shear changes the mixed SRP/instancing result.
                if (policy == ModelBatching.Incompatible) _modelDefaultsCompatible = false;
                matches &= policy == ModelBatching.Compatible;
            }
            _materialBuffer.Clear();
            return matches;
        }

        private enum ModelBatching
        {
            Other,
            Compatible,
            Incompatible
        }

        private void CompleteScope(RenderScope scope)
        {
            scope.Restore();
            _scopeDepth--;
            if (_scopeDepth == 0)
            {
                _baseRendererStates.Clear();
                if (_scopedPropertyBlocks.Count > 0) _scopedPropertyBlocks.Clear();
            }
        }

        private static void SetRootsActive(IEnumerable<Transform> roots)
        {
            foreach (Transform root in roots)
                root.gameObject.SetActive(true);
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
            private readonly List<RendererState> _renderers;
            private readonly Vector3 _surfaceGridPosition;
            private readonly Vector3 _caveGridPosition;
            private bool _disposed;
            public bool AllowModelBatching { get; }

            public RenderScope(MapLevelRenderer owner, Vector3 surfaceGridPosition, Vector3 caveGridPosition,
                bool allowModelBatching)
            {
                _owner = owner;
                _renderers = owner._rendererStates.Count > 0 ? owner._rendererStates.Pop() : new List<RendererState>();
                _surfaceGridPosition = surfaceGridPosition;
                _caveGridPosition = caveGridPosition;
                AllowModelBatching = allowModelBatching;
            }

            public void Add(RendererState state)
            {
                _renderers.Add(state);
            }

            public void Restore()
            {
                bool restoreOwned = _owner._ownedNullBlocks.Count > 0 || _owner._scopedPropertyBlocks.Count > 0;
                for (int i = _renderers.Count - 1; i >= 0; i--)
                {
                    if (restoreOwned)
                        _renderers[i].RestoreOwned(_owner._emptyPropertyBlock, _owner._ownedNullBlocks,
                            _owner._scopedPropertyBlocks);
                    else
                        _renderers[i].Restore(_owner._emptyPropertyBlock);
                    MaterialPropertyBlock snapshot = _renderers[i].PropertyBlock;
                    if (snapshot != null)
                    {
                        snapshot.Clear();
                        _owner._propertyBlocks.Push(snapshot);
                    }
                }
                _renderers.Clear();
                _owner._rendererStates.Push(_renderers);

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

            public Color? BaseColor { get; }

            public RendererBaseState(bool forceRenderingOff, Color? baseColor)
            {
                ForceRenderingOff = forceRenderingOff;
                BaseColor = baseColor;
            }
        }

        private readonly struct RendererState
        {
            public Renderer Renderer { get; }
            public bool ForceRenderingOff { get; }
            public MaterialPropertyBlock PropertyBlock { get; }
            private readonly bool _restoreVisibility;
            private readonly bool _restoreAbsence;
            private readonly bool _restoreScopedBlock;

            private RendererState(Renderer renderer, bool forceRenderingOff, MaterialPropertyBlock propertyBlock,
                bool restoreVisibility = true, bool restoreAbsence = false, bool restoreScopedBlock = false)
            {
                Renderer = renderer;
                ForceRenderingOff = forceRenderingOff;
                PropertyBlock = propertyBlock;
                _restoreVisibility = restoreVisibility;
                _restoreAbsence = restoreAbsence;
                _restoreScopedBlock = restoreScopedBlock;
            }

            public static RendererState Capture(Renderer renderer, bool forceRenderingOff, MaterialPropertyBlock propertyBlock,
                bool restoreAbsence = false, bool restoreScopedBlock = false)
            {
                if (propertyBlock != null)
                    renderer.GetPropertyBlock(propertyBlock);
                return new RendererState(renderer, forceRenderingOff, propertyBlock,
                    restoreAbsence: restoreAbsence, restoreScopedBlock: restoreScopedBlock);
            }

            public static RendererState ClearedEmptyBlock(Renderer renderer)
            {
                return new RendererState(renderer, false, null, false);
            }

            public void Restore(MaterialPropertyBlock emptyPropertyBlock)
            {
                if (!Renderer) return;
                if (_restoreVisibility) Renderer.forceRenderingOff = ForceRenderingOff;
                if (PropertyBlock != null)
                    Renderer.SetPropertyBlock(PropertyBlock);
                else if (!Renderer.HasPropertyBlock())
                    // Keep the existing batching path after the renderer's first pass.
                    Renderer.SetPropertyBlock(emptyPropertyBlock);
            }

            public void RestoreOwned(MaterialPropertyBlock emptyPropertyBlock, HashSet<Renderer> ownedNullBlocks,
                HashSet<Renderer> scopedPropertyBlocks)
            {
                if (!Renderer) return;
                if (_restoreVisibility) Renderer.forceRenderingOff = ForceRenderingOff;
                if (PropertyBlock != null)
                    Renderer.SetPropertyBlock(_restoreAbsence && ownedNullBlocks.Contains(Renderer) ? null : PropertyBlock);
                else if (!ownedNullBlocks.Contains(Renderer) && !Renderer.HasPropertyBlock())
                    // Keep the existing batching path after the renderer's first pass.
                    Renderer.SetPropertyBlock(emptyPropertyBlock);
                if (_restoreScopedBlock) scopedPropertyBlocks.Add(Renderer);
                else scopedPropertyBlocks.Remove(Renderer);
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
