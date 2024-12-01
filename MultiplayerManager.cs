using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using Steamworks;
using TMPro;

public class MultiplayerManager : MonoBehaviourSingleton<MultiplayerManager>
{
    [Header("UI Elements")]
    public GameObject loadingScreen;
    public TextMeshProUGUI playerConnectedText;

    [Header("Player Management")]
    public PlayerObjectController playerObjectController;
    public List<GameObject> multiPlayers = new List<GameObject>();

    [Header("Game State")]
    public bool isHost;
    public bool waitForPlayers;
    public int connectedPlayersAmount;
    public int loadedPlayersAmount;
    public int deadPlayers;
    public bool[] connectedPlayersArray = new bool[100];

    private void Start()
    {
        deadPlayers = 0;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnApplicationQuit()
    {
        // Clean up networking and lobby
        var networkManager = (CustomNetworkManager)NetworkManager.singleton;
        networkManager.ShutdownClient();

        var currentLobbyId = SteamLobby.Instance.lobbyController.currentLobbyId;
        SteamMatchmaking.SetLobbyType(new CSteamID(currentLobbyId), ELobbyType.k_ELobbyTypePrivate);
        SteamMatchmaking.LeaveLobby(new CSteamID(currentLobbyId));
    }

    private void Update()
    {
        if (waitForPlayers)
        {
            playerConnectedText.gameObject.SetActive(true);
            playerConnectedText.text = $"Waiting for players.";
            loadingScreen.SetActive(true);
        }
    }

    // Handle scene loading
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameplayScene")
        {
            if (!isHost)
                waitForPlayers = true;

            StartCoroutine(WaitForClientReady());
        }
    }

    private IEnumerator WaitForClientReady()
    {
        while (!NetworkClient.ready)
        {
            yield return new WaitForSeconds(0.15f);
        }
        playerObjectController.CmdSyncLoadInfo(playerObjectController.ConnectionID);
    }

    // Start game scene logic
    public void StartScene()
    {
        Debug.Log("Starting multiplayer scene...");
        StartCoroutine(WaitForPlayersToBeReady());
    }

    private IEnumerator WaitForPlayersToBeReady()
    {
        waitForPlayers = true;

        // Wait for local player readiness
        while (!playerObjectController.connectionToClient.isReady)
        {
            yield return new WaitForSeconds(0.15f);
        }

        // Wait for all players to connect
        while (!AreAllPlayersConnected())
        {
            Debug.Log($"Waiting for players... {loadedPlayersAmount}/{connectedPlayersAmount}");
            yield return new WaitForSeconds(0.15f);
        }

        Debug.Log("All players connected. Initializing gameplay...");
        waitForPlayers = false;

        playerObjectController.CmdGameplayInit();
    }

    private bool AreAllPlayersConnected()
    {
        loadedPlayersAmount = 0;
        foreach (var connected in connectedPlayersArray)
        {
            if (connected)
            {
                loadedPlayersAmount++;
            }
        }
        return loadedPlayersAmount == connectedPlayersAmount;
    }

    // Clean destruction of networked objects
    public void DestroyClean(GameObject target)
    {
        if (target.GetComponent<NetworkIdentity>() != null)
        {
            playerObjectController.CmdDestroyClean(target);
        }
        else
        {
            Destroy(target);
        }
    }

    public void CheckEndGame()
    {
        if (isHost && deadPlayers == connectedPlayersAmount - 1)
        {
            playerObjectController.CmdEndGame(SteamLobby.Instance.CurrentLobbyID);
        }
    }

    public void EndGame()
    {
        playerObjectController.LoadSceneAsync();
    }
}
