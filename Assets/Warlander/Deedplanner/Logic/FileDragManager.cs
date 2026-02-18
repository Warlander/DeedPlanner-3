#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Collections.Generic;
using System.IO;
using B83.Win32;
using UnityEngine;
using Zenject;

namespace Warlander.Deedplanner.Logic
{
    public class FileDragManager : IInitializable, IDisposable
    {
        [Inject] private MapHandler _mapHandler;
        
        void IInitializable.Initialize()
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
            
            _mapHandler.LoadMap(File.ReadAllText(files[0]));
        }
        
        void IDisposable.Dispose()
        {
            UnityDragAndDropHook.UninstallHook();
            UnityDragAndDropHook.OnDroppedFiles -= OnFileDropped;
        }
    }
}
#endif