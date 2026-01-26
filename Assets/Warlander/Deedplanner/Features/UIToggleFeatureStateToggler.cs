using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Warlander.Deedplanner.Features
{
    [RequireComponent(typeof(Toggle))]
    public class UIToggleFeatureStateToggler : MonoBehaviour
    {
        [SerializeField] private Feature _feature;
        [SerializeField] private Toggle _toggle;

        private IFeatureStateRetriever _featureStateRetriever;

        [Inject]
        private void Construct(IFeatureStateRetriever featureStateRetriever)
        {
            _featureStateRetriever = featureStateRetriever;
        }

        private void Start()
        {
            UpdateToggle();
        }

        private void UpdateToggle()
        {
            _toggle.interactable = _featureStateRetriever.IsFeatureEnabled(_feature);
        }
    }
}
