using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Scena "Posizionamento": ARIA racconta la missione (con il nome del personaggio scelto),
/// le frasi compaiono come sottotitolo una alla volta, l'ultima e l'istruzione di sedersi.
/// Solo alla FINE di tutte le frasi si sblocca il tasto A e compare "Premi A".
///
/// La prima scritta appare subito; ARIA la dice 2 secondi dopo. Poi le frasi in sequenza.
/// </summary>
[DisallowMultipleComponent]
public class PositioningGuide : MonoBehaviour
{
    [Header("Scena successiva")]
    public string nextScene = "SceltaPianeta";

    [Header("Aspetto")]
    public Color mainColor = new Color(0f, 0.88f, 0.82f); // teal
    public float distance = 2.2f;

    // {P} = nome del personaggio scelto (Astronauta / Scienziato)
    static readonly string[] StoryLines =
    {
        "{P}, stai per partire per una missione molto importante!",
        "Prima però ci serve la Scintilla, il carburante speciale del razzo. Si raccoglie solo restando molto fermi.",
        "Quando la squadra avrà preparato il razzo, ti siederai per l'estrazione. Da lì in poi è fondamentale non muoversi, finché il serbatoio non è pieno.",
    };
    const string FinalText = "Mettiti comodo\nsulla poltrona";
    const string ReadyText = "Premi  A  per continuare";

    TMP_Text m_Sub, m_Main, m_Ready;
    bool m_Going, m_Unlocked;

    void Start()
    {
        m_Sub   = MakeText("SubText",   1.5f, Color.white, 0.15f, 3.6f, 2.4f);
        m_Main  = MakeText("MainText",  2.8f, mainColor,   0.35f, 8.0f, 1.4f);
        m_Ready = MakeText("ReadyText", 1.2f, mainColor,  -0.85f, 5.0f, 0.8f);

        m_Ready.text = ReadyText;
        m_Sub.gameObject.SetActive(false);
        m_Main.gameObject.SetActive(false);
        m_Ready.gameObject.SetActive(false);

        StartCoroutine(PulseReady());
        StartCoroutine(Sequence());
    }

    IEnumerator Sequence()
    {
        string role = (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.SelectedRole))
            ? GameManager.Instance.SelectedRole : "Astronauta";

        m_Sub.gameObject.SetActive(true);

        for (int i = 0; i < StoryLines.Length; i++)
        {
            if (m_Going) yield break;
            string line = StoryLines[i].Replace("{P}", role);
            m_Sub.text = line;                              // la scritta appare subito
            if (i == 0) yield return new WaitForSeconds(2f); // ARIA parte 2 secondi dopo
            VoiceGuide.Speak(line);
            yield return new WaitForSeconds(SpeakTime(line));
        }
        if (m_Going) yield break;

        // Ultima: istruzione grande "Mettiti comodo sulla poltrona"
        m_Sub.gameObject.SetActive(false);
        m_Main.gameObject.SetActive(true);
        m_Main.text = FinalText;
        VoiceGuide.Speak(FinalText);
        yield return new WaitForSeconds(SpeakTime(FinalText) + 0.5f);

        // Solo ORA si puo premere A e compare la scritta
        Unlock();
    }

    // Durata stimata del parlato in base al numero di parole (per non accavallare le frasi)
    float SpeakTime(string line)
    {
        int words = line.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).Length;
        return 2f + words * 0.42f;
    }

    void Unlock()
    {
        if (m_Unlocked) return;
        m_Unlocked = true;
        m_Ready.gameObject.SetActive(true);
    }

    void Update()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            PlaceInFront(m_Sub.transform,   cam,  0.15f);
            PlaceInFront(m_Main.transform,  cam,  0.35f);
            PlaceInFront(m_Ready.transform, cam, -0.85f);
        }

        if (m_Going || !m_Unlocked) return;

        bool go = false;
        try { go = OVRInput.GetDown(OVRInput.RawButton.A) || OVRInput.GetDown(OVRInput.RawButton.X); }
        catch { }
        if (go) Proceed();
    }

    /// <summary>Avanza alla scena successiva (chiamabile anche dalla web app operatore).</summary>
    public void Proceed()
    {
        if (m_Going || !m_Unlocked) return; // solo dopo tutte le frasi
        m_Going = true;
        AudioManager.Instance?.PlayButtonClick();
        VoiceGuide.Speak("Perfetto! Andiamo a scegliere il pianeta.");
        if (GameManager.Instance != null) GameManager.Instance.LoadScene(nextScene);
        else UnityEngine.SceneManagement.SceneManager.LoadScene(nextScene);
    }

    void PlaceInFront(Transform t, Camera cam, float heightOffset)
    {
        Vector3 fwd = cam.transform.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 target = cam.transform.position + fwd * distance + Vector3.up * heightOffset;
        t.position = Vector3.Lerp(t.position, target, Time.deltaTime * 3f);
        t.rotation = Quaternion.LookRotation(t.position - cam.transform.position);
    }

    TMP_Text MakeText(string name, float size, Color color, float heightOffset, float width, float height)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.fontSize  = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = color;
        tmp.rectTransform.sizeDelta = new Vector2(width, height);
        tmp.enableWordWrapping = true;
        tmp.fontStyle = FontStyles.Bold;
        return tmp;
    }

    IEnumerator PulseReady()
    {
        while (true)
        {
            if (m_Ready != null && m_Ready.gameObject.activeSelf)
            {
                float a = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 2.5f);
                var c = m_Ready.color; c.a = a; m_Ready.color = c;
            }
            yield return null;
        }
    }
}
