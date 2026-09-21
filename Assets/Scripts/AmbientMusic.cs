using UnityEngine;
using System.Collections;

/// <summary>
/// Musica ambientale rilassante generata proceduralmente (nessun file audio, nessun
/// problema di copyright). Pensata per pazienti pediatrici: volume basso, nessun suono
/// improvviso, accordi morbidi che "respirano" e delicati rintocchi su scala pentatonica
/// (sempre consonanti).
///
/// Uso: aggiungi questo componente a un GameObject qualsiasi nella scena
/// (WaitingRoom, scelta pianeta, ecc.). Si genera e si avvia da solo con una dolce
/// dissolvenza in entrata. Non serve assegnare nulla.
/// </summary>
[DisallowMultipleComponent]
public class AmbientMusic : MonoBehaviour
{
    [Header("Volume (tienilo basso)")]
    [Range(0f, 1f)] public float masterVolume = 0.5f;
    [Range(0f, 1f)] public float padVolume  = 0.55f;
    [Range(0f, 1f)] public float bellVolume = 0.32f;

    [Header("Dissolvenza in entrata")]
    public float fadeInSeconds = 6f;

    [Header("Rintocchi carillon")]
    [Tooltip("Intervallo (secondi) tra un rintocco e l'altro.")]
    public float bellIntervalMin = 4.5f;
    public float bellIntervalMax = 9f;
    [Tooltip("Probabilita di silenzio invece del rintocco (per lasciare respiro).")]
    [Range(0f, 1f)] public float bellRestChance = 0.25f;

    [Header("Continuita")]
    [Tooltip("Se attivo, la musica continua tra una scena e l'altra senza ricominciare.")]
    public bool persistAcrossScenes = false;

    const int   k_Rate = 44100;
    const float k_LoopSeconds = 8f; // il pad e un loop perfetto di 8 secondi

    // Scala pentatonica di Do maggiore (sempre consonante, adatta ai bambini)
    static readonly float[] k_Pentatonic =
        { 261.63f, 293.66f, 329.63f, 392.00f, 440.00f, 523.25f };

    static AmbientMusic s_Instance;

    AudioSource m_Pad;
    AudioSource m_Bell;
    AudioClip[] m_BellClips;

    void Awake()
    {
        if (persistAcrossScenes)
        {
            if (s_Instance != null && s_Instance != this) { Destroy(gameObject); return; }
            s_Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        m_Pad  = gameObject.AddComponent<AudioSource>();
        m_Bell = gameObject.AddComponent<AudioSource>();
        foreach (var s in new[] { m_Pad, m_Bell })
        {
            s.playOnAwake = false;
            s.spatialBlend = 0f;   // 2D: ambiente ovunque
            s.loop = false;
        }

        m_Pad.clip = BuildPad();
        m_Pad.loop = true;
        m_BellClips = BuildBells();
    }

    void Start()
    {
        m_Pad.volume = 0f;
        m_Pad.Play();
        StartCoroutine(FadeIn());
        StartCoroutine(BellLoop());
    }

    IEnumerator FadeIn()
    {
        float t = 0f;
        float target = masterVolume * padVolume;
        while (t < fadeInSeconds)
        {
            t += Time.unscaledDeltaTime;
            m_Pad.volume = Mathf.Lerp(0f, target, t / fadeInSeconds);
            yield return null;
        }
        m_Pad.volume = target;
    }

    IEnumerator BellLoop()
    {
        // piccola attesa iniziale per non partire tutto insieme
        yield return new WaitForSeconds(3f);
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(bellIntervalMin, bellIntervalMax));
            if (Random.value < bellRestChance) continue;
            var clip = m_BellClips[Random.Range(0, m_BellClips.Length)];
            m_Bell.PlayOneShot(clip, masterVolume * bellVolume);
        }
    }

    // ── Generazione audio ─────────────────────────────

    // Accordo morbido (Do) con leggero "respiro" di volume. Loop perfetto di 8s.
    AudioClip BuildPad()
    {
        int n = Mathf.RoundToInt(k_Rate * k_LoopSeconds);
        float[] data = new float[n];

        // Note dell'accordo (Do maggiore su piu ottave), arrotondate per un loop senza scatti
        float[] notes = { 130.81f, 164.81f, 196.00f, 261.63f };
        for (int i = 0; i < notes.Length; i++)
            notes[i] = Mathf.Round(notes[i] * k_LoopSeconds) / k_LoopSeconds;

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / k_Rate;
            float sample = 0f;
            foreach (float f in notes)
            {
                // due voci leggermente scordate = calore (chorus)
                sample += Mathf.Sin(2f * Mathf.PI * f * t);
                sample += 0.7f * Mathf.Sin(2f * Mathf.PI * (f * 1.003f) * t);
            }
            sample /= (notes.Length * 1.7f);

            // "respiro" lento: un ciclo ogni 8s (coincide col loop -> nessuno scatto)
            float breathe = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * (1f / k_LoopSeconds) * t);
            data[i] = sample * breathe * 0.9f;
        }

        var clip = AudioClip.Create("AmbientPad", n, 1, k_Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Rintocchi morbidi tipo carillon (attacco dolce, lunga coda)
    AudioClip[] BuildBells()
    {
        var clips = new AudioClip[k_Pentatonic.Length];
        for (int k = 0; k < k_Pentatonic.Length; k++)
            clips[k] = BuildBell(k_Pentatonic[k]);
        return clips;
    }

    AudioClip BuildBell(float freq)
    {
        float dur = 2.6f;
        int n = Mathf.RoundToInt(k_Rate * dur);
        float[] data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / k_Rate;
            float tNorm = (float)i / n;
            // attacco morbido (~40ms) + decadimento esponenziale lungo
            float attack = Mathf.Clamp01(t / 0.04f);
            float decay  = Mathf.Exp(-3.5f * tNorm);
            float env = attack * decay;
            float tone =
                Mathf.Sin(2f * Mathf.PI * freq * t) +
                0.45f * Mathf.Sin(2f * Mathf.PI * freq * 2f * t) +
                0.2f  * Mathf.Sin(2f * Mathf.PI * freq * 3f * t);
            data[i] = env * tone * 0.28f;
        }
        var clip = AudioClip.Create("Bell", n, 1, k_Rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
