using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using Warlander.Deedplanner.Logic.Outlines;

namespace Warlander.Deedplanner.Graphics.Outline
{
    public class ScreenSpaceOutlinePass : ScriptableRenderPass
    {
        private static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");
        private static readonly int MaskTexId = Shader.PropertyToID("_MaskTex");
        private static readonly int MaskTexelSizeId = Shader.PropertyToID("_MaskTex_TexelSize");
        private static readonly int ShearYId = Shader.PropertyToID("_ShearY");

        private readonly Dictionary<OutlineType, Material> _solidMats;
        private readonly Material _dilateMat;

        public IOutlineCoordinator Coordinator { get; set; }
        
        private class MaskPassData
        {
            public TextureHandle MaskTarget;
            public Dictionary<OutlineType, Material> SolidMats;
            public List<OutlineEntry> Snapshot;
            public int ShearYPropertyId;
        }

        private class CompositePassData
        {
            public TextureHandle MaskHandle;
            public TextureHandle CameraColor;
            public TextureHandle TempHandle;
            public Material DilateMat;
            public float MaskWidth;
            public float MaskHeight;
        }

        public ScreenSpaceOutlinePass(Dictionary<OutlineType, Material> solidMats, Material dilateMat)
        {
            _solidMats = solidMats;
            _dilateMat = dilateMat;
            profilingSampler = new ProfilingSampler("ScreenSpaceOutline");
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (Coordinator == null || !Coordinator.HasOutlinedObjects) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData   = frameData.Get<UniversalCameraData>();

            int w = cameraData.cameraTargetDescriptor.width;
            int h = cameraData.cameraTargetDescriptor.height;
            
            var maskDesc = new TextureDesc(w, h)
            {
                colorFormat     = GraphicsFormat.R8G8B8A8_UNorm,
                depthBufferBits = DepthBits.None,
                msaaSamples     = MSAASamples.None,
                filterMode      = FilterMode.Bilinear,
                wrapMode        = TextureWrapMode.Clamp,
                clearBuffer     = true,
                clearColor      = Color.clear,
                name            = "_DPOutlineMask",
            };
            TextureHandle maskHandle = renderGraph.CreateTexture(maskDesc);
            
            var tempDesc = new TextureDesc(w, h)
            {
                colorFormat     = cameraData.cameraTargetDescriptor.graphicsFormat,
                depthBufferBits = DepthBits.None,
                msaaSamples     = MSAASamples.None,
                filterMode      = FilterMode.Bilinear,
                wrapMode        = TextureWrapMode.Clamp,
                name            = "_DPOutlineTemp",
            };
            TextureHandle tempHandle = renderGraph.CreateTexture(tempDesc);
            
            List<OutlineEntry> snapshot = Coordinator.GetOutlinedObjectsSnapshot();

            // ---- Pass 1: render each outlined model as a solid colour into the mask RT ----
            using (var builder = renderGraph.AddUnsafePass<MaskPassData>(
                "DP Outline Mask", out var maskData, profilingSampler))
            {
                maskData.MaskTarget = maskHandle;
                maskData.SolidMats = _solidMats;
                maskData.Snapshot = snapshot;
                maskData.ShearYPropertyId = ShearYId;

                builder.UseTexture(maskHandle, AccessFlags.Write);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (MaskPassData d, UnsafeGraphContext ctx) =>
                {
                    CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    cmd.SetRenderTarget(d.MaskTarget);
                    cmd.ClearRenderTarget(false, true, Color.clear);

                    foreach (OutlineEntry entry in d.Snapshot)
                    {
                        if (!d.SolidMats.TryGetValue(entry.Type, out Material mat)) continue;

                        foreach (Renderer r in entry.Renderers)
                        {
                            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                                continue;

                            // Feed the shear from this renderer's material so the mask aligns
                            // with the geometry as rendered by ModelShader.
                            Vector4 shear = r.sharedMaterial != null
                                ? r.sharedMaterial.GetVector(d.ShearYPropertyId)
                                : Vector4.zero;
                            cmd.SetGlobalVector(d.ShearYPropertyId, shear);

                            int subCount = r.sharedMaterials.Length;
                            for (int sub = 0; sub < subCount; sub++)
                                cmd.DrawRenderer(r, mat, sub, 0);
                        }
                    }
                    // Reset to zero so this global doesn't bleed into subsequent draw calls.
                    cmd.SetGlobalVector(d.ShearYPropertyId, Vector4.zero);
                });
            }

            // ---- Pass 2: dilate mask + composite outline onto scene colour ----
            using (var builder = renderGraph.AddUnsafePass<CompositePassData>(
                "DP Outline Composite", out var compData, profilingSampler))
            {
                compData.MaskHandle  = maskHandle;
                compData.CameraColor = resourceData.activeColorTexture;
                compData.TempHandle  = tempHandle;
                compData.DilateMat   = _dilateMat;
                compData.MaskWidth   = w;
                compData.MaskHeight  = h;

                builder.UseTexture(maskHandle,                      AccessFlags.Read);
                builder.UseTexture(tempHandle,                      AccessFlags.Write);
                builder.UseTexture(resourceData.activeColorTexture, AccessFlags.ReadWrite);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (CompositePassData d, UnsafeGraphContext ctx) =>
                {
                    CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);

                    // Step A: snapshot the camera colour into TempHandle so the shader can
                    // read from a stable copy while we write the composite to the camera.
                    // This also guarantees TempHandle is initialized before use.
                    cmd.CopyTexture(d.CameraColor, d.TempHandle);

                    // Bind TempHandle (scene snapshot) as _BlitTexture, not the live camera.
                    cmd.SetGlobalTexture(BlitTextureId, d.TempHandle);
                    cmd.SetGlobalTexture(MaskTexId, d.MaskHandle);
                    // _MaskTex_TexelSize is NOT auto-populated by SetGlobalTexture.
                    cmd.SetGlobalVector(MaskTexelSizeId, new Vector4(
                        1f / d.MaskWidth, 1f / d.MaskHeight,
                        d.MaskWidth,      d.MaskHeight));

                    // Step B: dilation + composite written directly to the camera colour buffer.
                    // Reads from TempHandle (_BlitTexture), writes to CameraColor — always distinct.
                    cmd.SetRenderTarget(d.CameraColor);
                    cmd.DrawProcedural(Matrix4x4.identity, d.DilateMat, 1, MeshTopology.Triangles, 3);
                });
            }
        }
    }
}
