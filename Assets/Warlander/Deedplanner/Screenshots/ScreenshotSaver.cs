using Warlander.Deedplanner.Platform.Web;
using System;
using System.IO;
using UnityEngine;
using Warlander.Deedplanner.Logging;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Screenshots
{
    public class ScreenshotSaver
    {
        public static readonly LogCategory Category = new LogCategory("Screenshots");

        private readonly ICategoryLogger _logger;
        private bool _folderOpened;

        public ScreenshotSaver(ILoggerSource loggerSource)
        {
            _logger = loggerSource.Create(Category);
        }

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
            _logger.Message($"Screenshot saved to {path}");

            if (!_folderOpened)
            {
                _folderOpened = true;
                try
                {
                    Application.OpenURL($"file://{directory}");
                }
                catch (Exception e)
                {
                    _logger.Warning($"Failed to open screenshots folder: {e.Message}");
                }
            }
#endif
        }
    }
}
