using System.Collections.Generic;
using System.IO;
using B83.Win32;
using UnityEngine;

namespace Warlander.Deedplanner.Logic
{
    public class FileDragManager : MonoBehaviour
    {
        #if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private void OnEnable()
        {
            UnityDragAndDropHook.InstallHook();
            UnityDragAndDropHook.OnDroppedFiles += OnFileDropped;
        }

        private void OnFileDropped(List<string> files, POINT point)
        {
            if (files.Count != 1 || !files[0].EndsWith(".MAP"))
            {
                return;
            }
            
            GameManager.Instance.LoadMap(File.ReadAllText(files[0]));
        }
        
        private void OnDisable()
        {
            UnityDragAndDropHook.UninstallHook();
            UnityDragAndDropHook.OnDroppedFiles -= OnFileDropped;
        }
        #else
        public void Awake()
        {
            // Immediately destroy the manager on other platforms than Windows.
            Destroy(gameObject);
        }
        #endif
    }
}