using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestisce il menu di scelta del pianeta (SampleScene).
/// - Al click su un pianeta: evidenzia il bottone scelto (HoloButton "selezionato"),
///   spegne gli altri, mostra il modello 3D del pianeta e memorizza la scelta.
/// - "Accetta Missione" resta disattivato finche non scegli un pianeta, poi carica
///   la scena della missione (il razzo).
///
/// Collega tutto da solo in Start(): nell'Inspector basta assegnare gli array e i
/// riferimenti, senza wiring manuale degli OnClick.
/// </summary>
public class PlanetMenu : MonoBehaviour
{
    [Header("Bottoni pianeta (stesso ordine dei nomi)")]
    [SerializeField] Button[] planetButtons;                 // Btn_Marte, Btn_Saturno, Btn_Luna
    [SerializeField] string[] planetNames = { "Marte", "Saturno", "Luna" };

    [Header("Anteprima 3D (opzionale)")]
    [SerializeField] PlanetShowcase showcase;                // mostra il modello del pianeta scelto

    [Header("Bottone Accetta Missione")]
    [SerializeField] Button startButton;

    [Header("Scena della missione (il razzo)")]
    [Tooltip("Deve essere in Build Settings. Nel progetto il razzo e in 'LaunchMission'.")]
    [SerializeField] string missionScene = "LaunchMission";

    [Header("Bottone Indietro (torna alla scelta personaggio)")]
    [SerializeField] Button backButton;
    [SerializeField] string previousScene = "WaitingRoom";

    string m_Selected;

    void Start()
    {
        // Collega i click dei pianeti
        for (int i = 0; i < planetButtons.Length; i++)
        {
            if (planetButtons[i] == null) continue;
            int idx = i; // cattura corretta per la closure
            planetButtons[i].onClick.AddListener(() => Select(idx));
        }

        // Collega Accetta Missione e disattivalo (smorzato) finche non si sceglie
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartMission);
            m_StartGroup = startButton.GetComponent<CanvasGroup>();
            if (m_StartGroup == null) m_StartGroup = startButton.gameObject.AddComponent<CanvasGroup>();
            SetStartEnabled(false);
        }

        // Bottone Indietro -> torna alla scena precedente
        if (backButton != null)
            backButton.onClick.AddListener(GoBack);

        // Nessun pianeta mostrato all'avvio
        showcase?.MostraPianeta("");
    }

    /// <summary>Torna alla scena precedente (scelta personaggio).</summary>
    public void GoBack()
    {
        AudioManager.Instance?.PlayButtonClick();
        if (GameManager.Instance != null)
            GameManager.Instance.LoadScene(previousScene);
        else
            SceneManager.LoadScene(previousScene);
    }

    CanvasGroup m_StartGroup;

    // Accende/smorza visivamente "Accetta Missione"
    void SetStartEnabled(bool on)
    {
        if (startButton == null) return;
        startButton.interactable = on;
        if (m_StartGroup != null) m_StartGroup.alpha = on ? 1f : 0.35f;
        startButton.GetComponent<HoloButton>()?.SetSelected(on); // glow teal quando attivo
    }

    /// <summary>Seleziona il pianeta all'indice dato.</summary>
    public void Select(int index)
    {
        if (index < 0 || index >= planetButtons.Length) return;

        m_Selected = (index < planetNames.Length) ? planetNames[index] : null;

        // Evidenzia il selezionato, spegni gli altri
        for (int i = 0; i < planetButtons.Length; i++)
        {
            var holo = planetButtons[i] != null ? planetButtons[i].GetComponent<HoloButton>() : null;
            holo?.SetSelected(i == index);
        }

        // Mostra il modello 3D
        if (showcase != null && m_Selected != null)
            showcase.MostraPianeta(m_Selected);

        // Memorizza la scelta
        if (GameManager.Instance != null && m_Selected != null)
            GameManager.Instance.SelectedPlanet = m_Selected;

        // Accende "Accetta Missione"
        SetStartEnabled(true);

        AudioManager.Instance?.PlayButtonClick();

        // Voce guida: rassicurazione sulla scelta
        if (m_Selected != null)
            VoiceGuide.Speak($"Hai scelto {m_Selected}! Ottima scelta. Quando sei pronto, premi Accetta Missione.");
    }

    /// <summary>Carica la scena della missione (razzo).</summary>
    public void StartMission()
    {
        if (string.IsNullOrEmpty(m_Selected)) return; // niente pianeta = niente partenza

        AudioManager.Instance?.PlayButtonClick();

        if (GameManager.Instance != null)
            GameManager.Instance.LoadScene(missionScene);
        else
            SceneManager.LoadScene(missionScene);
    }
}
