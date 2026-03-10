using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Warlander.Deedplanner.Logic.Cameras
{
    /// <summary>
    /// Manages a planar reflection camera for Ultra quality water.
    /// Plain C# class — not a MonoBehaviour. Unity lifecycle calls are delegated from MultiCamera.
    /// Call Initialize() once from MultiCamera.Start() to wire the source camera and water renderer.
    /// Call OnBeginCameraRendering() from MultiCamera's beginCameraRendering hook (Ultra quality only).
    /// Call Dispose() from MultiCamera.OnDestroy() to clean up GPU resources.
    /// </summary>
    public class WaterReflectionController
    {
        private static readonly int ReflectionTexId = Shader.PropertyToID("_ReflectionTex");
        private const int TextureSize = 512;
        private const float ClipPlaneOffset = 0.07f;

        private Camera _sourceCamera;
        private Renderer _waterRenderer;
        private Camera _reflectionCamera;
        private RenderTexture _reflectionRT;
        private MaterialPropertyBlock _propertyBlock;

        /// <summary>
        /// Wire the source camera and the shared ComplexWater renderer.
        /// Called by MultiCamera.Start() after Zenject injection and Awake have both run.
        /// </summary>
        public void Initialize(Camera sourceCamera, Renderer waterRenderer)
        {
            _sourceCamera = sourceCamera;
            _waterRenderer = waterRenderer;
            _propertyBlock = new MaterialPropertyBlock();
            CreateReflectionCamera();
        }

        /// <summary>
        /// Render this camera's reflection and push it to the global _ReflectionTex slot.
        /// Must be called from MultiCamera's beginCameraRendering hook, before the camera renders.
        /// Cameras render sequentially in URP, so setting the global texture here is safe —
        /// each camera reads its own RT during its own render pass.
        /// </summary>
        public void OnBeginCameraRendering()
        {
            if (_reflectionCamera == null) return;
            EnsureReflectionRT();
            RenderReflection();
        }

        /// <summary>
        /// Destroy the hidden reflection camera and release the render texture.
        /// Called by MultiCamera.OnDestroy().
        /// </summary>
        public void Dispose()
        {
            if (_reflectionCamera != null)
                Object.Destroy(_reflectionCamera.gameObject);

            if (_reflectionRT != null)
            {
                _reflectionRT.Release();
                Object.Destroy(_reflectionRT);
            }
        }

        private void CreateReflectionCamera()
        {
            GameObject camGO = new GameObject("__WaterReflCam__" + _sourceCamera.GetInstanceID());
            camGO.hideFlags = HideFlags.HideAndDontSave;

            _reflectionCamera = camGO.AddComponent<Camera>();
            _reflectionCamera.enabled = false;

            UniversalAdditionalCameraData addData = camGO.AddComponent<UniversalAdditionalCameraData>();
            addData.renderShadows = false;
            addData.requiresColorOption = CameraOverrideOption.Off;
            addData.requiresDepthOption = CameraOverrideOption.Off;
        }

        private void EnsureReflectionRT()
        {
            if (_reflectionRT != null && _reflectionRT.IsCreated() && _reflectionRT.width == TextureSize)
                return;

            _reflectionRT?.Release();
            if (_reflectionRT != null)
                Object.Destroy(_reflectionRT);

            _reflectionRT = new RenderTexture(TextureSize, TextureSize, 16, RenderTextureFormat.Default);
            _reflectionRT.name = "__WaterReflRT__" + _sourceCamera.GetInstanceID();
            _reflectionRT.hideFlags = HideFlags.DontSave;
            _reflectionRT.Create();
        }

        private void RenderReflection()
        {
            Vector3 waterNormal = Vector3.up;
            Vector3 waterPos    = _waterRenderer.transform.position;

            float     d          = -Vector3.Dot(waterNormal, waterPos) - ClipPlaneOffset;
            Vector4   reflPlane  = new Vector4(waterNormal.x, waterNormal.y, waterNormal.z, d);
            Matrix4x4 reflMatrix = Matrix4x4.zero;
            CalculateReflectionMatrix(ref reflMatrix, reflPlane);

            _reflectionCamera.transform.position    = reflMatrix.MultiplyPoint(_sourceCamera.transform.position);
            Vector3 euler = _sourceCamera.transform.eulerAngles;
            _reflectionCamera.transform.eulerAngles = new Vector3(-euler.x, euler.y, euler.z);
            _reflectionCamera.worldToCameraMatrix   = _sourceCamera.worldToCameraMatrix * reflMatrix;

            Vector4 clipPlane = CameraSpacePlane(_reflectionCamera, waterPos, waterNormal, 1.0f);
            _reflectionCamera.projectionMatrix = _sourceCamera.CalculateObliqueMatrix(clipPlane);
            // Preserve correct frustum culling from the source camera perspective
            _reflectionCamera.cullingMatrix    = _sourceCamera.projectionMatrix * _sourceCamera.worldToCameraMatrix;

            _reflectionCamera.clearFlags       = _sourceCamera.clearFlags;
            _reflectionCamera.backgroundColor  = _sourceCamera.backgroundColor;
            _reflectionCamera.farClipPlane     = _sourceCamera.farClipPlane;
            _reflectionCamera.nearClipPlane    = _sourceCamera.nearClipPlane;
            _reflectionCamera.fieldOfView      = _sourceCamera.fieldOfView;
            _reflectionCamera.aspect           = _sourceCamera.aspect;
            _reflectionCamera.orthographic     = _sourceCamera.orthographic;
            _reflectionCamera.orthographicSize = _sourceCamera.orthographicSize;
            _reflectionCamera.cullingMask      = ~(1 << 4); // exclude Water layer from its own reflection

            _reflectionCamera.targetTexture = _reflectionRT;
            GL.invertCulling = true;
            _reflectionCamera.Render();
            GL.invertCulling = false;

            _propertyBlock.SetTexture(ReflectionTexId, _reflectionRT);
            _waterRenderer.SetPropertyBlock(_propertyBlock);
        }

        // -------------------------------------------------------------------
        // Matrix helpers — ported from Standard Assets Water.cs
        // -------------------------------------------------------------------

        private static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane)
        {
            reflectionMat.m00 =  1f - 2f * plane[0] * plane[0];
            reflectionMat.m01 =      -2f * plane[0] * plane[1];
            reflectionMat.m02 =      -2f * plane[0] * plane[2];
            reflectionMat.m03 =      -2f * plane[3] * plane[0];

            reflectionMat.m10 =      -2f * plane[1] * plane[0];
            reflectionMat.m11 =  1f - 2f * plane[1] * plane[1];
            reflectionMat.m12 =      -2f * plane[1] * plane[2];
            reflectionMat.m13 =      -2f * plane[3] * plane[1];

            reflectionMat.m20 =      -2f * plane[2] * plane[0];
            reflectionMat.m21 =      -2f * plane[2] * plane[1];
            reflectionMat.m22 =  1f - 2f * plane[2] * plane[2];
            reflectionMat.m23 =      -2f * plane[3] * plane[2];

            reflectionMat.m30 = 0f;
            reflectionMat.m31 = 0f;
            reflectionMat.m32 = 0f;
            reflectionMat.m33 = 1f;
        }

        private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
        {
            Vector3 offsetPos = pos + normal * ClipPlaneOffset;
            Matrix4x4 m = cam.worldToCameraMatrix;
            Vector3 cpos    = m.MultiplyPoint(offsetPos);
            Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
        }
    }
}
