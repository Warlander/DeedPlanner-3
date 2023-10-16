using System;
using UnityEngine;
using Warlander.Core;

namespace Warlander.UI.Utils
{
    /// <summary>
    /// Helper class to make working with windows more seamless.
    /// Will disable root canvas if there's one on prefab's root level.
    /// </summary>
    public class EditingContentCanvas : WarlanderBehaviour
    {
        [SerializeField] private Canvas _rootCanvas;
        
        private void OnValidate()
        {
            if (_rootCanvas == null)
            {
                return;
            }
            
            // We need to delay canvas update by one frame to give Unity editor time to load the prefab.
            RunNextFrame(() =>
            {
                // There should always be one child if editing prefab in isolation.
                if (transform.childCount == 1)
                {
                    Transform childTransform = transform.GetChild(0);
                    Canvas childCanvas = childTransform.GetComponent<Canvas>();
                    if (childCanvas != null)
                    {
                        childCanvas.gameObject.SetActive(true);
                        childTransform.SetParent(transform.parent);
                        DestroyImmediate(gameObject);
                    }
                }
            });
        }
    }
}