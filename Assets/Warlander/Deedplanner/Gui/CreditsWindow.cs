using TMPro;
using UnityEngine;

namespace Warlander.Deedplanner.Gui
{
    public class CreditsWindow : MonoBehaviour
    {
        [SerializeField] private TextAsset creditsTextAsset;
        [SerializeField] private TMP_Text creditsText;

        private void Start()
        {
            creditsText.text = creditsTextAsset.text;
        }
    }
}