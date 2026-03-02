using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Graphics.Outline
{
    public class ScreenSpaceOutlineFeature : ScriptableRendererFeature
    {
        [SerializeField][Range(1f, 20f)] private float outlineWidth = 5f;

        private IOutlineCoordinator _coordinator;
        private ScreenSpaceOutlinePass _pass;

        private Dictionary<OutlineType, Material> _solidMats;
        private Material _dilateMat;

        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineWidthId  = Shader.PropertyToID("_OutlineWidth");

        public void RegisterCoordinator(IOutlineCoordinator coordinator)
        {
            _coordinator = coordinator;
            if (_pass != null) _pass.Coordinator = coordinator;
        }

        public void UnregisterCoordinator()
        {
            _coordinator = null;
            if (_pass != null) _pass.Coordinator = null;
        }

        public override void Create()
        {
            Shader shader = Shader.Find("DeedPlanner/ScreenSpaceOutline");
            if (shader == null)
            {
                Debug.LogError("[ScreenSpaceOutlineFeature] Shader 'DeedPlanner/ScreenSpaceOutline' not found.");
                return;
            }

            _solidMats = new Dictionary<OutlineType, Material>
            {
                { OutlineType.Neutral,  CreateSolidMat(shader, Color.white) },
                { OutlineType.Positive, CreateSolidMat(shader, Color.green) },
                { OutlineType.Negative, CreateSolidMat(shader, Color.red)   },
            };

            _dilateMat = CoreUtils.CreateEngineMaterial(shader);
            _dilateMat.name = "DP_DilateMat";
            _dilateMat.SetFloat(OutlineWidthId, outlineWidth);

            _pass = new ScreenSpaceOutlinePass(_solidMats, _dilateMat)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingOpaques,
                Coordinator = _coordinator,
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_pass == null || _dilateMat == null) return;
            if (_coordinator == null || !_coordinator.HasOutlinedObjects) return;

            // Keep the dilation width in sync with the inspector value at runtime.
            _dilateMat.SetFloat(OutlineWidthId, outlineWidth);

            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            _coordinator = null;
            if (_solidMats != null)
            {
                foreach (Material mat in _solidMats.Values)
                    CoreUtils.Destroy(mat);
                _solidMats = null;
            }
            CoreUtils.Destroy(_dilateMat);
            _dilateMat = null;
        }

        private static Material CreateSolidMat(Shader shader, Color color)
        {
            var mat = CoreUtils.CreateEngineMaterial(shader);
            mat.name = $"DP_SolidMat_{color}";
            mat.SetColor(OutlineColorId, color);
            return mat;
        }
    }
}
