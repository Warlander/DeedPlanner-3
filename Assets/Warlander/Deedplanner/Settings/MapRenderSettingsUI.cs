using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Warlander.Deedplanner.Settings
{
    public class MapRenderSettingsUI : MonoBehaviour
    {
        [Inject] private IMapRenderSettingsSetter _mapRenderSettingsSetter;

        [SerializeField] private Toggle _objectsToggle;
        [SerializeField] private Toggle _treesToggle;
        [SerializeField] private Toggle _bushesToggle;
        [SerializeField] private Toggle _shipsToggle;

        private void Awake()
        {
            _objectsToggle.onValueChanged.AddListener(newValue => _mapRenderSettingsSetter.RenderDecorations = newValue);
            _treesToggle.onValueChanged.AddListener(newValue => _mapRenderSettingsSetter.RenderTrees = newValue);
            _bushesToggle.onValueChanged.AddListener(newValue => _mapRenderSettingsSetter.RenderBushes = newValue);
            _shipsToggle.onValueChanged.AddListener(newValue => _mapRenderSettingsSetter.RenderShips = newValue);
        }
    }
}