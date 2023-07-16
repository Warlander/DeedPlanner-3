using TMPro;
using UnityEngine;

namespace Warlander.Deedplanner.Gui.Windows
{
    public class CreditsWindow : MonoBehaviour
    {
        [SerializeField] private TextAsset _creditsTextAsset = null;
        [SerializeField] private TMP_Text _creditsText = null;

        private void Start()
        {
            _creditsText.text = _creditsTextAsset.text;
        }
    }
}