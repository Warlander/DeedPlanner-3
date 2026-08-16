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
        [SerializeField] private LayoutElement _warningBoxLayout;
        [SerializeField] private GameObject _feasibilityBox;
        [SerializeField] private TMP_Text _feasibilityText;
        [SerializeField] private LayoutElement _feasibilityBoxLayout;

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
            _infoText.text = $"{_mapHandler.Map.DisplayName} · {sizeKb} KB";

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
                SaveBackendId backendId = backend.Id;
                button.onClick.AddListener(() => ActBackend(backendId));
                button.gameObject.SetActive(true);
                _actionButtons.Add(button);

                if (backend.IsVolatile && volatileBackend == null)
                {
                    volatileBackend = backend;
                }
            }

            if (volatileBackend != null)
            {
                ShowBox(_warningBox, _warningText, _warningBoxLayout, volatileBackend.VolatileWarning);
            }

            _feasibilityBox.SetActive(false);
        }

        private async void ActBackend(SaveBackendId backendId)
        {
            ISaveBackend backend = _saveCoordinator.GetBackend(backendId);
            if (backend == null)
            {
                return;
            }

            SaveFeasibility feasibility = backend.CheckSave(PayloadSizeFor(backend));
            if (!feasibility.Possible)
            {
                ShowBox(_feasibilityBox, _feasibilityText, _feasibilityBoxLayout, feasibility.Reason);
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
                ShowBox(_feasibilityBox, _feasibilityText, _feasibilityBoxLayout, e.Message);
            }
            finally
            {
                SetActionButtonsInteractable(true);
            }
        }

        private bool _boxHeightsDirty;

        private void OnEnable()
        {
            Canvas.willRenderCanvases += OnWillRenderCanvases;
        }

        private void OnDisable()
        {
            Canvas.willRenderCanvases -= OnWillRenderCanvases;
        }

        // runs after the layout pass, when box widths are real
        private void OnWillRenderCanvases()
        {
            if (!_boxHeightsDirty)
            {
                return;
            }

            _boxHeightsDirty = false;
            ApplyBoxHeight(_warningBox, _warningText, _warningBoxLayout);
            ApplyBoxHeight(_feasibilityBox, _feasibilityText, _feasibilityBoxLayout);
        }

        private static void ApplyBoxHeight(GameObject box, TMP_Text text, LayoutElement layout)
        {
            if (!box.activeSelf)
            {
                return;
            }

            text.ForceMeshUpdate();
            layout.minHeight = text.preferredHeight + 8f;
        }

        private void ShowBox(GameObject box, TMP_Text text, LayoutElement layout, string message)
        {
            text.text = message;
            box.SetActive(true);
            _boxHeightsDirty = true;
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
            return backend.Id == SaveBackendId.SteamCloud ? _gzipSize : _gzipSize * 4 / 3;
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
