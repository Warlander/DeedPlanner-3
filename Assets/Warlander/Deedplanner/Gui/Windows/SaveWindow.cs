using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Gui.Widgets;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Utils;
using Warlander.UI.Windows;
using Zenject;

namespace Warlander.Deedplanner.Gui.Windows
{
    public class SaveWindow : MonoBehaviour
    {
        [Inject] private Window _window;
        [Inject] private GameManager _gameManager;

        [SerializeField] private Button _saveToFileButton;
        [SerializeField] private Button _pastebinButton;
        [SerializeField] private TMP_Dropdown _pastebinDropdown;
        [SerializeField] private Button _webVersionButton;
        [SerializeField] private TMP_Dropdown _webVersionDropdown;

        private UnityWebRequest _currentPastebinRequest;

        private void Start()
        {
            _saveToFileButton.onClick.AddListener(SaveToFileOnClick);
            _pastebinButton.onClick.AddListener(PastebinOnClick);
            _webVersionButton.onClick.AddListener(WebVersionOnClick);
        }

        private void SaveToFileOnClick()
        {
            Map map = _gameManager.Map;

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
            
#if UNITY_WEBGL && !UNITY_EDITOR
            JavaScriptUtils.DownloadNative("Deed plan.MAP", mapString);
#else
            ExtensionFilter[] extensionArray = {
                new ExtensionFilter("DeedPlanner 3 save", "MAP")
            };
            string path = StandaloneFileBrowser.SaveFilePanel("Save Map", "", "Deed plan", extensionArray);
        
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
#endif
            _window.Close();
        }

        private void PastebinOnClick()
        {
            PostMapToPastebin(_pastebinDropdown.value, OnPastebinUploadComplete);
        }

        private void WebVersionOnClick()
        {
            PostMapToPastebin(_webVersionDropdown.value, OnWebVersionUploadComplete);
        }

        private void PostMapToPastebin(int expirationValueIndex, Action<AsyncOperation> completedCallback)
        {
            _pastebinButton.interactable = false;
            _webVersionButton.interactable = false;
            
            Map map = _gameManager.Map;

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

            _currentPastebinRequest = UnityWebRequest.Post(requestLink, form);
            UnityWebRequestAsyncOperation operation = _currentPastebinRequest.SendWebRequest();
            operation.completed += completedCallback;
        }

        private void OnPastebinUploadComplete(AsyncOperation obj)
        {
            _pastebinButton.interactable = true;
            _webVersionButton.interactable = true;

            if (!string.IsNullOrEmpty(_currentPastebinRequest.error))
            {
                Debug.LogError(_currentPastebinRequest.error);
            }

            string response = _currentPastebinRequest.downloadHandler.text;
            _currentPastebinRequest.Dispose();
            if (response.Contains("Bad API request"))
            {
                Debug.LogError(response);
            }
            else
            {
                Application.OpenURL(response);
            }
            
            _window.Close();
        }

        private void OnWebVersionUploadComplete(AsyncOperation obj)
        {
            _pastebinButton.interactable = true;
            _webVersionButton.interactable = true;
            
            string response = _currentPastebinRequest.downloadHandler.text;
            if (response.Contains("Bad API request"))
            {
                Debug.LogError(response);
            }
            else
            {
                string webVersionUrl = Constants.WebVersionLink + "?map=" + response;
                Application.OpenURL(webVersionUrl);
            }

            _window.Close();
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