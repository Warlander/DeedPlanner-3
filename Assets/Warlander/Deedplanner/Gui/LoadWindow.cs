using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using StandaloneFileBrowser;
using TMPro;
using UnityEngine;
using Warlander.Deedplanner.Gui.Widgets;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Gui
{
    public class LoadWindow : MonoBehaviour
    {
        
        [SerializeField] private Window window = null;
        [SerializeField] private TMP_InputField pastebinInput = null;

        public void OnFileLoad()
        {
            if (Properties.Web)
            {
                LoadFileBrowser();
            }
            else
            {
                LoadFileStandalone();
                window.HideWindow();
            }
        }

        private void LoadFileBrowser()
        {
            JavaScriptUtils.UploadNative(gameObject.name, nameof(LoadFileBrowserCallback));
        }

        public void LoadFileBrowserCallback(string result)
        {
            if (string.IsNullOrEmpty(result))
            {
                return;
            }
            
            GameManager.Instance.LoadMap(result);
            window.HideWindow();
        }

        private void LoadFileStandalone()
        {
            ExtensionFilter[] extensionArray = {
                new ExtensionFilter("DeedPlanner 3 save", "MAP")
            };
            
            string[] pathArray = StandaloneFileBrowser.StandaloneFileBrowser.OpenFilePanel("Load Map", "", extensionArray, false);
            if (pathArray == null || pathArray.Length != 1)
            {
                return;
            }

            string path = pathArray[0];
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            
            byte[] mapBytes = File.ReadAllBytes(path);
            string mapString = Encoding.Default.GetString(mapBytes);

            GameManager.Instance.LoadMap(mapString);
        }

        public void OnWebLoad()
        {
            string rawLink = pastebinInput.text;

            try
            {
                string requestLink = WebLinkUtils.ParseToDirectDownloadLink(rawLink);

                byte[] downloadedBytes = WebUtils.ReadUrlToByteArray(requestLink);
                string downloadedString = Encoding.Default.GetString(downloadedBytes);
                
                // try to decompress the map, or load directly if it's not compressed
                try
                {
                    byte[] compressedBytes = Convert.FromBase64String(downloadedString);
                    byte[] pasteBytes = Decompress(compressedBytes);
                    string pasteString = Encoding.Default.GetString(pasteBytes);

                    GameManager.Instance.LoadMap(pasteString);
                }
                catch
                {
                    GameManager.Instance.LoadMap(downloadedString);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Unable to load map from: " + rawLink);
                if (Debug.isDebugBuild)
                {
                    Debug.LogError(ex);
                }
            }
            finally
            {
                window.HideWindow();
            }
        }
        
        private byte[] Decompress(byte[] gzip)
        {
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    
                    return memory.ToArray();
                }
            }
        }
    }
}