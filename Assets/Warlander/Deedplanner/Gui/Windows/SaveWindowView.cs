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
        [Inject] private SaveCoordinator _saveCoordinator;
        [Inject] private MapHandler _mapHandler;

        [SerializeField] private TMP_Text _headerText;
        [SerializeField] private Transform _backendListContainer;
        [SerializeField] private SaveBackendRowView _rowPrototype;
        [SerializeField] private TMP_Text _sectionHeaderPrototype;
        [SerializeField] private GameObject _warningBox;
        [SerializeField] private TMP_Text _warningText;
        [SerializeField] private GameObject _feasibilityBox;
        [SerializeField] private TMP_Text _feasibilityText;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Button _saveButton;

        private readonly List<SaveBackendRowView> _rows = new List<SaveBackendRowView>();
        private string _selectedBackendId;
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
            _headerText.text = $"Save map · {_mapHandler.Map.DisplayName} · {sizeKb} KB";

            var saveBackends = new List<ISaveBackend>();
            var exportBackends = new List<ISaveBackend>();
            foreach (ISaveBackend backend in _saveCoordinator.Backends)
            {
                if ((backend.Capabilities & SaveCapabilities.Save) == 0)
                {
                    continue;
                }

                if ((backend.Capabilities & SaveCapabilities.Overwrite) != 0)
                {
                    saveBackends.Add(backend);
                }
                else
                {
                    exportBackends.Add(backend);
                }
            }

            BuildSection("Save", saveBackends);
            BuildSection("Export", exportBackends);

            _cancelButton.onClick.AddListener(() => _window.Close());
            _saveButton.onClick.AddListener(SaveOnClick);

            if (_rows.Count > 0)
            {
                SelectBackend(_rows[0].name.Substring("Backend ".Length));
            }
        }

        private void BuildSection(string title, List<ISaveBackend> backends)
        {
            if (backends.Count == 0)
            {
                return;
            }

            TMP_Text header = Instantiate(_sectionHeaderPrototype, _backendListContainer);
            header.name = title + " Header";
            header.text = title;
            header.gameObject.SetActive(true);

            foreach (ISaveBackend backend in backends)
            {
                SaveBackendRowView row = Instantiate(_rowPrototype, _backendListContainer);
                row.name = "Backend " + backend.Id;
                row.SetData(backend.DisplayName, Describe(backend.Id));
                string backendId = backend.Id;
                row.Clicked += () => SelectBackend(backendId);
                row.gameObject.SetActive(true);
                _rows.Add(row);
            }
        }

        private void SelectBackend(string backendId)
        {
            _selectedBackendId = backendId;
            int index = 0;
            foreach (SaveBackendRowView row in _rows)
            {
                string rowBackendId = row.name.Substring("Backend ".Length);
                row.SetSelected(rowBackendId == backendId);
                index++;
            }

            RefreshNotices();
        }

        private void RefreshNotices()
        {
            ISaveBackend backend = _saveCoordinator.GetBackend(_selectedBackendId);
            if (backend == null)
            {
                return;
            }

            _warningBox.SetActive(backend.IsVolatile);
            if (backend.IsVolatile)
            {
                _warningText.text = VolatileWarning(backend.Id);
            }

            SaveFeasibility feasibility = backend.CheckSave(PayloadSizeFor(backend));
            _feasibilityBox.SetActive(!feasibility.Possible);
            if (!feasibility.Possible)
            {
                _feasibilityText.text = feasibility.Reason;
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

        private async void SaveOnClick()
        {
            ISaveBackend backend = _saveCoordinator.GetBackend(_selectedBackendId);
            if (backend == null)
            {
                return;
            }

            SaveFeasibility feasibility = backend.CheckSave(PayloadSizeFor(backend));
            if (!feasibility.Possible)
            {
                RefreshNotices();
                return;
            }

            _saveButton.interactable = false;
            try
            {
                MapLocation? location = await _saveCoordinator.SaveAsync(_selectedBackendId);
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
                _saveButton.interactable = true;
            }
        }

        private static string Describe(string backendId)
        {
            switch (backendId)
            {
                case "file": return "Any folder, full tracking, quick save and auto-save supported.";
                case "pastebin": return "Creates a new permanent paste you can share. No quick save, no status tracking.";
                case "webfile": return "Downloads a .MAP file through your browser.";
                case "steamcloud": return "Synced across your PCs. Name only, no folders.";
                case "localstorage": return "Stored in this browser. Can be wiped by browser cleanup.";
                default: return "";
            }
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
