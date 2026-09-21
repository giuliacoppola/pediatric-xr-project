using UnityEngine;
using System.Collections;

/// <summary>
/// Gestisce lo sfondo della scena del razzo.
///
/// Con "Start In Passthrough" attivo (consigliato): all'inizio si vede LA STANZA REALE
/// attraverso il visore (il bambino resta ancorato all'ambiente durante il prelievo).
/// Al lancio, l'ambiente sfuma gradualmente nel nero dello spazio con le stelle.
///
/// Con "Start In Passthrough" spento: cielo azzurro pieno che sfuma nello spazio.
///
/// Si avvia da solo col lancio (chiamato da MissionController.BeginAscent).
/// Richiede il rig "MR Interaction Setup" (lo stesso della scena scelta pianeta).
/// </summary>
[DisallowMultipleComponent]
public class AtmosphereLaunch : MonoBehaviour
{
    [Header("Passthrough (vedere la stanza reale)")]
    [Tooltip("Se attivo: prima del lancio si vede la stanza reale; al lancio si sfuma nello spazio. NOTA: sul Quest questo percorso passthrough non funziona (nero) — lascialo spento.")]
    public bool startInPassthrough = false;

    [Header("Colori cielo (usati se passthrough spento, e come arrivo al lancio)")]
    public Color atmosphereColor = new Color(0.25f, 0.55f, 0.90f); // azzurro pieno
    public Color spaceColor      = new Color(0.01f, 0.01f, 0.04f); // nero spazio

    [Header("Durata transizione (circa come il volo)")]
    public float duration = 5f;

    [Header("Oggetti spaziali che compaiono salendo (opzionale)")]
    public GameObject[] spaceObjects;

    [Tooltip("A che punto della salita compaiono stelle/nebulose (0-1).")]
    [Range(0f, 1f)] public float spaceRevealAt = 0.4f;

    void Start()
    {
        SetSpace(false);

        if (startInPassthrough)
        {
            // Attiva il passthrough del rig MR e rendi trasparente lo sfondo camera
            FindAnyObjectByType<UnityEngine.XR.Templates.MR.ARFeatureController>()
                ?.TogglePassthrough(true);
            ApplyColor(new Color(0f, 0f, 0f, 0f)); // alpha 0 = si vede la stanza
        }
        else
        {
            ApplyColor(atmosphereColor);
        }
    }

    /// <summary>Avvia la transizione verso lo spazio (chiamata al decollo).</summary>
    public void BeginAscent()
    {
        StopAllCoroutines();
        StartCoroutine(Ascent());
    }

    IEnumerator Ascent()
    {
        // Da dove partiamo: trasparente (passthrough) o azzurro (VR)
        Color from = startInPassthrough ? new Color(0f, 0f, 0f, 0f) : atmosphereColor;
        Color to   = spaceColor; to.a = 1f;

        float t = 0f;
        bool revealed = false;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            ApplyColor(Color.Lerp(from, to, k));
            if (!revealed && k >= spaceRevealAt) { SetSpace(true); revealed = true; }
            yield return null;
        }
        ApplyColor(to);
        SetSpace(true);

        // A fine transizione il passthrough non serve piu (siamo nello spazio)
        if (startInPassthrough)
            FindAnyObjectByType<UnityEngine.XR.Templates.MR.ARFeatureController>()
                ?.TogglePassthrough(false);
    }

    void ApplyColor(Color c)
    {
        foreach (var cam in Camera.allCameras)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = c;
        }
    }

    void SetSpace(bool on)
    {
        if (spaceObjects == null) return;
        foreach (var g in spaceObjects)
            if (g != null) g.SetActive(on);
    }
}
