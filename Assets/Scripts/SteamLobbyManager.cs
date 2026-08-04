using UnityEngine;
using UnityEngine.UI; // <-- BU SATIRI EKLEYİN
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
    [SerializeField] private GameObject playerItemPrefab; // Oyuncu adının yazacağı metin prefab'ı
    [SerializeField] private GameObject startGameButton; // Sadece Kurucu görür

    [Header("Ready Button UI")]
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI readyButtonText;

    // Steam Callbacks (Olaylar)
    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;

    private CSteamID currentLobbyID;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        // Oyun başlar başlamaz panelleri garantiye alalım
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);

        if (!SteamManager.Initialized) return;

        // Steam olayları...
        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    // 1. "Arkadaşlarınla Oyna" butonuna basılınca çalışacak
    public void HostLobby()
    {
        Debug.Log("[SteamLobby] Arkadaşlarınla Oyna butonuna tıklandı!");

        // 1. Arayüzün çalıştığını teyit etmek için panelleri doğrudan değiştiriyoruz
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);

        // 2. Steam bağlıysa lobi oluşturma isteği atıyoruz
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
        Debug.Log($"[SteamLobby] Lobi oluşturma sonucu: {callback.m_eResult}");

        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError($"[SteamLobby] Lobi oluşturulamadı! Hata Kodu: {callback.m_eResult}");
            return;
        }

        currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        SteamMatchmaking.SetLobbyData(currentLobbyID, "HostAddress", SteamUser.GetSteamID().ToString());
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        // Steam davetine tıklayınca çalışır
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);

        // Kendi hazır durumumuzu Steam tarafında varsayılan olarak "false" yapıyoruz
        if (SteamManager.Initialized)
        {
            SteamMatchmaking.SetLobbyMemberData(currentLobbyID, "ReadyStatus", "false");
        }

        // Buton görünümünü sıfırla ("HAZIR OL" yeşil buton yap)
        UpdateReadyButtonUI(false);

        // Panelleri ayarla ve oyuncu listesini güncelle
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);

        UpdatePlayerList();
    }

    public void ToggleReady()
    {
        if (!SteamManager.Initialized || !currentLobbyID.IsValid()) return;

        CSteamID myID = SteamUser.GetSteamID();
        
        string currentReadyStatus = SteamMatchmaking.GetLobbyMemberData(currentLobbyID, myID, "ReadyStatus");
        string newStatus = (currentReadyStatus == "true") ? "false" : "true";

        SteamMatchmaking.SetLobbyMemberData(currentLobbyID, "ReadyStatus", newStatus);

        // Ekranı ve butonun durumunu yenile
        UpdateReadyButtonUI(newStatus == "true");
        UpdatePlayerList();
    }

    private void UpdateReadyButtonUI(bool isReady)
    {
        if (readyButtonText == null) return;

        if (isReady)
        {
            readyButtonText.text = "HAZIRI BOZ";
            // Butonun rengini turuncu/kırmızı yapmak istersen (Opsiyonel):
            if (readyButton != null) 
                readyButton.GetComponent<Image>().color = new Color(0.9f, 0.3f, 0.2f); 
        }
        else
        {
            readyButtonText.text = "HAZIR OL";
            if (readyButton != null) 
                readyButton.GetComponent<Image>().color = new Color(0.2f, 0.8f, 0.3f); // Yeşil
        }
    }

    public void UpdatePlayerList()
    {
        // 1. Önce mevcut tüm slotları temizle
        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        int numPlayers = SteamMatchmaking.GetNumLobbyMembers(currentLobbyID);
        CSteamID hostID = SteamMatchmaking.GetLobbyOwner(currentLobbyID);

        // 2. Her zaman 4 slot oluştur (Dolu ve Boşlar için)
        for (int i = 0; i < 4; i++)
        {
            GameObject slotItem = Instantiate(playerItemPrefab, playerListContent);
            LobbySlotUI slotUI = slotItem.GetComponent<LobbySlotUI>();

            if (i < numPlayers)
            {
                // DOLU SLOT
                CSteamID memberID = SteamMatchmaking.GetLobbyMemberByIndex(currentLobbyID, i);
                string memberName = SteamFriends.GetFriendPersonaName(memberID);
                bool isHost = (memberID == hostID);

                // Steam'den oyuncunun hazır durumunu çekelim (Varsayılan: false)
                string readyData = SteamMatchmaking.GetLobbyMemberData(currentLobbyID, memberID, "ReadyStatus");
                bool isReady = (readyData == "true");

                // 3 parametre ile çağırıyoruz: (İsim, Host mu, Hazır mı)
                slotUI.SetPlayer(memberID, memberName, isHost, isReady);
            }
            else
            {
                // BOŞ SLOT
                slotUI.SetEmpty();
            }
        }
    }

    public void LeaveLobby()
    {
        Debug.Log("[SteamLobby] Lobiden ayrılıyor...");

        // 1. Steam tarafında lobiden çıkış yap
        if (SteamManager.Initialized && currentLobbyID.IsValid())
        {
            SteamMatchmaking.LeaveLobby(currentLobbyID);
        }

        // 2. Panelleri sıfırla (Lobi Kapanır, Ana Menü Açılır)
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }
}