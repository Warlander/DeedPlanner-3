using UnityEngine;
using UnityEngine.UI;
using VContainer;
using Warlogic.Features;

namespace Warlander.Deedplanner.Features
{
    [RequireComponent(typeof(Toggle))]
    public class UIToggleFeatureStateToggler : MonoBehaviour
    {
        [SerializeField] private string _featureName;
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
            _toggle.interactable = _featureStateRetriever.IsFeatureEnabled(_featureName);
        }
    }
}
