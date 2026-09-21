using UnityEngine;
using System.Collections;

/// <summary>
/// Guida vocale di scena: fa parlare ARIA all'avvio con una o piu frasi (istruzioni +
/// rassicurazione), utile per i bambini che non leggono o sono troppo ansiosi.
///
/// Uso: crea un GameObject vuoto (es. "Guida Vocale"), aggiungi questo componente e
/// scrivi le frasi in "Intro Lines". Vengono dette in sequenza all'avvio della scena.
///
/// Nota: la voce si sente sul Quest (TTS Android), su Windows e su Mac (comando 'say').
/// Nell'editor Mac ora funziona; se non senti nulla controlla il volume di sistema.
///
/// Altri script possono far parlare ARIA con: VoiceGuide.Speak("frase");
/// </summary>
[DisallowMultipleComponent]
public class VoiceGuide : MonoBehaviour
{
    [Header("Frasi dette all'avvio della scena")]
    [TextArea] public string[] introLines;

    [Header("Tempi")]
    public float startDelay   = 1.2f;
    public float pauseBetween = 3.5f;

    void Start()
    {
        EnsureAria();
        if (introLines != null && introLines.Length > 0)
            StartCoroutine(PlayIntro());
    }

    IEnumerator PlayIntro()
    {
        yield return new WaitForSeconds(startDelay);
        foreach (var line in introLines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            Speak(line);
            yield return new WaitForSeconds(pauseBetween);
        }
    }

    /// <summary>Fa parlare ARIA da qualunque script. Crea il gestore voce se manca.</summary>
    public static void Speak(string text)
    {
        EnsureAria();
        ARIATTSManager.Instance?.Speak(text);
    }

    static void EnsureAria()
    {
        if (ARIATTSManager.Instance == null)
            new GameObject("ARIATTSManager").AddComponent<ARIATTSManager>();
    }
}
