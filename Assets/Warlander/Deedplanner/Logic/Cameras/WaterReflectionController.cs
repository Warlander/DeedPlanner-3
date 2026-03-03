using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Warlander.Deedplanner.Logic.Cameras
{
    /// <summary>
    /// Manages a planar reflection camera for Ultra quality water.
    /// Attach to the Ultra Quality Water GameObject alongside a MeshRenderer.
    /// Call SetMainCamera from MultiCamera.Awake() to wire the source camera.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class WaterReflectionController : MonoBehaviour
    {
        [SerializeField] private int _textureSize = 512;
        [SerializeField] private LayerMask _reflectLayers = -1;
        [SerializeField] private float _clipPlaneOffset = 0.07f;

        private Camera _sourceCamera;
        private Camera _reflectionCamera;
        private RenderTexture _reflectionRT;
        private Material _waterMaterialInstance;

        // Static guard: prevents the reflection camera from recursively triggering
        // another reflection render when beginCameraRendering fires for it.
        private static bool s_RenderingReflection;

        private static readonly int ReflectionTexId = Shader.PropertyToID("_ReflectionTex");

        private void Awake()
        {
            // Obtain an instanced material so each water object has its own _ReflectionTex
            // binding. .material creates the instance; .sharedMaterial would be shared across all.
            _waterMaterialInstance = GetComponent<Renderer>().material;
        }

        /// <summary>
        /// Wire the source camera this water object belongs to.
        /// Called by MultiCamera.Awake() immediately after obtaining AttachedCamera.
        /// </summary>
        public void SetMainCamera(Camera sourceCamera)
        {
            if (_sourceCamera != null)
            {
                Debug.LogWarning("[WaterReflectionController] SetMainCamera called more than once.", this);
                return;
            }

            _sourceCamera = sourceCamera;
            CreateReflectionCamera();
            EnsureRenderTexture();
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private void CreateReflectionCamera()
        {
            GameObject camGO = new GameObject("__WaterReflCam__" + _sourceCamera.GetInstanceID());
            camGO.hideFlags = HideFlags.HideAndDontSave;
            camGO.transform.SetParent(transform, false);

            _reflectionCamera = camGO.AddComponent<Camera>();
            // Disabled so URP never auto-renders this camera in its own pass
            _reflectionCamera.enabled = false;

            UniversalAdditionalCameraData addData = camGO.AddComponent<UniversalAdditionalCameraData>();
            addData.renderShadows = false;
            addData.requiresColorOption = CameraOverrideOption.Off;
            addData.requiresDepthOption = CameraOverrideOption.Off;
        }

        private void EnsureRenderTexture()
        {
            if (_reflectionRT != null && _reflectionRT.IsCreated() && _reflectionRT.width == _textureSize)
                return;

            if (_reflectionRT != null)
            {
                _reflectionRT.Release();
                Destroy(_reflectionRT);
            }

            _reflectionRT = new RenderTexture(_textureSize, _textureSize, 16, RenderTextureFormat.Default);
            _reflectionRT.name = "__WaterReflRT__" + GetInstanceID();
            _reflectionRT.hideFlags = HideFlags.DontSave;
            _reflectionRT.Create();
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _sourceCamera) return;
            if (s_RenderingReflection) return;
            if (_reflectionCamera == null) return;
            if (!gameObject.activeInHierarchy) return;

            EnsureRenderTexture();
            RenderReflection();
        }

        private void RenderReflection()
        {
            s_RenderingReflection = true;
            try
            {
                Vector3 waterNormal = Vector3.up;
                Vector3 waterPos    = transform.position;

                // Build the reflection matrix about the water plane
                float    d           = -Vector3.Dot(waterNormal, waterPos) - _clipPlaneOffset;
                Vector4  reflPlane   = new Vector4(waterNormal.x, waterNormal.y, waterNormal.z, d);
                Matrix4x4 reflMatrix = Matrix4x4.zero;
                CalculateReflectionMatrix(ref reflMatrix, reflPlane);

                // Mirror camera position
                Vector3 newCamPos = reflMatrix.MultiplyPoint(_sourceCamera.transform.position);
                _reflectionCamera.transform.position = newCamPos;

                // Flip pitch to get mirrored orientation
                Vector3 euler = _sourceCamera.transform.eulerAngles;
                _reflectionCamera.transform.eulerAngles = new Vector3(-euler.x, euler.y, euler.z);

                // Reflected worldToCameraMatrix
                _reflectionCamera.worldToCameraMatrix = _sourceCamera.worldToCameraMatrix * reflMatrix;

                // Oblique projection matrix clips geometry below the water surface for free
                Vector4 clipPlane = CameraSpacePlane(_reflectionCamera, waterPos, waterNormal, 1.0f);
                _reflectionCamera.projectionMatrix = _sourceCamera.CalculateObliqueMatrix(clipPlane);

                // Culling matrix preserves correct frustum culling from the source camera perspective
                _reflectionCamera.cullingMatrix = _sourceCamera.projectionMatrix * _sourceCamera.worldToCameraMatrix;

                // Mirror source camera settings
                _reflectionCamera.clearFlags      = _sourceCamera.clearFlags;
                _reflectionCamera.backgroundColor = _sourceCamera.backgroundColor;
                _reflectionCamera.farClipPlane    = _sourceCamera.farClipPlane;
                _reflectionCamera.nearClipPlane   = _sourceCamera.nearClipPlane;
                _reflectionCamera.fieldOfView     = _sourceCamera.fieldOfView;
                _reflectionCamera.aspect          = _sourceCamera.aspect;
                _reflectionCamera.orthographic    = _sourceCamera.orthographic;
                _reflectionCamera.orthographicSize = _sourceCamera.orthographicSize;

                // Never render the Water layer in its own reflection
                _reflectionCamera.cullingMask = ~(1 << 4) & _reflectLayers.value;

                _reflectionCamera.targetTexture = _reflectionRT;

                GL.invertCulling = true;
                _reflectionCamera.Render();
                GL.invertCulling = false;

                _waterMaterialInstance.SetTexture(ReflectionTexId, _reflectionRT);
            }
            finally
            {
                s_RenderingReflection = false;
            }
        }

        private void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

            if (_reflectionCamera != null)
                Destroy(_reflectionCamera.gameObject);

            if (_reflectionRT != null)
            {
                _reflectionRT.Release();
                Destroy(_reflectionRT);
            }

            // Destroy the instanced material to avoid memory leaks
            if (_waterMaterialInstance != null)
                Destroy(_waterMaterialInstance);
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
            Vector3 offsetPos = pos + normal * _clipPlaneOffset;
            Matrix4x4 m = cam.worldToCameraMatrix;
            Vector3 cpos    = m.MultiplyPoint(offsetPos);
            Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
        }
    }
}
