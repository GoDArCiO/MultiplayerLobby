using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;

public class LobbyController : MonoBehaviour
{
    [Header("Lobby Data")]
    public ulong currentLobbyId;
    public bool isPublicLobby = true;

    [Header("UI Elements")]
    public TMP_Text lobbyNameText;
    public Button startGameButton;
    public Button publicToggleButton;
    public GameObject readyIndicator;
    public GameObject publicIndicator;

    [Header("Local Player Data")]
    public GameObject localPlayerObject;
    public PlayerObjectController localPlayerController;

    [Header("Lobby Player Data")]
    public GameObject lobbyCharacterPrefab;
    private readonly List<LobbyCharacterInstance> lobbyCharacterInstances = new List<LobbyCharacterInstance>();

    public List<Transform> lobbyCharacterPositions;

    [Header("Canvases")]
    public GameObject startMenuCanvas;
    public GameObject lobbyCanvas;

    // Network Manager
    private CustomNetworkManager manager;
    private CustomNetworkManager Manager => manager ??= CustomNetworkManager.singleton as CustomNetworkManager;

    /// <summary>
    /// Toggles the ready status for the local player.
    /// </summary>
    public void ToggleReadyStatus()
    {
        localPlayerController?.ChangeReady();
    }

    /// <summary>
    /// Updates the ready indicator for the local player.
    /// </summary>
    public void UpdateReadyIndicator()
    {
        readyIndicator.SetActive(localPlayerController?.Ready ?? false);
    }

    /// <summary>
    /// Checks if all players are ready, enabling the Start Game button if conditions are met.
    /// </summary>
    public void CheckIfAllPlayersReady()
    {
        bool allPlayersReady = Manager.GamePlayers.All(player => player.Ready);
        startGameButton.interactable = allPlayersReady && localPlayerController?.PlayerIdNumber == 1;
    }

    /// <summary>
    /// Updates the lobby name displayed in the UI.
    /// </summary>
    public void UpdateLobbyName()
    {
        currentLobbyId = Manager.GetComponent<SteamLobby>().CurrentLobbyID;
        lobbyNameText.text = SteamMatchmaking.GetLobbyData(new CSteamID(currentLobbyId), "name");
    }

    /// <summary>
    /// Assigns the local player's game object and updates UI accordingly.
    /// </summary>
    public void AssignLocalPlayer(GameObject playerObject)
    {
        localPlayerObject = playerObject;
        localPlayerController = playerObject.GetComponent<PlayerObjectController>();

        bool isHost = localPlayerController?.PlayerIdNumber == 1;
        publicToggleButton.interactable = isHost;
    }

    /// <summary>
    /// Updates the player list UI to match the current player data.
    /// </summary>
    public void UpdatePlayerList()
    {
        if (lobbyCharacterInstances.Count < Manager.GamePlayers.Count)
            CreatePlayerItem();
        else if (lobbyCharacterInstances.Count > Manager.GamePlayers.Count)
            RemoveExtraPlayerItems();
        else
            UpdatePlayerItems();
    }


    private void CreatePlayerItem()
    {
        foreach (var player in Manager.GamePlayers)
        {
            if (!lobbyCharacterInstances.Any(item => item.ConnectionID == player.ConnectionID))
            {
                AddPlayer(player);
            }
        }
    }

    private void AddPlayer(PlayerObjectController player)
    {
        GameObject lobbyCharacterObject = Instantiate(lobbyCharacterPrefab);
        LobbyCharacterInstance lobbyCharacterInstance = lobbyCharacterObject.GetComponent<LobbyCharacterInstance>();

        foreach (Transform position in lobbyCharacterPositions)
        {
            if (position.childCount == 0)
            {
                lobbyCharacterObject.transform.SetParent(position);
                lobbyCharacterObject.transform.localPosition = Vector3.zero;
                lobbyCharacterObject.transform.localRotation = Quaternion.identity;
                lobbyCharacterObject.transform.localScale = Vector3.one;
                break;
            }
        }

        lobbyCharacterInstance.SetPlayerData(player.PlayerName, player.ConnectionID, player.PlayerSteamID, player.Ready);
        lobbyCharacterInstances.Add(lobbyCharacterInstance);
    }

    private void UpdatePlayerItems()
    {
        foreach (var player in Manager.GamePlayers)
        {
            foreach (var playerItem in lobbyCharacterInstances)
            {
                if (playerItem.ConnectionID == player.ConnectionID)
                {
                    playerItem.SetPlayerData(player.PlayerName, player.ConnectionID, player.PlayerSteamID, player.Ready);
                }
            }
        }
        UpdateReadyIndicator();
        CheckIfAllPlayersReady();
    }

    private void RemoveExtraPlayerItems()
    {
        var itemsToRemove = lobbyCharacterInstances
            .Where(item => Manager.GamePlayers.All(player => player.ConnectionID != item.ConnectionID))
            .ToList();

        foreach (var item in itemsToRemove)
        {
            lobbyCharacterInstances.Remove(item);
            Destroy(item.gameObject);
        }
    }

    // Lobby Management
    public void StartGame(string sceneName)
    {
        SteamMatchmaking.SetLobbyType(new CSteamID(currentLobbyId), ELobbyType.k_ELobbyTypePrivate);
        localPlayerController?.CanStartGame(sceneName);
    }

    public void ToggleLobbyPrivacy()
    {
        isPublicLobby = !isPublicLobby;
        var newLobbyType = isPublicLobby ? ELobbyType.k_ELobbyTypePublic : ELobbyType.k_ELobbyTypeFriendsOnly;
        SteamMatchmaking.SetLobbyType(new CSteamID(currentLobbyId), newLobbyType);

        UpdatePrivacyIndicators(isPublicLobby);
        localPlayerController.CmdToggleLobbyPrivacy(isPublicLobby);
    }

    public void UpdatePrivacyIndicators(bool isPublic)
    {
        publicIndicator.SetActive(isPublic);
    }

    public void LeaveLobby()
    {
        Manager.ShutdownClient();
        SwitchToLobbyUi(false);
    }

    public void SwitchToLobbyUi(bool isActive)
    {
        lobbyCanvas.SetActive(isActive);
        startMenuCanvas.SetActive(!isActive);
    }
}
