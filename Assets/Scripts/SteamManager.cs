using UnityEngine;
using Steamworks;

public class SteamManager : MonoBehaviour
{
    private static SteamManager instance;
    public static SteamManager Instance => instance;

    private bool isInitialized = false;
    public static bool Initialized => Instance != null && Instance.isInitialized;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        try
        {
            // Steam API'yi başlatıyoruz
            if (SteamAPI.Init())
            {
                isInitialized = true;
                string personaName = SteamFriends.GetPersonaName();
                Debug.Log($"[SteamManager] Steam başarıyla bağlandı! Oyuncu: {personaName}");
            }
            else
            {
                Debug.LogError("[SteamManager] SteamAPI.Init() başarısız oldu. Steam açık mı?");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SteamManager] Steam başlatılırken hata oluştu: {e.Message}");
        }
    }

    private void Update()
    {
        if (isInitialized)
        {
            // Steam callback'lerini her karede çalıştırır (Lobi, davetler vs. için şarttır)
            SteamAPI.RunCallbacks();
        }
    }

    private void OnApplicationQuit()
    {
        if (isInitialized)
        {
            SteamAPI.Shutdown();
        }
    }
}