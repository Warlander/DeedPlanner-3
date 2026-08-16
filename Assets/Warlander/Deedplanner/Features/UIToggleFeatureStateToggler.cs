using UnityEngine;
using UnityEngine.UI;
using VContainer;
using Warlogic.Features;

namespace Warlander.Deedplanner.Features
{
    [RequireComponent(typeof(Toggle))]
    public class UIToggleFeatureStateToggler : MonoBehaviour
    {
        [SerializeField] private Feature _feature;
        [SerializeField] private Toggle _toggle;

        private IFeatureStateRetriever<Feature> _featureStateRetriever;

        [Inject]
        private void Construct(IFeatureStateRetriever<Feature> featureStateRetriever)
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
