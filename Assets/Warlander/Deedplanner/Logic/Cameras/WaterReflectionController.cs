using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Warlander.Deedplanner.Logic.Cameras
{
    /// <summary>
    /// Manages a shared planar reflection camera for Ultra quality water.
    /// Plain C# class — not a MonoBehaviour. Bound as a singleton; all MultiCamera instances share
    /// one reflection camera and one render texture.
    /// Call RenderForCamera() from MultiCamera's beginCameraRendering hook (Ultra quality only),
    /// passing that camera's source Camera and water Renderer as arguments.
    /// Zenject disposes this singleton (IDisposable) when the scene container is torn down.
    /// </summary>
    public class WaterReflectionController : IDisposable
    {
        private static readonly int ReflectionTexId = Shader.PropertyToID("_ReflectionTex");
        private const int TextureSize = 512;
        private const float ClipPlaneOffset = 0.07f;

        private Camera _reflectionCamera;
        private RenderTexture _reflectionRT;
        private MaterialPropertyBlock _propertyBlock;

        /// <summary>
        /// Render a reflection for the given source camera and push it to the water renderer's
        /// property block. Safe to call from multiple MultiCamera instances per frame — URP renders
        /// cameras sequentially, so the shared RT always holds the correct reflection when the
        /// corresponding camera's water draw occurs.
        /// </summary>
        public void RenderForCamera(Camera sourceCamera, Renderer waterRenderer)
        {
            EnsureReflectionCamera();
            EnsureReflectionRT();
            RenderReflection(sourceCamera, waterRenderer);
        }

        /// <summary>
        /// Destroy the hidden reflection camera and release the render texture.
        /// Called once by Zenject when the scene container is disposed.
        /// </summary>
        public void Dispose()
        {
            if (_reflectionCamera != null)
                UnityEngine.Object.Destroy(_reflectionCamera.gameObject);

            if (_reflectionRT != null)
            {
                _reflectionRT.Release();
                UnityEngine.Object.Destroy(_reflectionRT);
            }
        }

        private void EnsureReflectionCamera()
        {
            if (_reflectionCamera != null) return;

            GameObject camGO = new GameObject("__WaterReflCam__Shared");
            camGO.hideFlags = HideFlags.HideAndDontSave;

            _reflectionCamera = camGO.AddComponent<Camera>();
            _reflectionCamera.enabled = false;

            UniversalAdditionalCameraData addData = camGO.AddComponent<UniversalAdditionalCameraData>();
            addData.renderShadows = false;
            addData.requiresColorOption = CameraOverrideOption.Off;
            addData.requiresDepthOption = CameraOverrideOption.Off;

            _propertyBlock = new MaterialPropertyBlock();
        }

        private void EnsureReflectionRT()
        {
            if (_reflectionRT != null && _reflectionRT.IsCreated() && _reflectionRT.width == TextureSize)
                return;

            _reflectionRT?.Release();
            if (_reflectionRT != null)
                UnityEngine.Object.Destroy(_reflectionRT);

            _reflectionRT = new RenderTexture(TextureSize, TextureSize, 16, RenderTextureFormat.Default);
            _reflectionRT.name = "__WaterReflRT__Shared";
            _reflectionRT.hideFlags = HideFlags.DontSave;
            _reflectionRT.Create();
        }

        private void RenderReflection(Camera sourceCamera, Renderer waterRenderer)
        {
            Vector3 waterNormal = Vector3.up;
            Vector3 waterPos    = waterRenderer.transform.position;

            float     d          = -Vector3.Dot(waterNormal, waterPos) - ClipPlaneOffset;
            Vector4   reflPlane  = new Vector4(waterNormal.x, waterNormal.y, waterNormal.z, d);
            Matrix4x4 reflMatrix = Matrix4x4.zero;
            CalculateReflectionMatrix(ref reflMatrix, reflPlane);

            _reflectionCamera.transform.position    = reflMatrix.MultiplyPoint(sourceCamera.transform.position);
            Vector3 euler = sourceCamera.transform.eulerAngles;
            _reflectionCamera.transform.eulerAngles = new Vector3(-euler.x, euler.y, euler.z);
            _reflectionCamera.worldToCameraMatrix   = sourceCamera.worldToCameraMatrix * reflMatrix;

            Vector4 clipPlane = CameraSpacePlane(_reflectionCamera, waterPos, waterNormal, 1.0f);
            _reflectionCamera.projectionMatrix = sourceCamera.CalculateObliqueMatrix(clipPlane);
            // Preserve correct frustum culling from the source camera perspective
            _reflectionCamera.cullingMatrix    = sourceCamera.projectionMatrix * sourceCamera.worldToCameraMatrix;

            _reflectionCamera.clearFlags       = sourceCamera.clearFlags;
            _reflectionCamera.backgroundColor  = sourceCamera.backgroundColor;
            _reflectionCamera.farClipPlane     = sourceCamera.farClipPlane;
            _reflectionCamera.nearClipPlane    = sourceCamera.nearClipPlane;
            _reflectionCamera.fieldOfView      = sourceCamera.fieldOfView;
            _reflectionCamera.aspect           = sourceCamera.aspect;
            _reflectionCamera.orthographic     = sourceCamera.orthographic;
            _reflectionCamera.orthographicSize = sourceCamera.orthographicSize;
            _reflectionCamera.cullingMask      = ~(1 << 4); // exclude Water layer from its own reflection

            _reflectionCamera.targetTexture = _reflectionRT;
            GL.invertCulling = true;
            _reflectionCamera.Render();
            GL.invertCulling = false;

            _propertyBlock.SetTexture(ReflectionTexId, _reflectionRT);
            waterRenderer.SetPropertyBlock(_propertyBlock);
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
