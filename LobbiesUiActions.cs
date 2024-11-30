using System.Collections.Generic;
using UnityEngine;
using Steamworks;

public class LobbiesUiActions : MonoBehaviour
{
    [Header("Lobbies List UI")]
    [SerializeField] private GameObject lobbyDataItemPrefab; // Prefab for displaying lobby data
    [SerializeField] private GameObject lobbyListContent;    // Parent object for lobby UI elements

    private readonly List<GameObject> activeLobbyItems = new List<GameObject>();

    public void HostNewLobby()
    {
        SteamLobby.Instance.HostLobby();
    }

    public void RefreshLobbiesList()
    {
        ClearLobbiesList(); // Remove any existing lobby entries
        SteamLobby.Instance.GetLobbiesList(); // Trigger Steam lobby retrieval
    }

    /// <summary>
    /// Displays the list of lobbies in the UI.
    /// </summary>
    /// <param name="lobbyIDs">List of lobby IDs retrieved from Steam.</param>
    /// <param name="result">Lobby data update result from Steamworks.</param>
    public void DisplayLobbies(List<CSteamID> lobbyIDs, LobbyDataUpdate_t result)
    {
        ClearLobbiesList(); // Ensure no duplicates

        foreach (var lobbyID in lobbyIDs)
        {
            if (lobbyID.m_SteamID == result.m_ulSteamIDLobby)
            {
                GameObject lobbyItem = Instantiate(lobbyDataItemPrefab, lobbyListContent.transform);
                LobbyDataEntry lobbyData = lobbyItem.GetComponent<LobbyDataEntry>();

                if (lobbyData != null)
                {
                    lobbyData.SetLobbyData(lobbyID, SteamMatchmaking.GetLobbyData(lobbyID, "name"));
                }
                else
                {
                    Debug.LogError("LobbyDataEntry component missing from lobbyDataItemPrefab.");
                }

                lobbyItem.transform.localScale = Vector3.one;
                activeLobbyItems.Add(lobbyItem);
            }
        }
    }

    public void ClearLobbiesList()
    {
        foreach (GameObject lobbyItem in activeLobbyItems)
        {
            Destroy(lobbyItem);
        }
        activeLobbyItems.Clear();
    }
}
