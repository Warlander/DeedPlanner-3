using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using R3;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Gui.Widgets;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Settings;
using Warlander.Deedplanner.Utils;
using Warlander.UI.Windows;
using Zenject;

namespace Warlander.Deedplanner.Gui.Windows
{
    public class LoadWindow : MonoBehaviour
    {
        [Inject] private Window _window;
        [Inject] private GameManager _gameManager;

        [SerializeField] private Button _loadFromFileButton;
        [SerializeField] private Button _loadFromWebButton;
        [SerializeField] private TMP_InputField _pastebinInput = null;
        [SerializeField] private GameObject _webSaveGroup;

        private void Start()
        {
            _loadFromFileButton.onClick.AddListener(LoadFromFileOnClick);
            _loadFromWebButton.onClick.AddListener(LoadFromWebOnClick);
            
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                _webSaveGroup.gameObject.SetActive(false);
            }
        }

        private void LoadFromFileOnClick()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                LoadFileBrowser();
            }
            else
            {
                LoadFileStandalone();
            }
        }

        private void LoadFromWebOnClick()
        {
            string rawLink = _pastebinInput.text;
            
            string requestLink = WebLinkUtils.ParseToDirectDownloadLink(rawLink);

            WebUtils.ReadUrlToByteArray(requestLink)
                .ToObservable()
                .Subscribe(downloadedBytes =>
                {
                    string downloadedString = Encoding.Default.GetString(downloadedBytes);
            
                    // try to decompress the map, or load directly if it's not compressed
                    try
                    {
                        byte[] compressedBytes = Convert.FromBase64String(downloadedString);
                        byte[] pasteBytes = Decompress(compressedBytes);
                        string pasteString = Encoding.Default.GetString(pasteBytes);

                        _gameManager.LoadMap(pasteString);
                    }
                    catch
                    {
                        _gameManager.LoadMap(downloadedString);
                    }
                },
                completion =>
                {
                    if (completion.IsFailure)
                    {
                        Debug.LogWarning("Unable to load map from: " + rawLink);
                        if (Debug.isDebugBuild)
                        {
                            Debug.LogError(completion.Exception);
                        }
                    }
                    
                    _window.Close();
                })
                .AddTo(this);
        }

        private void LoadFileBrowser()
        {
#if UNITY_WEBGL
            JavaScriptUtils.UploadNative(gameObject.name, nameof(LoadFileBrowserCallback));
#endif
        }

        public void LoadFileBrowserCallback(string result)
        {
            if (string.IsNullOrEmpty(result))
            {
                return;
            }
            
            _gameManager.LoadMap(result);
            _window.Close();
        }

        private void LoadFileStandalone()
        {
            FileBrowser.SetFilters(false, new FileBrowser.Filter("DeedPlanner 3 save", "MAP"));
            FileBrowser.ShowLoadDialog(OnLoadSuccess, OnLoadCancel, FileBrowser.PickMode.Files, title: "Load Map", loadButtonText: "Load");
        }

        private void OnLoadSuccess(string[] pathArray)
        {
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

            _gameManager.LoadMap(mapString);
            _window.Close();
        }

        private void OnLoadCancel()
        {
            
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