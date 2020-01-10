using TMPro;
using UnityEngine;

namespace Warlander.Deedplanner.Gui
{
    public class CreditsWindow : MonoBehaviour
    {
        [SerializeField] private TextAsset creditsTextAsset = null;
        [SerializeField] private TMP_Text creditsText = null;

        private void Start()
        {
            creditsText.text = creditsTextAsset.text;
        }
    }
}