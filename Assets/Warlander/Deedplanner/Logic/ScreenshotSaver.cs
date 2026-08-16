using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Logic
{
    public class ScreenshotSaver
    {
        private bool _folderOpened;

        public void Save(Texture2D texture)
        {
            byte[] pngBytes = texture.EncodeToPNG();
            UnityEngine.Object.Destroy(texture);

            string filename = $"DeedPlanner_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";

#if UNITY_WEBGL && !UNITY_EDITOR
            JavaScriptUtils.DownloadBinary(filename, pngBytes);
#else
            string directory = Path.Combine(Application.persistentDataPath, "Screenshots");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, filename);
            File.WriteAllBytes(path, pngBytes);
            UnityEngine.Debug.Log($"Screenshot saved to {path}");

            if (!_folderOpened)
            {
                _folderOpened = true;
                Process.Start(new ProcessStartInfo { FileName = directory, UseShellExecute = true });
            }
#endif
        }
    }
}
