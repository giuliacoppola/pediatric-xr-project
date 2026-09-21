using UnityEngine;

// Gestisce musica e SFX. Gli SFX della WaitingRoom sono generati proceduralmente
// in Awake — non serve assegnare nulla nell'Inspector.
// I campi SerializeField sono override opzionali: se assegni un clip, usa quello;
// altrimenti usa il suono procedurale generato.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sorgenti audio")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource sfxSource;

    [Header("Musica (opzionale)")]
    [SerializeField] AudioClip backgroundMusic;

    [Header("SFX — WaitingRoom (opzionali, sovrascrivono il procedurale)")]
    [SerializeField] AudioClip sfxButtonClick;
    [SerializeField] AudioClip sfxCharacterHover;
    [SerializeField] AudioClip sfxCharacterConfirm;

    [Header("SFX — SpaceMission (opzionali)")]
    [SerializeField] AudioClip sfxEngineStable;
    [SerializeField] AudioClip sfxFuelLoading;
    [SerializeField] AudioClip sfxCountdownTick;
    [SerializeField] AudioClip sfxLaunch;
    [SerializeField] AudioClip sfxMissionComplete;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSources();
        GenerateProceduralSFX();
    }

    void Start()
    {
        if (musicSource != null && backgroundMusic != null)
        {
            musicSource.clip   = backgroundMusic;
            musicSource.loop   = true;
            musicSource.volume = 0.4f;
            musicSource.Play();
        }
    }

    // Crea AudioSource se non assegnati nell'Inspector
    void EnsureAudioSources()
    {
        if (musicSource == null)
        {
            musicSource        = gameObject.AddComponent<AudioSource>();
            musicSource.loop   = true;
            musicSource.volume = 0.4f;
        }
        if (sfxSource == null)
        {
            sfxSource        = gameObject.AddComponent<AudioSource>();
            sfxSource.loop   = false;
            sfxSource.volume = 1f;
        }
    }

    // Genera i suoni WaitingRoom se non è stato assegnato un clip manualmente
    void GenerateProceduralSFX()
    {
        if (sfxButtonClick    == null) sfxButtonClick    = MakeTone(750f, 0.06f, 0.25f);
        if (sfxCharacterHover == null) sfxCharacterHover = MakeGlide(320f, 520f, 0.18f, 0.20f);
        if (sfxCharacterConfirm == null) sfxCharacterConfirm =
            MakeArpeggio(new[] { 523f, 659f, 784f }, 0.10f, 0.06f, 0.35f);
    }

    // Tono puro con attacco rapido e decay morbido
    AudioClip MakeTone(float freq, float duration, float volume)
    {
        const int rate = 44100;
        int n = Mathf.Max(1, (int)(rate * duration));
        float[] data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t   = (float)i / n;
            float env = t < 0.05f
                ? t / 0.05f
                : Mathf.Pow(1f - t, 1.5f);
            data[i] = volume * env * Mathf.Sin(2f * Mathf.PI * freq * i / rate);
        }
        var clip = AudioClip.Create("btn", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Glide tra due frequenze (effetto shimmer/holo)
    AudioClip MakeGlide(float freqA, float freqB, float duration, float volume)
    {
        const int rate = 44100;
        int n = Mathf.Max(1, (int)(rate * duration));
        float[] data = new float[n];
        float   phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t    = (float)i / n;
            float freq = Mathf.Lerp(freqA, freqB, t);
            phase += 2f * Mathf.PI * freq / rate;
            float env = Mathf.Sin(Mathf.PI * t); // curva a campana
            data[i] = volume * env * Mathf.Sin(phase);
        }
        var clip = AudioClip.Create("hover", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Arpeggio ascendente (do-mi-sol)
    AudioClip MakeArpeggio(float[] freqs, float noteDur, float gap, float volume)
    {
        const int rate = 44100;
        int noteN  = Mathf.Max(1, (int)(rate * noteDur));
        int gapN   = Mathf.Max(0, (int)(rate * gap));
        int totalN = freqs.Length * (noteN + gapN);
        float[] data = new float[totalN];

        for (int fi = 0; fi < freqs.Length; fi++)
        {
            int offset = fi * (noteN + gapN);
            for (int i = 0; i < noteN; i++)
            {
                float t   = (float)i / noteN;
                float env = Mathf.Sin(Mathf.PI * t);
                data[offset + i] = volume * env *
                    Mathf.Sin(2f * Mathf.PI * freqs[fi] * i / rate);
            }
        }
        var clip = AudioClip.Create("confirm", totalN, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    void PlaySfx(AudioClip clip, string sfxName)
    {
        DebugHUD.ShowSFX(sfxName);
        if (sfxSource != null && clip != null)
            sfxSource.PlayOneShot(clip);
    }

    // WaitingRoom
    public void PlayButtonClick()      => PlaySfx(sfxButtonClick,     "ButtonClick");
    public void PlayCharacterHover()   => PlaySfx(sfxCharacterHover,  "CharacterHover");
    public void PlayCharacterConfirm() => PlaySfx(sfxCharacterConfirm,"CharacterConfirm");

    // SpaceMission
    public void PlayEngineStable()     => PlaySfx(sfxEngineStable,    "EngineStable");
    public void PlayFuelLoading()      => PlaySfx(sfxFuelLoading,     "FuelLoading");
    public void PlayCountdownTick()    => PlaySfx(sfxCountdownTick,   "CountdownTick");
    public void PlayLaunch()           => PlaySfx(sfxLaunch,          "Launch");
    public void PlayMissionComplete()  => PlaySfx(sfxMissionComplete, "MissionComplete");
}
