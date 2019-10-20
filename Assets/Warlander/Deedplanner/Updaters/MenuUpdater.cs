using Steamworks;
using TMPro;
using UnityEngine;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Updaters
{
    public class MenuUpdater : MonoBehaviour
    {

        [SerializeField] private TMP_Text steamConnectionText;
        
        private void OnEnable()
        {
            LayoutManager.Instance.TileSelectionMode = TileSelectionMode.Nothing;

            bool connectedToSteam = SteamManager.ConnectedToSteam;
            steamConnectionText.gameObject.SetActive(connectedToSteam);
            if (connectedToSteam)
            {
                steamConnectionText.text = "Connected to Steam as " + SteamFriends.GetPersonaName();
            }
        }
        
    }
}