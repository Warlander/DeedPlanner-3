using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using StandaloneFileBrowser;
using TMPro;
using UnityEngine;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Gui
{
    public class LoadWindow : MonoBehaviour
    {

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
            }
            
            gameObject.SetActive(false);
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
            gameObject.SetActive(false);
        }

        private void LoadFileStandalone()
        {
            ExtensionFilter[] extensionArray = {
                new ExtensionFilter("DeedPlanner 3 save", "MAP"),
            };
            
            string[] pathArray = StandaloneFileBrowser.StandaloneFileBrowser.OpenFilePanel("Load Map", "", extensionArray, false);
            if (pathArray == null || pathArray.Length != 1)
            {
                return;
            }

            string path = pathArray[0];
            byte[] mapBytes = File.ReadAllBytes(path);
            string mapString = Encoding.Default.GetString(mapBytes);

            GameManager.Instance.LoadMap(mapString);
        }

        public void OnPastebinLoad()
        {
            string rawLink = pastebinInput.text;

            if (!rawLink.Contains("pastebin") || !rawLink.Contains("/"))
            {
                return;
            }

            string requestLink;
            if (rawLink.Contains("raw"))
            {
                requestLink = rawLink;
            }
            else
            {
                string pasteId = rawLink.Substring(rawLink.LastIndexOf("/") + 1);
                requestLink = "https://pastebin.com/raw.php?i=" + pasteId;
            }

            if (Properties.Web)
            {
                requestLink = "https://cors-anywhere.herokuapp.com/" + requestLink;
            }

            byte[] base64Bytes = WebUtils.ReadUrlToByteArray(requestLink);
            string base64String = Encoding.Default.GetString(base64Bytes);
            byte[] compressedBytes = Convert.FromBase64String(base64String);
            byte[] pasteBytes = Decompress(compressedBytes);
            string pasteString = Encoding.Default.GetString(pasteBytes);

            GameManager.Instance.LoadMap(pasteString);
            gameObject.SetActive(false);
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