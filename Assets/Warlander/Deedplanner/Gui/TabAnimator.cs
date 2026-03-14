using UnityEngine;
using DG.Tweening;
using Warlander.Deedplanner.Logic;
using Zenject;

namespace Warlander.Deedplanner.Gui
{
    public class TabAnimator : MonoBehaviour
    {
        [SerializeField] private UIContentTab[] tabs = new UIContentTab[12];

        private TabContext _tabContext;

        private Tab _displayedTab;
        private Sequence _tabFadeSequence;

        [Inject]
        private void Inject(TabContext tabContext)
        {
            _tabContext = tabContext;
        }

        private void Start()
        {
            _displayedTab = _tabContext.CurrentTab;
            _tabContext.TabChanged += OnTabContextTabChanged;

            SwitchToTabInstantly(_displayedTab);
        }

        private void OnTabContextTabChanged(Tab newTab)
        {
            CreateAndStartTabFadeAnimation(_displayedTab, newTab);
            _displayedTab = newTab;
        }

        private void SwitchToTabInstantly(Tab tab)
        {
            _tabFadeSequence?.Complete(true);

            foreach (UIContentTab contentTab in tabs)
            {
                bool isTarget = contentTab.Tab == tab;
                contentTab.gameObject.SetActive(isTarget);
                if (isTarget)
                    contentTab.FadeGroup.alpha = 1;
            }

            _displayedTab = tab;
        }

        private void CreateAndStartTabFadeAnimation(Tab previousTab, Tab newTab)
        {
            _tabFadeSequence?.Complete(true);

            UIContentTab previousTabObject = FindObjectForTab(previousTab);
            UIContentTab newTabObject = FindObjectForTab(newTab);

            newTabObject.FadeGroup.alpha = 0;

            Sequence newSequence = DOTween.Sequence();
            newSequence.Append(previousTabObject.FadeGroup.DOFade(0, 0.15f));
            newSequence.AppendCallback(() =>
            {
                previousTabObject.gameObject.SetActive(false);
                newTabObject.gameObject.SetActive(true);
            });
            newSequence.Append(newTabObject.FadeGroup.DOFade(1, 0.2f));
            newSequence.OnKill(() => _tabFadeSequence = null);

            _tabFadeSequence = newSequence;
        }

        private UIContentTab FindObjectForTab(Tab tab)
        {
            foreach (UIContentTab uiTab in tabs)
            {
                if (uiTab.Tab == tab)
                    return uiTab;
            }

            return null;
        }

        private void OnDestroy()
        {
            _tabFadeSequence?.Kill();
            _tabContext.TabChanged -= OnTabContextTabChanged;
        }
    }
}
