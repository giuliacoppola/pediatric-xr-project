using UnityEngine;

// Garantisce che ci sia esattamente un AudioListener attivo nella scena.
// Mantiene quello sulla Main Camera (tag "MainCamera"), disabilita tutti gli altri.
// Va messo su qualsiasi GameObject nella scena — si auto-gestisce in Awake.
public class AudioListenerManager : MonoBehaviour
{
    void Awake()
    {
        var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        if (listeners.Length <= 1) return;

        Camera mainCam = Camera.main;
        foreach (var listener in listeners)
        {
            bool isOnMainCamera = mainCam != null &&
                                  listener.gameObject == mainCam.gameObject;
            listener.enabled = isOnMainCamera;
        }

        // Se nessuno è sulla main camera, abilita solo il primo
        bool anyEnabled = false;
        foreach (var listener in listeners)
            if (listener.enabled) { anyEnabled = true; break; }

        if (!anyEnabled && listeners.Length > 0)
            listeners[0].enabled = true;
    }
}
