using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks; // Steam API kütüphanesi

public class LobbySlotUI : MonoBehaviour
{
    [Header("Slot Elemanları")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private GameObject hostIcon;            // Kurucu tacı veya ikonu
    [SerializeField] private TextMeshProUGUI readyStatusText;// "HAZIR!" / "HAZIR DEĞİL" metni
    [SerializeField] private GameObject emptySlotPanel;       // "BOŞ SLOT" paneli
    [SerializeField] private GameObject filledSlotPanel;      // Oyuncu doluysa görünecek panel

    [Header("Steam Avatar")]
    [SerializeField] private RawImage avatarRawImage;

    // SteamLobbyManager'ın aradığı tek parametreli metod
    public void SetPlayerName(string playerName)
    {
        SetPlayer(CSteamID.Nil, playerName, false, false);
    }
    
    // Oyuncu bilgilerini detaylı set eden metod
    public void SetPlayer(CSteamID steamID, string playerName, bool isHost, bool isReady)
    {
        if (filledSlotPanel != null) filledSlotPanel.SetActive(true);
        if (emptySlotPanel != null) emptySlotPanel.SetActive(false);

        if (playerNameText != null) playerNameText.text = isHost ? playerName + " (Lider)" : playerName;
        if (hostIcon != null) hostIcon.SetActive(isHost);

        if (readyStatusText != null)
        {
            readyStatusText.gameObject.SetActive(true);
            readyStatusText.text = isReady ? "<color=#00FF00>HAZIR!</color>" : "<color=#FF0000>HAZIR DEĞİL</color>";
        }

        // Steam Profil Resmini Çek ve Yükle
        LoadSteamAvatar(steamID);
    }

    public void SetEmpty()
    {
        if (filledSlotPanel != null) filledSlotPanel.SetActive(false);
        if (emptySlotPanel != null) emptySlotPanel.SetActive(true);

        if (playerNameText != null) playerNameText.text = "Boş Slot";
        if (hostIcon != null) hostIcon.SetActive(false);
        if (readyStatusText != null) readyStatusText.gameObject.SetActive(false);
    }

    // Steam'den Profil Resmi (Avatar) İndiren Metod
    private void LoadSteamAvatar(CSteamID steamID)
    {
        if (avatarRawImage == null || !SteamManager.Initialized) return;

        // Geçersiz SteamID kontrolü
        if (steamID == CSteamID.Nil) return;

        // Büyük avatar çekilerek çözünürlük arttırıldı (128x128)
        int imageID = SteamFriends.GetLargeFriendAvatar(steamID);
        if (imageID == -1) return;

        uint width, height;
        if (SteamUtils.GetImageSize(imageID, out width, out height))
        {
            byte[] imageBytes = new byte[width * height * 4];
            if (SteamUtils.GetImageRGBA(imageID, imageBytes, (int)(width * height * 4)))
            {
                // 1. Texture2D'yi oluştur ve veriyi yükle (Performanslı)
                Texture2D avatarTexture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
                avatarTexture.LoadRawTextureData(imageBytes);
                avatarTexture.Apply();

                // 2. Texture2D'yi RawImage bileşenine bağlama
                avatarRawImage.texture = avatarTexture;

                // 3. Ters görüntüyü koddan düzeltme
                avatarRawImage.uvRect = new Rect(0f, 1f, 1f, -1f);

                // 4. Yuvarlak Alanı Tam Kaplama (Stretch / Cover)
                // RawImage'in RectTransform'unu ebeveynine (Yuvarlak Maske Paneline) sıfırlayarak esnetiyoruz
                RectTransform rectTransform = avatarRawImage.rectTransform;
                rectTransform.anchorMin = Vector2.zero; // (0,0) - Sol Alt
                rectTransform.anchorMax = Vector2.one;  // (1,1) - Sağ Üst
                rectTransform.offsetMin = Vector2.zero; // Left & Bottom = 0
                rectTransform.offsetMax = Vector2.zero; // Right & Top = 0
            }
        }
    }
}