using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;
using UnityEngine.SceneManagement;

public class SteamLobbyManager : MonoBehaviour
{
    public static SteamLobbyManager Instance;

    [Header("UI Panelleri")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject lobbyPanel;

    [Header("Lobi UI Elemanları")]
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerItemPrefab; 
    [SerializeField] private GameObject startGameButton; // Sadece Kurucu görür

    [Header("Ready Button UI")]
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI readyButtonText;

    [Header("Sahne Ayarları")]
    [SerializeField] private string gameSceneName = "GameScene"; // Yüklenecek oyun sahnesinin adı

    // Steam Callbacks (Olaylar)
    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;
    protected Callback<LobbyDataUpdate_t> lobbyDataUpdated; // Oyuncular hazır olduğunda listenin güncellenmesi için

    private CSteamID currentLobbyID;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);

        if (!SteamManager.Initialized) return;

        // Steam Olay Dinleyicileri
        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        lobbyDataUpdated = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdated);
    }

    public void HostLobby()
    {
        Debug.Log("[SteamLobby] Arkadaşlarınla Oyna butonuna tıklandı!");

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);

        if (SteamManager.Initialized)
        {
            Debug.Log("[SteamLobby] Steam aktif, lobi oluşturuluyor...");
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 4);
        }
        else
        {
            Debug.LogWarning("[SteamLobby] Steam bağlı değil ancak UI paneli test için açıldı.");
        }
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError($"[SteamLobby] Lobi oluşturulamadı! Hata Kodu: {callback.m_eResult}");
            return;
        }

        currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        SteamMatchmaking.SetLobbyData(currentLobbyID, "HostAddress", SteamUser.GetSteamID().ToString());

        LobbyChatManager chatManager = FindObjectOfType<LobbyChatManager>();
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

        if (SteamManager.Initialized)
        {
            SteamMatchmaking.SetLobbyMemberData(currentLobbyID, "ReadyStatus", "false");
        }

        UpdateReadyButtonUI(false);

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);

        // Kurucu kontrolü: Başlat butonunu sadece Kurucuya göster
        CheckHostStatus();
        UpdatePlayerList();

        LobbyChatManager chatManager = FindObjectOfType<LobbyChatManager>();
        if (chatManager != null)
        {
            chatManager.InitializeChat((CSteamID)callback.m_ulSteamIDLobby);
        }
    }

    // Bir oyuncu Hazır butonuna bastığında veya lobi verisi değiştiğinde çalışır
    private void OnLobbyDataUpdated(LobbyDataUpdate_t callback)
    {
        UpdatePlayerList();
    }

    // --- YENİ EKLEYECEĞİN BUTON METODLARI ---

    // 1. "ARKADAŞ DAVET ET" Butonuna Bağlanacak Metod
    public void OpenInviteOverlay()
    {
        if (SteamManager.Initialized && currentLobbyID.IsValid())
        {
            // Steam Overlay Arkadaş Davet Penceresini Açar
            SteamFriends.ActivateGameOverlayInviteDialog(currentLobbyID);
        }
        else
        {
            Debug.LogWarning("Lobi geçerli değil veya Steam açık değil!");
        }
    }

    // 2. "OYUNU BAŞLAT" Butonuna Bağlanacak Metod (Sadece Kurucu İçin)
    public void StartGame()
    {
        if (!SteamManager.Initialized || !currentLobbyID.IsValid()) return;

        // Kurucu kontrolü
        if (SteamMatchmaking.GetLobbyOwner(currentLobbyID) == SteamUser.GetSteamID())
        {
            Debug.Log("[SteamLobby] Oyunu Başlatılıyor... Oyun Sahnesi Yükleniyor.");
            SceneManager.LoadScene(gameSceneName);
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
            if (readyButton != null) 
                readyButton.GetComponent<Image>().color = new Color(0.9f, 0.3f, 0.2f); 
        }
        else
        {
            readyButtonText.text = "HAZIR OL";
            if (readyButton != null) 
                readyButton.GetComponent<Image>().color = new Color(0.2f, 0.8f, 0.3f); 
        }
    }

    private void CheckHostStatus()
    {
        if (!SteamManager.Initialized || !currentLobbyID.IsValid() || startGameButton == null) return;

        bool isHost = SteamMatchmaking.GetLobbyOwner(currentLobbyID) == SteamUser.GetSteamID();
        startGameButton.SetActive(isHost); // Başlat butonunu sadece Kurucuya açar
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
        Debug.Log("[SteamLobby] Lobiden ayrılıyor...");

        if (SteamManager.Initialized && currentLobbyID.IsValid())
        {
            SteamMatchmaking.LeaveLobby(currentLobbyID);
        }

        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }
}