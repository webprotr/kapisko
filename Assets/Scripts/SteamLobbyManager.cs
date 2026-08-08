using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;
using UnityEngine.SceneManagement;
using Mirror;

public class SteamLobbyManager : MonoBehaviour
{
    public static SteamLobbyManager Instance;

    [Header("UI Panelleri")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject lobbyPanel;

    [Header("Lobi UI Elemanları")]
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerItemPrefab; 
    [SerializeField] private GameObject startGameButton; 

    [Header("Ready Button UI")]
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI readyButtonText;

    [Header("Sahne Ayarları")]
    [SerializeField] private string gameSceneName = "01_GameScene"; 

    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;
    protected Callback<LobbyDataUpdate_t> lobbyDataUpdated; 

    private CSteamID currentLobbyID;
    private bool isGameStarting = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);

        if (!SteamManager.Initialized) return;

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        lobbyDataUpdated = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdated);
    }

    public void HostLobby()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);

        if (SteamManager.Initialized)
        {
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 4);
        }
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK) return;

        currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        SteamMatchmaking.SetLobbyData(currentLobbyID, "HostAddress", SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(currentLobbyID, "GameStarted", "false");

        LobbyChatManager chatManager = FindFirstObjectByType<LobbyChatManager>();
        if (chatManager != null)
        {
            chatManager.InitializeChat((CSteamID)callback.m_ulSteamIDLobby);
        }
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        isGameStarting = false;

        if (SteamManager.Initialized)
        {
            SteamMatchmaking.SetLobbyMemberData(currentLobbyID, "ReadyStatus", "false");
        }

        UpdateReadyButtonUI(false);

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);

        CheckHostStatus();
        UpdatePlayerList();

        LobbyChatManager chatManager = FindFirstObjectByType<LobbyChatManager>();
        if (chatManager != null)
        {
            chatManager.InitializeChat((CSteamID)callback.m_ulSteamIDLobby);
        }
    }

    private void OnLobbyDataUpdated(LobbyDataUpdate_t callback)
    {
        // Lobi listesini güncelle
        UpdatePlayerList();

        // SADECE KATILAN OYUNCULAR (CLIENT) İÇİN KONTROL
        if (!currentLobbyID.IsValid()) return;

        CSteamID hostID = SteamMatchmaking.GetLobbyOwner(currentLobbyID);
        
        // Eğer biz Kurucu (Host) değilsek ve oyun başlatıldıysa
        if (hostID != SteamUser.GetSteamID())
        {
            string gameStarted = SteamMatchmaking.GetLobbyData(currentLobbyID, "GameStarted");

            if (gameStarted == "true" && !isGameStarting)
            {
                isGameStarting = true;
                string hostAddress = SteamMatchmaking.GetLobbyData(currentLobbyID, "HostAddress");

                Debug.Log($"[SteamLobby] Kurucu oyunu başlattı! Host Adresi: {hostAddress}. Client olarak bağlanılıyor...");

                if (NetworkManager.singleton != null)
                {
                    NetworkManager.singleton.networkAddress = hostAddress;
                    NetworkManager.singleton.StartClient();
                }
            }
        }
    }

    public void OpenInviteOverlay()
    {
        if (SteamManager.Initialized && currentLobbyID.IsValid())
        {
            SteamFriends.ActivateGameOverlayInviteDialog(currentLobbyID);
        }
    }

    public void StartGame()
    {
        if (!SteamManager.Initialized || !currentLobbyID.IsValid()) return;

        if (SteamMatchmaking.GetLobbyOwner(currentLobbyID) == SteamUser.GetSteamID())
        {
            Debug.Log("[SteamLobby] Kurucu oyunu başlatıyor...");

            // 1. Önce Host IP/SteamID adresini lobiye yaz
            SteamMatchmaking.SetLobbyData(currentLobbyID, "HostAddress", SteamUser.GetSteamID().ToString());
            
            // 2. Oyun başladı bayrağını set et (Bu bilgi katılan istemcilere iletilecek)
            SteamMatchmaking.SetLobbyData(currentLobbyID, "GameStarted", "true");

            isGameStarting = true;

            if (NetworkManager.singleton != null)
            {
                NetworkManager.singleton.onlineScene = gameSceneName;

                // Eğer ağ sunucusu aktif değilse önce Host'u başlat
                if (!NetworkServer.active)
                {
                    NetworkManager.singleton.StartHost();
                }
                else
                {
                    NetworkManager.singleton.ServerChangeScene(gameSceneName);
                }
            }
        }
    }

    public void ToggleReady()
    {
        if (!SteamManager.Initialized || !currentLobbyID.IsValid()) return;

        CSteamID myID = SteamUser.GetSteamID();
        string currentReadyStatus = SteamMatchmaking.GetLobbyMemberData(currentLobbyID, myID, "ReadyStatus");
        string newStatus = (currentReadyStatus == "true") ? "false" : "true";

        SteamMatchmaking.SetLobbyMemberData(currentLobbyID, "ReadyStatus", newStatus);

        UpdateReadyButtonUI(newStatus == "true");
        UpdatePlayerList();
    }

    private void UpdateReadyButtonUI(bool isReady)
    {
        if (readyButtonText == null) return;

        if (isReady)
        {
            readyButtonText.text = "HAZIRI BOZ";
            if (readyButton != null) readyButton.GetComponent<Image>().color = new Color(0.9f, 0.3f, 0.2f); 
        }
        else
        {
            readyButtonText.text = "HAZIR OL";
            if (readyButton != null) readyButton.GetComponent<Image>().color = new Color(0.2f, 0.8f, 0.3f); 
        }
    }

    private void CheckHostStatus()
    {
        if (!SteamManager.Initialized || !currentLobbyID.IsValid() || startGameButton == null) return;

        bool isHost = SteamMatchmaking.GetLobbyOwner(currentLobbyID) == SteamUser.GetSteamID();
        startGameButton.SetActive(isHost); 
    }

    public void UpdatePlayerList()
    {
        if (playerListContent == null) return;

        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        int numPlayers = SteamMatchmaking.GetNumLobbyMembers(currentLobbyID);
        CSteamID hostID = SteamMatchmaking.GetLobbyOwner(currentLobbyID);

        for (int i = 0; i < 4; i++)
        {
            GameObject slotItem = Instantiate(playerItemPrefab, playerListContent);
            LobbySlotUI slotUI = slotItem.GetComponent<LobbySlotUI>();

            if (i < numPlayers)
            {
                CSteamID memberID = SteamMatchmaking.GetLobbyMemberByIndex(currentLobbyID, i);
                string memberName = SteamFriends.GetFriendPersonaName(memberID);
                bool isHost = (memberID == hostID);

                string readyData = SteamMatchmaking.GetLobbyMemberData(currentLobbyID, memberID, "ReadyStatus");
                bool isReady = (readyData == "true");

                slotUI.SetPlayer(memberID, memberName, isHost, isReady);
            }
            else
            {
                slotUI.SetEmpty();
            }
        }

        CheckHostStatus();
    }

    public void LeaveLobby()
    {
        if (SteamManager.Initialized && currentLobbyID.IsValid())
        {
            SteamMatchmaking.LeaveLobby(currentLobbyID);
        }

        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }
}