using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Gui;

namespace Warlander.Deedplanner.Logic
{
    public class HeightUpdater : MonoBehaviour
    {

        [SerializeField]
        private Color neutralColor = Color.white;
        [SerializeField]
        private Color hoveredColor = new Color(0.7f, 0.7f, 0, 1);
        [SerializeField]
        private Color selectedColor = new Color(0, 1, 0, 1);
        [SerializeField]
        private Color activeColor = new Color(1, 0, 0, 1);

        private List<HeightmapHandle> lastFrameHoveredHandles = new List<HeightmapHandle>();
        private List<HeightmapHandle> selectedHandles = new List<HeightmapHandle>();
        
        private Vector2 dragStartPos;
        private Vector2 dragEndPos;

        private void OnEnable()
        {
            LayoutManager.Instance.TileSelectionMode = TileSelectionMode.Nothing;
        }

        private void Update()
        {
            RaycastHit raycast = LayoutManager.Instance.CurrentCamera.CurrentRaycast;

            Ground ground = raycast.transform ? raycast.transform.GetComponent<Ground>() : null;
            
            List<HeightmapHandle> currentFrameHoveredHandles = UpdateHoveredHandles(raycast);

            foreach (HeightmapHandle handle in currentFrameHoveredHandles)
            {
                handle.Color = hoveredColor;
            }

            if (Input.GetMouseButtonDown(0))
            {
                foreach (HeightmapHandle handle in selectedHandles)
                {
                    handle.Color = neutralColor;
                }

                selectedHandles = new List<HeightmapHandle>();
            }
            
            if (Input.GetMouseButtonUp(0))
            {
                foreach (HeightmapHandle handle in lastFrameHoveredHandles)
                {
                    handle.Color = selectedColor;
                }

                selectedHandles = lastFrameHoveredHandles;
            }
            else
            {
                foreach (HeightmapHandle handle in lastFrameHoveredHandles)
                {
                    if (!currentFrameHoveredHandles.Contains(handle))
                    {
                        handle.Color = neutralColor;
                    }
                }
            }
            

            lastFrameHoveredHandles = currentFrameHoveredHandles;
        }

        private List<HeightmapHandle> UpdateHoveredHandles(RaycastHit raycast)
        {
            List<HeightmapHandle> hoveredHandles = new List<HeightmapHandle>();


            if (Input.GetMouseButtonDown(0))
            {
                dragStartPos = LayoutManager.Instance.CurrentCamera.MousePosition;
                dragEndPos = dragStartPos;
            }
            
            if (Input.GetMouseButton(0))
            {
                dragEndPos = LayoutManager.Instance.CurrentCamera.MousePosition;

                if (Vector2.Distance(dragStartPos, dragEndPos) > 5)
                {
                    LayoutManager.Instance.CurrentCamera.RenderSelectionBox = true;
                }
                
                Vector2 difference = dragEndPos - dragStartPos;
                float clampedDifferenceX = Mathf.Clamp(-difference.x, 0, float.MaxValue);
                float clampedDifferenceY = Mathf.Clamp(-difference.y, 0, float.MaxValue);
                Vector2 clampedDifference = new Vector2(clampedDifferenceX, clampedDifferenceY);

                Vector2 selectionStart = dragStartPos - clampedDifference;
                Vector2 selectionEnd = dragEndPos - dragStartPos + clampedDifference * 2;

                LayoutManager.Instance.CurrentCamera.SelectionBoxPosition = selectionStart;
                LayoutManager.Instance.CurrentCamera.SelectionBoxSize = selectionEnd;

                Vector2 viewportStart = selectionStart / LayoutManager.Instance.CurrentCamera.Screen.GetComponent<RectTransform>().sizeDelta;
                Vector2 viewportEnd = selectionEnd / LayoutManager.Instance.CurrentCamera.Screen.GetComponent<RectTransform>().sizeDelta;
                Rect viewportRect = new Rect(viewportStart, viewportEnd);

                Camera checkedCamera = LayoutManager.Instance.CurrentCamera.AttachedCamera;

                for (int i = 0; i <= GameManager.Instance.Map.Width; i++)
                {
                    for (int i2 = 0; i2 < GameManager.Instance.Map.Height; i2++)
                    {
                        float height = GameManager.Instance.Map[i, i2].GetHeightForFloor(LayoutManager.Instance.CurrentCamera.Floor) * 0.1f;
                        Vector2 viewportLocation = checkedCamera.WorldToViewportPoint(new Vector3(i * 4, height, i2 * 4));
                        if (viewportRect.Contains(viewportLocation))
                        {
                            hoveredHandles.Add(GameManager.Instance.Map.SurfaceGridMesh.GetHandle(i, i2));
                        }
                    }
                }
            }
            
            if (Input.GetMouseButtonUp(0))
            {
                LayoutManager.Instance.CurrentCamera.RenderSelectionBox = false;
            }
            
            if (hoveredHandles.Count == 0)
            {
                HeightmapHandle heightmapHandle = raycast.transform ? raycast.transform.GetComponent<HeightmapHandle>() : null;
                if (heightmapHandle)
                {
                    hoveredHandles.Add(heightmapHandle);
                }
            }
            
            return hoveredHandles;
        }

    }
}
