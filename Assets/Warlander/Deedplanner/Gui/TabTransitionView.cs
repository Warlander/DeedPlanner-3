using DG.Tweening;
using UnityEngine;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Gui
{
    public class TabTransitionView : MonoBehaviour, ITabTransitionView
    {
        [SerializeField] private UIContentTab[] _tabs = new UIContentTab[12];

        private Tab _displayedTab;
        private Sequence _tabFadeSequence;

        public void ShowTab(Tab tab, bool animated)
        {
            if (animated)
                AnimateToTab(tab);
            else
                ShowTabInstantly(tab);
        }

        private void ShowTabInstantly(Tab tab)
        {
            _tabFadeSequence?.Complete(true);

            foreach (UIContentTab contentTab in _tabs)
            {
                bool isTarget = contentTab.Tab == tab;
                contentTab.gameObject.SetActive(isTarget);
                if (isTarget)
                    contentTab.FadeGroup.alpha = 1;
            }

            _displayedTab = tab;
        }

        private void AnimateToTab(Tab tab)
        {
            _tabFadeSequence?.Complete(true);

            UIContentTab previousTabObject = FindTabObject(_displayedTab);
            UIContentTab newTabObject = FindTabObject(tab);

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
            _displayedTab = tab;
        }

        private UIContentTab FindTabObject(Tab tab)
        {
            foreach (UIContentTab uiTab in _tabs)
            {
                if (uiTab.Tab == tab)
                    return uiTab;
            }

            return null;
        }

        private void OnDestroy()
        {
            _tabFadeSequence?.Kill();
        }
    }
}
