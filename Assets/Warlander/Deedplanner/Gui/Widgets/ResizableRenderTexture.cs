using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Gui.Widgets
{
    
    [RequireComponent(typeof(RawImage))]
    [RequireComponent(typeof(RectTransform))]
    public class ResizableRenderTexture : MonoBehaviour
    {
        
        public Camera renderCamera;
        
        private RawImage rawImage;

        private void Awake()
        {
            rawImage = GetComponent<RawImage>();
        }

        private void Update()
        {
            RectTransform rectTransform = transform as RectTransform;
            Vector2 size = RectTransformUtility.CalculateRelativeRectTransformBounds(rectTransform).size;
            Vector3 scale = transform.lossyScale;
            int width = (int) (size.x * scale.x);
            int height = (int) (size.y * scale.y);
            if (width <= 0 || height <= 0)
            {
                return;
            }

            Texture oldTexture = rawImage.texture;
            if (oldTexture && (oldTexture.width != width || oldTexture.height != height))
            {
                Destroy(oldTexture);
                RenderTexture renderTexture = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32);
                rawImage.texture = renderTexture;
                UpdateRenderCamera();
            }
            else if (!oldTexture)
            {
                RenderTexture renderTexture = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32);
                rawImage.texture = renderTexture;
                UpdateRenderCamera();
            }
        }

        private void UpdateRenderCamera()
        {
            if (renderCamera)
            {
                RenderTexture renderTexture = (RenderTexture) rawImage.texture;
                renderCamera.targetTexture = renderTexture;
                renderCamera.aspect = (float) renderTexture.width / renderTexture.height;
            }
        }
    }

}