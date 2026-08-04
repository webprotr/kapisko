using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;

public class LobbySlotUI : MonoBehaviour
{
    [Header("Paneller & Çerçeve")]
    [SerializeField] private GameObject filledSlotPanel;       // Oyuncu doluysa görünecek panel
    [SerializeField] private GameObject emptySlotPanel;        // "BOŞ SLOT" paneli
    [SerializeField] private Image slotOuterBorder;           // En dıştaki kart çerçevesi (Border Image)

    [Header("Dolu Slot Elemanları")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private GameObject hostIcon;            // Sol üstteki taç ikonu 👑
    [SerializeField] private GameObject leaderBadge;          // İsim altındaki "LİDER" rozeti
    [SerializeField] private GameObject readyStatusHeader;    // Kartın en üstündeki yeşil "HAZIR" şeridi
    [SerializeField] private Image avatarOutline;             // Dairesel profil resmi çerçevesi
    [SerializeField] private TextMeshProUGUI readyStatusText; // Kartın en altındaki "✓ HAZIR" metni

    [Header("Steam Avatar")]
    [SerializeField] private RawImage avatarRawImage;

    [Header("Renk Paleti")]
    [SerializeField] private Color borderEmptyColor = new Color(0.1f, 0.3f, 0.7f);   // Mavi (Boş Slot)
    [SerializeField] private Color borderReadyColor = new Color(0.2f, 0.9f, 0.2f);   // Neon Yeşil (Hazır)
    [SerializeField] private Color borderNotReadyColor = new Color(0.4f, 0.4f, 0.4f); // Gri (Hazır Değil)

    // SteamLobbyManager tek parametre çağırdığında geriye dönük uyumluluk için
    public void SetPlayerName(string playerName)
    {
        SetPlayer(CSteamID.Nil, playerName, false, false);
    }

    // Slot bir oyuncuyla dolduğunda çalışan ana metod
    public void SetPlayer(CSteamID steamID, string playerName, bool isHost, bool isReady)
    {
        // 1. Panellerin durumunu ayarla
        if (filledSlotPanel != null) filledSlotPanel.SetActive(true);
        if (emptySlotPanel != null) emptySlotPanel.SetActive(false);

        // 2. Oyuncu İsmi ve Liderlik/Taç Görselleri
        if (playerNameText != null) playerNameText.text = playerName;
        if (hostIcon != null) hostIcon.SetActive(isHost);
        if (leaderBadge != null) leaderBadge.SetActive(isHost);

        // 3. Üst Şerit ve Alt Metin
        if (readyStatusHeader != null) readyStatusHeader.SetActive(isReady);
        if (readyStatusText != null)
        {
            readyStatusText.gameObject.SetActive(true);
            readyStatusText.text = isReady ? "✓ HAZIR" : "BEKLENİYOR...";
        }

        // 4. Çerçeve ve Daire Renk Güncellemeleri
        Color targetColor = isReady ? borderReadyColor : borderNotReadyColor;

        if (slotOuterBorder != null) slotOuterBorder.color = targetColor;
        if (avatarOutline != null) avatarOutline.color = targetColor;

        // 5. High-Res Steam Profil Resmini Yükle
        LoadSteamAvatar(steamID);
    }

    // Slot boş kaldığında çalışacak metod
    public void SetEmpty()
    {
        if (filledSlotPanel != null) filledSlotPanel.SetActive(false);
        if (emptySlotPanel != null) emptySlotPanel.SetActive(true);

        if (playerNameText != null) playerNameText.text = "Boş Slot";
        if (hostIcon != null) hostIcon.SetActive(false);
        if (leaderBadge != null) leaderBadge.SetActive(false);
        if (readyStatusHeader != null) readyStatusHeader.SetActive(false);
        if (readyStatusText != null) readyStatusText.gameObject.SetActive(false);

        // Boş Slot Dış Çerçevesini MAVİ yap
        if (slotOuterBorder != null) slotOuterBorder.color = borderEmptyColor;
    }

    // Steam'den Profil Resmi (Avatar) İndiren Metod
    private void LoadSteamAvatar(CSteamID steamID)
    {
        if (avatarRawImage == null || !SteamManager.Initialized) return;
        if (steamID == CSteamID.Nil) return;

        // Büyük avatar (128x128)
        int imageID = SteamFriends.GetLargeFriendAvatar(steamID);
        if (imageID == -1) return;

        uint width, height;
        if (SteamUtils.GetImageSize(imageID, out width, out height))
        {
            byte[] imageBytes = new byte[width * height * 4];
            if (SteamUtils.GetImageRGBA(imageID, imageBytes, (int)(width * height * 4)))
            {
                // Texture2D oluşturma ve yükleme
                Texture2D avatarTexture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
                avatarTexture.LoadRawTextureData(imageBytes);
                avatarTexture.Apply();

                // RawImage'e atama
                avatarRawImage.texture = avatarTexture;

                // Ters görüntüyü düzeltme
                avatarRawImage.uvRect = new Rect(0f, 1f, 1f, -1f);

                // Yuvarlak Maske İçinde Tam Kaplama (Stretch / Cover)
                RectTransform rectTransform = avatarRawImage.rectTransform;
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
            }
        }
    }

    // Boş slotlardaki "+ DAVET ET" butonuna tıklanınca Steam arayüzünü açar
    public void OnClickInviteButton()
    {
        if (SteamManager.Initialized)
        {
            SteamFriends.ActivateGameOverlay("LobbyInvite");
        }
    }
}