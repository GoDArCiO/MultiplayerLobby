using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using Steamworks;

public class CustomNetworkManager : NetworkManager
{
    [Header("Player Settings")]
    [SerializeField] private PlayerObjectController gamePlayerPrefab;
    public List<PlayerObjectController> GamePlayers = new List<PlayerObjectController>();

    [Header("Prefabs")]
    [SerializeField] private List<GameObject> spawnableGameObjects;

    [Header("Game Settings")]
    public string lobbyScene;
    public string sceneToLoad;


    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (SceneManager.GetActiveScene().name == lobbyScene)
        {
            AddPlayerToLobby(conn);
        }
    }

    /// <summary>
    /// Adds a player to the lobby, initializing their unique data.
    /// </summary>
    private void AddPlayerToLobby(NetworkConnectionToClient conn)
    {
        var newPlayer = Instantiate(gamePlayerPrefab);
        newPlayer.ConnectionID = conn.connectionId;
        newPlayer.PlayerIdNumber = GamePlayers.Count + 1;
        newPlayer.PlayerSteamID = (ulong)SteamMatchmaking.GetLobbyMemberByIndex(
            (CSteamID)SteamLobby.Instance.CurrentLobbyID,
            GamePlayers.Count
        );

        NetworkServer.AddPlayerForConnection(conn, newPlayer.gameObject);

        MultiplayerManager.Instance.connectedPlayers = numPlayers;
    }

    public override void Start()
    {
        base.Start();
        InitializeSpawnPrefabs();
    }

    private void InitializeSpawnPrefabs()
    {
        foreach (var prefab in spawnableGameObjects)
        {
            spawnPrefabs.Add(prefab);
        }
    }

    public void StartGame(string sceneName)
    {
        sceneToLoad = sceneName;
        Invoke(nameof(LoadGameScene), 0.5f);
    }

    private void LoadGameScene()
    {
        ServerChangeScene(sceneToLoad);
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);

        if (sceneName != lobbyScene)
        {
            MultiplayerManager.Instance.SceneStart();
        }
    }

    public void ShutdownClient()
    {
        NetworkClient.Shutdown();
        NetworkServer.Shutdown();
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
        // Additional logic for player disconnection (if needed)
    }
}
