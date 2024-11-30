using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;

public class SteamLobby : MonoBehaviourSingleton<SteamLobby>
{
    [Header("References")]
    public LobbiesUiActions lobbiesListManager;
    public AfterGameLobbyJoiner afterGameLobbyJoiner;
    public LobbyController lobbyController;

    [Header("Steamworks Settings")]
    private const string HostAddressKey = "HostAddress";
    private Callback<LobbyCreated_t> lobbyCreatedCallback;
    private Callback<GameLobbyJoinRequested_t> joinRequestCallback;
    private Callback<LobbyEnter_t> lobbyEnteredCallback;
    private Callback<LobbyDataUpdate_t> lobbyDataUpdatedCallback;
    private Callback<LobbyMatchList_t> lobbyListCallback;

    public List<CSteamID> LobbyIDs { get; private set; } = new List<CSteamID>();
    public ulong CurrentLobbyID { get; private set; }

    private CustomNetworkManager networkManager;

    private void Start()
    {
        if (!SteamManager.Initialized) return;

        networkManager = GetComponent<CustomNetworkManager>();

        RegisterSteamCallbacks();
    }

    private void RegisterSteamCallbacks()
    {
        lobbyCreatedCallback = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        joinRequestCallback = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequest);
        lobbyEnteredCallback = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        lobbyListCallback = Callback<LobbyMatchList_t>.Create(OnGetLobbyList);
        lobbyDataUpdatedCallback = Callback<LobbyDataUpdate_t>.Create(OnGetLobbyData);
    }

    public void HostLobby()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, networkManager.maxConnections);
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK) return;

        var steamLobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        CurrentLobbyID = callback.m_ulSteamIDLobby;

        networkManager.StartHost();

        SteamMatchmaking.SetLobbyData(steamLobbyID, HostAddressKey, SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(steamLobbyID, "name", $"{SteamFriends.GetPersonaName()}'s Lobby");
    }

    private void OnJoinRequest(GameLobbyJoinRequested_t callback)
    {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        if (!NetworkServer.active)
        {
            networkManager.networkAddress = SteamMatchmaking.GetLobbyData(
                new CSteamID(callback.m_ulSteamIDLobby),
                HostAddressKey
            );
            networkManager.StartClient();
        }

        afterGameLobbyJoiner.CurrentLobbyID = callback.m_ulSteamIDLobby;
        lobbyController.SwitchToLobbyUi(true);
    }

    public void JoinLobby(CSteamID lobbyId)
    {
        SteamMatchmaking.JoinLobby(lobbyId);
    }

    public void GetLobbiesList()
    {
        LobbyIDs.Clear();
        SteamMatchmaking.AddRequestLobbyListResultCountFilter(60);
        SteamMatchmaking.RequestLobbyList();
    }

    private void OnGetLobbyList(LobbyMatchList_t result)
    {
        lobbiesListManager.ClearLobbiesList();

        for (int i = 0; i < result.m_nLobbiesMatching; i++)
        {
            var lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
            LobbyIDs.Add(lobbyID);
            SteamMatchmaking.RequestLobbyData(lobbyID);
        }
    }

    private void OnGetLobbyData(LobbyDataUpdate_t result)
    {
        lobbiesListManager.DisplayLobbies(LobbyIDs, result);
    }
}
