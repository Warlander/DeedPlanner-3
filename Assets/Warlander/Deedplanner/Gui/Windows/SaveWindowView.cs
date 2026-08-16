using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Logic.Saving;
using VContainer;
using Warlander.UI.Windows;

namespace Warlander.Deedplanner.Gui.Windows
{
    public class SaveWindowView : MonoBehaviour
    {
        private Window _window;
        [Inject] private ISaveCoordinator _saveCoordinator;
        [Inject] private MapHandler _mapHandler;

        [SerializeField] private TMP_Text _infoText;
        [SerializeField] private Transform _saveContainer;
        [SerializeField] private Transform _exportContainer;
        [SerializeField] private Button _actionButtonPrototype;
        [SerializeField] private GameObject _warningBox;
        [SerializeField] private TMP_Text _warningText;
        [SerializeField] private GameObject _feasibilityBox;
        [SerializeField] private TMP_Text _feasibilityText;

        private readonly List<Button> _actionButtons = new List<Button>();
        private string _payload;
        private long _gzipSize = -1;

        private void Awake()
        {
            _window = GetComponentInParent<Window>(true);
        }

        private void Start()
        {
            _payload = _saveCoordinator.SerializeCurrentMap();
            long sizeKb = Encoding.UTF8.GetByteCount(_payload) / 1024;
            _infoText.text = $"Save map · {_mapHandler.Map.DisplayName} · {sizeKb} KB";

            ISaveBackend volatileBackend = null;
            foreach (ISaveBackend backend in _saveCoordinator.Backends)
            {
                if (!backend.IsAvailable || (backend.Capabilities & SaveCapabilities.Save) == 0)
                {
                    continue;
                }

                Transform container = (backend.Capabilities & SaveCapabilities.Overwrite) != 0
                    ? _saveContainer
                    : _exportContainer;

                Button button = Instantiate(_actionButtonPrototype, container);
                button.name = backend.DisplayName + " Button";
                button.GetComponentInChildren<TMP_Text>().text = backend.DisplayName;
                string backendId = backend.Id;
                button.onClick.AddListener(() => ActBackend(backendId));
                button.gameObject.SetActive(true);
                _actionButtons.Add(button);

                if (backend.IsVolatile && volatileBackend == null)
                {
                    volatileBackend = backend;
                }
            }

            _warningBox.SetActive(volatileBackend != null);
            if (volatileBackend != null)
            {
                _warningText.text = VolatileWarning(volatileBackend.Id);
            }

            _feasibilityBox.SetActive(false);
        }

        private async void ActBackend(string backendId)
        {
            ISaveBackend backend = _saveCoordinator.GetBackend(backendId);
            if (backend == null)
            {
                return;
            }

            SaveFeasibility feasibility = backend.CheckSave(PayloadSizeFor(backend));
            if (!feasibility.Possible)
            {
                _feasibilityBox.SetActive(true);
                _feasibilityText.text = feasibility.Reason;
                return;
            }

            SetActionButtonsInteractable(false);
            try
            {
                MapLocation? location = await _saveCoordinator.SaveAsync(backendId);
                if (location.HasValue)
                {
                    _window.Close();
                }
            }
            catch (Exception e)
            {
                _feasibilityBox.SetActive(true);
                _feasibilityText.text = e.Message;
            }
            finally
            {
                SetActionButtonsInteractable(true);
            }
        }

        private void SetActionButtonsInteractable(bool interactable)
        {
            foreach (Button button in _actionButtons)
            {
                button.interactable = interactable;
            }
        }

        private long PayloadSizeFor(ISaveBackend backend)
        {
            long rawSize = Encoding.UTF8.GetByteCount(_payload);
            if (!backend.CompressesOutput)
            {
                return rawSize;
            }

            if (_gzipSize < 0)
            {
                _gzipSize = Compress(Encoding.UTF8.GetBytes(_payload)).LongLength;
            }

            // text targets carry the payload as base64
            return backend.Id == "steamcloud" ? _gzipSize : _gzipSize * 4 / 3;
        }

        private static string VolatileWarning(string backendId)
        {
            switch (backendId)
            {
                case "pastebin":
                    return "Pastebin is not permanent storage. Pastes can be removed by Pastebin at any time. Keep a local file copy of any map you care about.";
                case "localstorage":
                    return "Browser storage can be wiped. Clearing site data, private browsing, or browser cleanup tools will delete maps saved here. Export important maps as files.";
                default:
                    return "This save location is not permanent storage.";
            }
        }

        private static byte[] Compress(byte[] raw)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                using (GZipStream stream = new GZipStream(memory, CompressionMode.Compress, true))
                {
                    stream.Write(raw, 0, raw.Length);
                }

                return memory.ToArray();
            }
        }
    }
}
