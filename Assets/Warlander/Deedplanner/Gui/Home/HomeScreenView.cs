using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Persistence;

namespace Warlander.Deedplanner.Gui.Home
{
    public class HomeScreenView : MonoBehaviour, IHomeScreenView
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _newDeedButton;
        [SerializeField] private Button _loadButton;
        [SerializeField] private Button _webLinkButton;
        [SerializeField] private Button _aboutButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Button _patreonButton;
        [SerializeField] private Button _paypalButton;
        [SerializeField] private Button _allSavesButton;
        [SerializeField] private Transform _categoryContainer;
        [SerializeField] private Button _categoryButtonPrototype;
        [SerializeField] private Transform _cardsContainer;
        [SerializeField] private DeedCardView _cardPrototype;

        private static readonly Color SelectedCategoryColor = new Color(0.45f, 0.65f, 0.95f);

        public event Action BackClicked = delegate { };
        public event Action NewDeedClicked = delegate { };
        public event Action LoadClicked = delegate { };
        public event Action WebLinkClicked = delegate { };
        public event Action AboutClicked = delegate { };
        public event Action QuitClicked = delegate { };
        public event Action PatreonClicked = delegate { };
        public event Action PaypalClicked = delegate { };
        public event Action<SaveBackendId?> CategoryClicked = delegate { };
        public event Action<MapLocation> CardClicked = delegate { };
        public event Action<MapLocation> CardDeleteClicked = delegate { };

        private readonly List<Button> _categoryButtons = new List<Button>();
        private readonly Dictionary<MapLocation, DeedCardView> _cards =
            new Dictionary<MapLocation, DeedCardView>();

        private CanvasGroup _fadeGroup;
        private Tween _fadeTween;

        private CanvasGroup FadeGroup
        {
            get
            {
                if (_fadeGroup == null)
                {
                    _fadeGroup = _panel.GetComponent<CanvasGroup>();
                    if (_fadeGroup == null)
                        _fadeGroup = _panel.AddComponent<CanvasGroup>();
                }

                return _fadeGroup;
            }
        }

        public bool Visible => _panel.activeSelf;

        private void Awake()
        {
            _backButton.onClick.AddListener(() => BackClicked());
            _newDeedButton.onClick.AddListener(() => NewDeedClicked());
            _loadButton.onClick.AddListener(() => LoadClicked());
            _webLinkButton.onClick.AddListener(() => WebLinkClicked());
            _aboutButton.onClick.AddListener(() => AboutClicked());
            _quitButton.onClick.AddListener(() => QuitClicked());
            _patreonButton.onClick.AddListener(() => PatreonClicked());
            _paypalButton.onClick.AddListener(() => PaypalClicked());
            _allSavesButton.onClick.AddListener(() => CategoryClicked(null));
        }

        public void Show(bool animated)
        {
            _fadeTween?.Kill();
            gameObject.SetActive(true);
            _panel.SetActive(true);
            FadeGroup.blocksRaycasts = true;

            if (animated)
            {
                FadeGroup.alpha = 0;
                _fadeTween = FadeGroup.DOFade(1, 0.2f);
            }
            else
            {
                FadeGroup.alpha = 1;
            }
        }

        public void Hide(bool animated)
        {
            _fadeTween?.Kill();

            if (animated)
            {
                FadeGroup.blocksRaycasts = false;
                _fadeTween = FadeGroup.DOFade(0, 0.15f)
                    .OnComplete(() =>
                    {
                        _panel.SetActive(false);
                        gameObject.SetActive(false);
                    });
            }
            else
            {
                FadeGroup.alpha = 0;
                _panel.SetActive(false);
                gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            _fadeTween?.Kill();
        }

        public void SetLoadButtonVisible(bool visible)
        {
            _loadButton.gameObject.SetActive(visible);
        }

        public void SetFundingLinksVisible(bool visible)
        {
            _patreonButton.gameObject.SetActive(visible);
            _paypalButton.gameObject.SetActive(visible);
        }

        public void SetCategories(IReadOnlyList<HomeScreenCategory> categories, SaveBackendId? selectedBackendId)
        {
            foreach (Button button in _categoryButtons)
            {
                Destroy(button.gameObject);
            }

            _categoryButtons.Clear();

            TintCategoryButton(_allSavesButton, selectedBackendId == null);

            foreach (HomeScreenCategory category in categories)
            {
                Button button = Instantiate(_categoryButtonPrototype, _categoryContainer);
                button.name = "Category " + category.Label;
                button.GetComponentInChildren<TMPro.TMP_Text>().text = category.Label;
                SaveBackendId backendId = category.BackendId;
                button.onClick.AddListener(() => CategoryClicked(backendId));
                button.gameObject.SetActive(true);
                TintCategoryButton(button, selectedBackendId == backendId);
                _categoryButtons.Add(button);
            }
        }

        public void SetCards(IReadOnlyList<HomeScreenCardData> cards)
        {
            foreach (DeedCardView card in _cards.Values)
            {
                Destroy(card.gameObject);
            }

            _cards.Clear();

            foreach (HomeScreenCardData data in cards)
            {
                DeedCardView card = Instantiate(_cardPrototype, _cardsContainer);
                card.name = "Card " + data.Name;
                card.SetData(data);
                MapLocation location = data.Location;
                card.Clicked += () => CardClicked(location);
                card.DeleteClicked += () => CardDeleteClicked(location);
                card.gameObject.SetActive(true);
                _cards[location] = card;
            }
        }

        public void UpdateCard(MapLocation location, HomeScreenCardData data)
        {
            if (_cards.TryGetValue(location, out DeedCardView card))
            {
                card.SetData(data);
            }
        }

        private static void TintCategoryButton(Button button, bool selected)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = selected ? SelectedCategoryColor : Color.white;
            button.colors = colors;
        }
    }
}
