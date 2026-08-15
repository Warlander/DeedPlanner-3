using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Gui
{
    public class DeedCardView : MonoBehaviour
    {
        private static readonly Color MissingColor = new Color(0.75f, 0.2f, 0.2f);
        private static readonly Color UnknownColor = new Color(0.4f, 0.45f, 0.5f);
        private static readonly Color VolatileColor = new Color(0.85f, 0.55f, 0.15f);
        private static readonly Color RecoveryColor = new Color(0.2f, 0.55f, 0.5f);

        [SerializeField] private Button _button;
        [SerializeField] private RawImage _thumbnail;
        [SerializeField] private GameObject _thumbnailPlaceholder;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private TMP_Text _hintText;
        [SerializeField] private TMP_Text _badgeText;
        [SerializeField] private GameObject _chipObject;
        [SerializeField] private TMP_Text _chipText;
        [SerializeField] private Image _chipBackground;

        public event Action Clicked;

        private void Awake()
        {
            _button.onClick.AddListener(() => Clicked?.Invoke());
        }

        public void SetData(HomeScreenCardData data)
        {
            _nameText.text = data.Name;
            _timeText.text = data.TimeText;
            _badgeText.text = data.BadgeText;

            bool hasHint = !string.IsNullOrEmpty(data.LocationHint);
            _hintText.gameObject.SetActive(hasHint);
            if (hasHint)
            {
                // keep the tail: it is the distinguishing part of a long path
                const int maxHintLength = 34;
                string hint = data.LocationHint;
                _hintText.text = hint.Length > maxHintLength
                    ? "…" + hint.Substring(hint.Length - maxHintLength + 1)
                    : hint;
            }

            bool hasThumbnail = data.Thumbnail != null;
            _thumbnail.gameObject.SetActive(hasThumbnail);
            _thumbnailPlaceholder.SetActive(!hasThumbnail);
            if (hasThumbnail)
            {
                _thumbnail.texture = data.Thumbnail;
            }

            if (data.Chip == HomeScreenChip.None)
            {
                _chipObject.SetActive(false);
            }
            else
            {
                _chipObject.SetActive(true);
                switch (data.Chip)
                {
                    case HomeScreenChip.Missing:
                        _chipText.text = "Missing";
                        _chipBackground.color = MissingColor;
                        break;
                    case HomeScreenChip.Unknown:
                        _chipText.text = "Unknown";
                        _chipBackground.color = UnknownColor;
                        break;
                    case HomeScreenChip.Volatile:
                        _chipText.text = "volatile";
                        _chipBackground.color = VolatileColor;
                        break;
                    case HomeScreenChip.Recovery:
                        _chipText.text = "Recover";
                        _chipBackground.color = RecoveryColor;
                        break;
                }
            }
        }
    }
}
