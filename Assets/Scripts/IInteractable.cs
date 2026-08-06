using UnityEngine;

public interface IInteractable
{
    // Objenin adı veya etkileşim ipucu (Örn: "E - Bardağı Tut", "E - Kapıyı Aç")
    string GetInteractPrompt();

    // Etkileşim gerçekleştiğinde ne olsun?
    void OnInteract(GameObject interactor);

    // Bırakma veya etkileşimi bitirme durumu (Tutmalı objeler için)
    void OnStopInteract(GameObject interactor);
}