using UnityEngine;
using Steamworks;
using TMPro;

public class LobbyDataEntry : MonoBehaviour
{
    public CSteamID lobbyID;
    public TMP_Text lobbyNameText;

    public void SetLobbyData(CSteamID lobbyID, string lobbyName)
    {
        this.lobbyID = lobbyID;
        if (lobbyName == "")
        {
            lobbyNameText.text = "Empty";
        }
        else
        {
            lobbyNameText.text = lobbyName;
        }
    }

    public void JoinLobby()
    {
        SteamLobby.Instance.JoinLobby(lobbyID);
    }
}
