using System.Collections;
using UnityEngine;

// Gestisce la voce di ARIA.
// Su Quest 3 (Android): Android TTS in italiano.
// Su PC Windows (Editor): Windows Speech via PowerShell.
// Fallback: se ariaVoiceSource ha un clip assegnato, usa quello.
public class ARIATTSManager : MonoBehaviour
{
    public static ARIATTSManager Instance { get; private set; }

    [Header("Clip pre-registrata (opzionale — sovrascrive TTS se assegnata)")]
    [SerializeField] AudioSource ariaVoiceSource;

    [Header("Parametri voce TTS")]
    [SerializeField] [Range(0.5f, 1.5f)] float speechRate = 0.85f;
    [SerializeField] [Range(0.5f, 2.0f)] float pitch      = 1.05f;

#if UNITY_ANDROID && !UNITY_EDITOR
    AndroidJavaObject tts;
    volatile bool     ttsReady;
    bool              ttsConfigured;

    class InitListener : AndroidJavaProxy
    {
        readonly ARIATTSManager owner;
        public InitListener(ARIATTSManager m) : base("android.speech.tts.TextToSpeech$OnInitListener") => owner = m;
        void onInit(int status) => owner.ttsReady = (status == 0);
    }
#endif

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    System.Diagnostics.Process currentSpeechProcess;
#endif

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitAndroidTTS();
    }

    void InitAndroidTTS()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using var player   = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, new InitListener(this));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[ARIA TTS] Init fallita: " + e.Message);
        }
#endif
    }

    // ── Banca voci pre-registrate (Resources/ARIA/*.aiff|wav) ──
    // Sul Quest NON esiste un motore TTS: le frasi vengono riprodotte da clip
    // generate sul Mac con lo script tools/genera_voci.py. La chiave e un hash
    // del testo, identico tra C# e Python.
    static System.Collections.Generic.Dictionary<string, AudioClip> s_VoiceBank;
    AudioSource m_BankSource;

    static string VoiceKey(string text)
    {
        // normalizza: minuscole, solo lettere/numeri
        var sb = new System.Text.StringBuilder();
        foreach (char c in text.ToLowerInvariant())
            if (char.IsLetterOrDigit(c)) sb.Append(c);
        string norm = sb.ToString();
        using var md5 = System.Security.Cryptography.MD5.Create();
        byte[] h = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(norm));
        return "aria_" + System.BitConverter.ToString(h).Replace("-", "").Substring(0, 8).ToLowerInvariant();
    }

    void EnsureVoiceBank()
    {
        if (s_VoiceBank != null) return;
        s_VoiceBank = new System.Collections.Generic.Dictionary<string, AudioClip>();
        foreach (var clip in Resources.LoadAll<AudioClip>("ARIA"))
            s_VoiceBank[clip.name] = clip;
        Debug.Log($"[ARIA] VoiceBank: {s_VoiceBank.Count} clip caricate");
    }

    bool TryPlayFromBank(string text)
    {
        EnsureVoiceBank();
        if (!s_VoiceBank.TryGetValue(VoiceKey(text), out var clip) || clip == null)
            return false;

        if (m_BankSource == null)
        {
            m_BankSource = gameObject.AddComponent<AudioSource>();
            m_BankSource.spatialBlend = 0f;
            m_BankSource.volume = 1f;
        }
        m_BankSource.Stop();
        m_BankSource.clip = clip;
        m_BankSource.Play();
        return true;
    }

    public void Speak(string text)
    {
        DebugHUD.ShowARIA(text);

        // 1) Clip pre-registrata della frase (funziona OVUNQUE, Quest compreso)
        if (TryPlayFromBank(text))
            return;

        // 2) Clip manuale assegnata
        if (ariaVoiceSource != null && ariaVoiceSource.clip != null)
        {
            ariaVoiceSource.Play();
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        StartCoroutine(SpeakWhenReady(text));
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        SpeakWindows(text);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        SpeakMac(text);
#endif
    }

    public void Stop()
    {
        m_BankSource?.Stop();
#if UNITY_ANDROID && !UNITY_EDITOR
        tts?.Call<int>("stop");
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        try { currentSpeechProcess?.Kill(); } catch { }
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        try { currentSayProcess?.Kill(); } catch { }
#endif
    }

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    System.Diagnostics.Process currentSayProcess;

    // macOS: usa il comando di sistema 'say' (voce italiana Alice)
    void SpeakMac(string text)
    {
        string safe = text.Replace("\"", "").Replace("\n", " ").Replace("\r", "");
        int wpm = Mathf.RoundToInt(175f * speechRate); // parole al minuto

        try { currentSayProcess?.Kill(); } catch { }

        var psi = new System.Diagnostics.ProcessStartInfo("/usr/bin/say")
        {
            Arguments       = $"-v Alice -r {wpm} \"{safe}\"",
            CreateNoWindow  = true,
            UseShellExecute = false
        };
        try { currentSayProcess = System.Diagnostics.Process.Start(psi); }
        catch (System.Exception e) { Debug.LogWarning("[ARIA say] " + e.Message); }
    }
#endif

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    void SpeakWindows(string text)
    {
        // Rimuove caratteri che rompono il comando PowerShell
        string safe = text.Replace("'", "").Replace("\"", "").Replace("\n", " ").Replace("\r", "");

        string rateValue = Mathf.RoundToInt((speechRate - 1f) * 10f).ToString(); // -1 to +1 range

        string cmd =
            $"Add-Type -AssemblyName System.speech; " +
            $"$s = New-Object System.Speech.Synthesis.SpeechSynthesizer; " +
            $"$s.Rate = {rateValue}; " +
            $"$s.Speak('{safe}');";

        try
        {
            currentSpeechProcess?.Kill();
        }
        catch { }

        var psi = new System.Diagnostics.ProcessStartInfo("powershell")
        {
            Arguments      = $"-WindowStyle Hidden -NonInteractive -Command \"{cmd}\"",
            CreateNoWindow = true,
            UseShellExecute = false
        };

        currentSpeechProcess = System.Diagnostics.Process.Start(psi);
    }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
    IEnumerator SpeakWhenReady(string text)
    {
        float timeout = 3f;
        while (!ttsReady && timeout > 0f) { timeout -= Time.deltaTime; yield return null; }
        if (!ttsReady) { Debug.LogWarning("[ARIA TTS] Timeout inizializzazione"); yield break; }

        if (!ttsConfigured)
        {
            using var locale = new AndroidJavaObject("java.util.Locale", "it", "IT");
            tts.Call<AndroidJavaObject>("setLanguage", locale);
            tts.Call<AndroidJavaObject>("setSpeechRate", speechRate);
            tts.Call<AndroidJavaObject>("setPitch", pitch);
            ttsConfigured = true;
        }

        tts.Call<int>("speak", text, 0, null, "aria_" + Time.frameCount);
    }
#endif

    void OnDestroy()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        tts?.Call("shutdown");
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        try { currentSpeechProcess?.Kill(); } catch { }
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        try { currentSayProcess?.Kill(); } catch { }
#endif
    }
}
