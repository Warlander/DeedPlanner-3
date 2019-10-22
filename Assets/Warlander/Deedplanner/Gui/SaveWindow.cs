using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using StandaloneFileBrowser;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Gui
{
    public class SaveWindow : MonoBehaviour
    {

        [SerializeField] private Button pastebinButton = null;
        [SerializeField] private TMP_Dropdown pastebinDropdown = null;

        [SerializeField] private Button webVersionButton = null;
        [SerializeField] private TMP_Dropdown webVersionDropdown = null;

        private UnityWebRequest currentPastebinRequest;

        public void OnFileSave()
        {
            Map map = GameManager.Instance.Map;

            if (!map)
            {
                return;
            }

            StringBuilder build = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Indent = true;

            using (XmlWriter xmlWriter = XmlWriter.Create(build, settings))
            {
                XmlDocument document = new XmlDocument();
                map.Serialize(document, null);
                document.Save(xmlWriter);
            }

            string mapString = build.ToString();
            
            if (Properties.Web)
            {
                JavaScriptUtils.DownloadNative("Deed plan.MAP", mapString);
            }
            else
            {
                ExtensionFilter[] extensionArray = {
                    new ExtensionFilter("DeedPlanner 3 save", "MAP")
                };
                string path = StandaloneFileBrowser.StandaloneFileBrowser.SaveFilePanel("Save Map", "", "Deed plan", extensionArray);
            
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                if (!path.EndsWith(".MAP"))
                {
                    path += ".MAP";
                }
            
                byte[] bytes = Encoding.Default.GetBytes(build.ToString());
                File.WriteAllBytes(path, bytes);
            }

            gameObject.SetActive(false);
        }

        public void OnPastebinSave()
        {
            PostMapToPastebin(pastebinDropdown.value, OnPastebinUploadComplete);
        }

        public void OnWebVersionSave()
        {
            PostMapToPastebin(webVersionDropdown.value, OnWebVersionUploadComplete);
        }
        
        private void PostMapToPastebin(int expirationValueIndex, Action<AsyncOperation> completedCallback)
        {
            pastebinButton.interactable = false;
            webVersionButton.interactable = false;
            
            Map map = GameManager.Instance.Map;

            if (!map)
            {
                return;
            }
            
            XmlDocument document = new XmlDocument();
            map.Serialize(document, null);
            byte[] bytes = Encoding.Default.GetBytes(document.OuterXml);
            byte[] compressedBytes = Compress(bytes);
            string base64String = Convert.ToBase64String(compressedBytes);
            
            WWWForm form = new WWWForm();
            form.AddField("api_dev_key", "24844c99ae9971a2da79a2f7d0da7642");
            form.AddField("api_paste_private", "1");
            form.AddField("api_paste_name", "DeedPlanner map");
            form.AddField("api_option", "paste");
            form.AddField("api_paste_expire_date", ParseExpirationDateIndex(expirationValueIndex));
            form.AddField("api_paste_code", base64String);

            const string pastebinApiEndpoint = "https://pastebin.com/api/api_post.php";
            string requestLink = WebLinkUtils.AsCrossOriginLink(pastebinApiEndpoint);

            currentPastebinRequest = UnityWebRequest.Post(requestLink, form);
            UnityWebRequestAsyncOperation operation = currentPastebinRequest.SendWebRequest();
            operation.completed += completedCallback;
        }

        private void OnPastebinUploadComplete(AsyncOperation obj)
        {
            pastebinButton.interactable = true;
            webVersionButton.interactable = true;
            
            string response = currentPastebinRequest.downloadHandler.text;
            currentPastebinRequest.Dispose();
            if (response.Contains("Bad API request"))
            {
                Debug.LogError(response);
            }
            else
            {
                Application.OpenURL(response);
            }
            
            gameObject.SetActive(false);
        }

        private void OnWebVersionUploadComplete(AsyncOperation obj)
        {
            pastebinButton.interactable = true;
            webVersionButton.interactable = true;
            
            string response = currentPastebinRequest.downloadHandler.text;
            if (response.Contains("Bad API request"))
            {
                Debug.LogError(response);
            }
            else
            {
                string webVersionUrl = Constants.WebVersionLink + "?map=" + response;
                Application.OpenURL(webVersionUrl);
            }

            gameObject.SetActive(false);
        }

        private byte[] Compress(byte[] raw)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(memory,
                    CompressionMode.Compress, true))
                {
                    gzip.Write(raw, 0, raw.Length);
                }
                
                return memory.ToArray();
            }
        }

        private string ParseExpirationDateIndex(int index)
        {
            switch (index)
            {
                case 0:
                    return "10M";
                case 1:
                    return "1H";
                case 2:
                    return "1D";
                case 3:
                    return "1W";
                case 4:
                    return "2W";
                case 5:
                    return "1M";
                default:
                    return "N";
            }
        }

    }
}