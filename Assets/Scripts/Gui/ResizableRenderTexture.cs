using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Gui
{

    [ExecuteInEditMode]
    [RequireComponent(typeof(RawImage))]
    [RequireComponent(typeof(RectTransform))]
    public class ResizableRenderTexture : MonoBehaviour
    {

        public Camera renderCamera;

        private void Start()
        {
            OnRectTransformDimensionsChange();
        }

        private void OnRectTransformDimensionsChange()
        {
            RectTransform rectTransform = transform as RectTransform;
            RawImage rawImage = GetComponent<RawImage>();
            float width = rectTransform.sizeDelta.x;
            float height = rectTransform.sizeDelta.y;
            if (width <= 0 || height <= 0)
            {
                return;
            }

            RenderTexture renderTexture = new RenderTexture((int) width, (int) height, 16);
            rawImage.texture = renderTexture;

            if (renderCamera)
            {
                renderCamera.targetTexture = renderTexture;
                renderCamera.aspect = width / height;
            }
        }

    }

}