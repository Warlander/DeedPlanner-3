#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Collections.Generic;
using System.IO;
using B83.Win32;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Warlander.Deedplanner.Logic.Saving;

namespace Warlander.Deedplanner.Logic
{
    public class FileDragManager : IInitializable, IDisposable
    {
        [Inject] private ISaveCoordinator _saveCoordinator;

        void IInitializable.Initialize()
        {
            UnityDragAndDropHook.InstallHook();
            UnityDragAndDropHook.OnDroppedFiles += OnFileDropped;
        }

        private async void OnFileDropped(List<string> files, POINT point)
        {
            if (files.Count != 1 || !files[0].EndsWith(".MAP"))
            {
                return;
            }

            string path = files[0];
            await _saveCoordinator.LoadAsync(new MapLocation(SaveBackendId.File, path, Path.GetFileNameWithoutExtension(path)));
        }
        
        void IDisposable.Dispose()
        {
            UnityDragAndDropHook.UninstallHook();
            UnityDragAndDropHook.OnDroppedFiles -= OnFileDropped;
        }
    }
}
#endif