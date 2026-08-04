using System.Collections; // Coroutine için gerekli
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;
using System.Text;

public class LobbyChatManager : MonoBehaviour
{
    [Header("UI Elemanları")]
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private TextMeshProUGUI chatContentText;
    [SerializeField] private ScrollRect chatScrollRect;

    [Header("Lobi Bilgileri UI")]
    [SerializeField] private TextMeshProUGUI lobbyCodeText;

    protected Callback<LobbyChatMsg_t> callbackLobbyChatMsg;
    private CSteamID currentLobbyID;

    private void OnEnable()
    {
        if (SteamManager.Initialized)
        {
            callbackLobbyChatMsg = Callback<LobbyChatMsg_t>.Create(OnLobbyChatMsg);
        }
    }

    public void InitializeChat(CSteamID lobbyID)
    {
        currentLobbyID = lobbyID;
        if (chatContentText != null) chatContentText.text = "";

        if (lobbyCodeText != null)
        {
            lobbyCodeText.text = lobbyID.ToString();
        }

        AddChatMessage("Sistem", "Lobiye katıldınız.", "#38B000");
    }

    public void OnClickSendChatMessage()
    {
        if (string.IsNullOrWhiteSpace(chatInputField.text)) return;
        if (currentLobbyID == CSteamID.Nil) return;

        string message = chatInputField.text.Trim();
        byte[] msgBytes = Encoding.UTF8.GetBytes(message);
        bool success = SteamMatchmaking.SendLobbyChatMsg(currentLobbyID, msgBytes, msgBytes.Length);

        if (success)
        {
            chatInputField.text = "";
            chatInputField.ActivateInputField(); // Yazmaya devam edebilmek için odağı koru
        }
    }

    private void OnLobbyChatMsg(LobbyChatMsg_t param)
    {
        if ((CSteamID)param.m_ulSteamIDLobby != currentLobbyID) return;

        CSteamID senderID;
        EChatEntryType chatEntryType;
        byte[] data = new byte[4096];
        int ret = SteamMatchmaking.GetLobbyChatEntry(
            (CSteamID)param.m_ulSteamIDLobby,
            (int)param.m_iChatID,
            out senderID,
            data,
            data.Length,
            out chatEntryType
        );

        if (ret > 0)
        {
            string message = Encoding.UTF8.GetString(data, 0, ret);
            string senderName = SteamFriends.GetFriendPersonaName(senderID);

            bool isMe = senderID == SteamUser.GetSteamID();
            string nameColor = isMe ? "#3A86EF" : "#FFB703";

            AddChatMessage(senderName, message, nameColor);
        }
    }

    public void AddChatMessage(string sender, string message, string colorHex = "#FFFFFF")
    {
        if (chatContentText == null) return;

        string formattedLine = $"<color={colorHex}><b>{sender}:</b></color> {message}\n";
        chatContentText.text += formattedLine;

        // UI düzeni yenilenene kadar bekle ve sonra en alta kaydır
        StartCoroutine(ScrollToBottomCoroutine());
    }

    // Arayüz boyutlandırması yapıldıktan sonra kaydıran Coroutine
    private IEnumerator ScrollToBottomCoroutine()
    {
        // 1. TextMeshPro metninin yüksekliğini hesaplat
        if (chatContentText != null)
        {
            chatContentText.ForceMeshUpdate();
        }

        // 2. Content ve ScrollView'in yeni boyutunu zorla hesaplat
        Canvas.ForceUpdateCanvases();
        
        // 3. Bir kare bekle (Layout bileşenleri otursun)
        yield return new WaitForEndOfFrame();

        // 4. Yeniden boyutlandırmayı garantiye al
        if (chatScrollRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatScrollRect.content);
            chatScrollRect.verticalNormalizedPosition = 0f; // 0f = En alt
        }
    }

    public void OnClickCopyLobbyCode()
    {
        if (currentLobbyID != CSteamID.Nil)
        {
            GUIUtility.systemCopyBuffer = currentLobbyID.ToString();
            AddChatMessage("Sistem", "Lobi kodu panoya kopyalandı!", "#8E9AAF");
        }
    }
}