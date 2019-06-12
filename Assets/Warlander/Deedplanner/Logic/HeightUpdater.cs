﻿using System;
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
        private Color selectedHoveredColor = new Color(0.7f, 0.39f, 0f);
        [SerializeField]
        private Color activeColor = new Color(1, 0, 0, 1);

        private List<HeightmapHandle> currentFrameHoveredHandles = new List<HeightmapHandle>();
        private List<HeightmapHandle> lastFrameHoveredHandles = new List<HeightmapHandle>();
        private List<HeightmapHandle> selectedHandles = new List<HeightmapHandle>();
        private List<HeightmapHandle> deselectedHandles = new List<HeightmapHandle>();

        private bool manipulating = false;
        private bool dragging = false;
        private Vector2 dragStartPos;
        private Vector2 dragEndPos;

        private void OnEnable()
        {
            LayoutManager.Instance.TileSelectionMode = TileSelectionMode.Nothing;
        }

        private void Update()
        {
            RaycastHit raycast = LayoutManager.Instance.CurrentCamera.CurrentRaycast;
            bool cameraOnScreen = LayoutManager.Instance.CurrentCamera.MouseOver;

            if (!cameraOnScreen)
            {
                return;
            }

            currentFrameHoveredHandles = UpdateHoveredHandles(raycast);

            if (Input.GetMouseButtonDown(0))
            {
                if (currentFrameHoveredHandles.Count == 1 && selectedHandles.Contains(currentFrameHoveredHandles[0]))
                {
                    manipulating = true;
                }
                else if (Input.GetKey(KeyCode.LeftShift))
                {
                    dragging = true;
                }
                else
                {
                    deselectedHandles = selectedHandles;
                    selectedHandles = new List<HeightmapHandle>();
                    dragging = true;
                }
            }
            
            if (Input.GetMouseButtonUp(0))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    selectedHandles.AddRange(lastFrameHoveredHandles);
                }
                else if (!manipulating)
                {
                    deselectedHandles = selectedHandles;
                    selectedHandles = lastFrameHoveredHandles;
                }
                manipulating = false;
                dragging = false;
            }

            if (Input.GetMouseButtonDown(1))
            {
                dragging = false;
                manipulating = false;
                LayoutManager.Instance.CurrentCamera.RenderSelectionBox = false;
            }

            UpdateHandlesColors();
            deselectedHandles = new List<HeightmapHandle>();
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
            
            if (dragging)
            {
                dragEndPos = LayoutManager.Instance.CurrentCamera.MousePosition;

                if (!manipulating && Vector2.Distance(dragStartPos, dragEndPos) > 5)
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
        
        private void UpdateHandlesColors()
        {
            foreach (HeightmapHandle handle in currentFrameHoveredHandles)
            {
                if (!selectedHandles.Contains(handle))
                {
                    handle.Color = hoveredColor;
                }
            }
            
            foreach (HeightmapHandle handle in lastFrameHoveredHandles)
            {
                if (!currentFrameHoveredHandles.Contains(handle) && !selectedHandles.Contains(handle))
                {
                    handle.Color = neutralColor;
                }
            }
            
            foreach (HeightmapHandle handle in selectedHandles)
            {
                if (manipulating)
                {
                    handle.Color = activeColor;
                }
                else if (currentFrameHoveredHandles.Count == 1 && currentFrameHoveredHandles.Contains(handle) && !dragging)
                {
                    handle.Color = selectedHoveredColor;
                }
                else
                {
                    handle.Color = selectedColor;
                }
            }
            
            foreach (HeightmapHandle handle in deselectedHandles)
            {
                handle.Color = neutralColor;
            }
        }

    }
}