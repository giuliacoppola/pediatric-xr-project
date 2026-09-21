using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class WaitingRoomManager : MonoBehaviour
{
    public static WaitingRoomManager Instance { get; private set; }

    [Header("Slot personaggi (AstronautaSlot, ScienziataSlot)")]
    [SerializeField] List<GameObject> allSlots;

    [Header("Testo istruzione e step")]
    [SerializeField] TextMeshProUGUI instructionText;
    [SerializeField] TextMeshProUGUI stepIndicator;

    [Header("ARIA — testo narrativo WaitingRoom")]
    [SerializeField] TextMeshProUGUI ariaText;

    [Header("Showcase personaggio selezionato")]
    [Tooltip("Empty GameObject in scena: il personaggio selezionato si sposta qui. Posizionalo con Camera Preview.")]
    [SerializeField] Transform characterShowcasePoint;
    [SerializeField] float     showcaseScale = 1f;

    [Header("Scheda dettagli (destra)")]
    [Tooltip("Empty GameObject in scena: il pannello dettagli si sposta/orienta qui.")]
    [SerializeField] Transform       infoPanelPoint;
    [SerializeField] GameObject      detailsPanel;
    [SerializeField] TextMeshProUGUI detailsName;
    [SerializeField] TextMeshProUGUI detailsStats;
    [SerializeField] GameObject      backButton;
    [SerializeField] GameObject      confirmButton;

    [Header("Avvio missione")]
    [SerializeField] GameObject startMissionButton;

    CharacterPickable selectedCharacter;

    void Awake() => Instance = this;

    static readonly Color uiColor = new Color(0f, 0.88f, 0.82f);

    void Start()
    {
        detailsPanel?.SetActive(false);
        backButton?.SetActive(false);
        startMissionButton?.SetActive(false);
        if (instructionText != null) instructionText.color = Color.white;
        if (stepIndicator   != null) stepIndicator.color   = uiColor;
        SetInstruction("Scegli il tuo personaggio");
        SetStep(0);

        const string welcome = "Ciao, piccola stella! Scegli il tuo personaggio per iniziare la missione.";
        SetAriaText(welcome);
        ARIATTSManager.Instance?.Speak(welcome);
    }

    public void SelectCharacter(string role, CharacterPickable pickable)
    {
        if (selectedCharacter != null) { selectedCharacter.SetHighlight(false); selectedCharacter.Deselect(); }
        selectedCharacter = pickable;

        GameManager.Instance.SelectedRole = role;
        AudioManager.Instance?.PlayButtonClick();

        GameObject selectedSlot = pickable.transform.parent.gameObject;
        foreach (var slot in allSlots)
            slot.SetActive(slot == selectedSlot);

        if (characterShowcasePoint != null)
            pickable.AnimateToShowcase(characterShowcasePoint.position, showcaseScale);

        // Flusso semplificato: un solo bottone. Scelto il personaggio, appare
        // subito "Inizia Missione" (niente passaggio di conferma separato).
        detailsPanel?.SetActive(true);
        if (detailsPanel != null && infoPanelPoint != null)
        {
            detailsPanel.transform.position = infoPanelPoint.position;
            detailsPanel.transform.rotation = infoPanelPoint.rotation;
        }
        backButton?.SetActive(true);
        confirmButton?.SetActive(false);
        startMissionButton?.SetActive(true);

        if (detailsName  != null) { detailsName.text  = role.ToUpper(); detailsName.color  = Color.white; }
        if (detailsStats != null) { detailsStats.text = GetStats(role); detailsStats.color = uiColor; }

        string ariaMsg = $"Ottima scelta, {role}! Premi Inizia quando sei pronto per la missione.";
        SetAriaText(ariaMsg);
        ARIATTSManager.Instance?.Speak(ariaMsg);

        SetInstruction("Pronto per la missione!");
        SetStep(1);
    }

    public void GoBack()
    {
        if (selectedCharacter != null)
        {
            selectedCharacter.SetHighlight(false);
            selectedCharacter.ResetToOriginal();
            selectedCharacter = null;
        }
        foreach (var slot in allSlots)
            slot.SetActive(true);
        detailsPanel?.SetActive(false);
        backButton?.SetActive(false);
        confirmButton?.SetActive(false);
        startMissionButton?.SetActive(false);
        SetInstruction("Scegli il tuo personaggio");
        SetStep(0);
        SetAriaText("Nessun problema! Scegli il personaggio che preferisci.");
    }

    public void ConfirmCharacter()
    {
        selectedCharacter?.PlayConfirm();
        confirmButton?.SetActive(false);
        backButton?.SetActive(false);
        AudioManager.Instance?.PlayCharacterConfirm();
        SetInstruction("Missione accettata!");
        SetStep(2);

        string role = GameManager.Instance?.SelectedRole ?? "Eroe";

        // Testo su schermo (con a-capo per leggibilità)
        SetAriaText(
            $"Missione accettata, {role}!\n\n" +
            $"La stanza di lancio è pronta.\n" +
            $"Segui la dottoressa — quella è la tua base di partenza.\n" +
            $"Ti aspetto là per il conto alla rovescia."
        );

        // TTS: frase unica senza a-capo per pronuncia naturale
        ARIATTSManager.Instance?.Speak(
            $"Missione accettata, {role}! " +
            $"La stanza di lancio è pronta. " +
            $"Segui la dottoressa: quella è la tua base di partenza. " +
            $"Ti aspetto là per il conto alla rovescia."
        );

        // Il bottone "Entra nella stanza di lancio" appare dopo che ARIA ha finito di parlare
        StartCoroutine(ShowStartButtonAfterDelay(2.5f));
    }

    public void OnStartMission()
    {
        AudioManager.Instance?.PlayButtonClick();
        ARIATTSManager.Instance?.Stop();
        GameManager.Instance.LoadScene("Posizionamento");
    }

    IEnumerator ShowStartButtonAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        startMissionButton?.SetActive(true);
    }

    void SetAriaText(string text)
    {
        if (ariaText != null) ariaText.text = text;
    }

    string GetStats(string role)
    {
        return role switch
        {
            "Astronauta" =>
                "CORAGGIO      ●●●●●\n" +
                "RESISTENZA    ●●●●○\n" +
                "VELOCITÀ      ●●●○○\n" +
                "INTELLIGENZA  ●●●○○\n" +
                "PRECISIONE    ●●○○○",
            "Scienziato" =>
                "CORAGGIO      ●●●○○\n" +
                "RESISTENZA    ●●○○○\n" +
                "VELOCITÀ      ●●●●○\n" +
                "INTELLIGENZA  ●●●●●\n" +
                "PRECISIONE    ●●●●●",
            _ =>
                "CORAGGIO      ●●●○○\n" +
                "RESISTENZA    ●●●○○\n" +
                "VELOCITÀ      ●●●○○\n" +
                "INTELLIGENZA  ●●●○○\n" +
                "PRECISIONE    ●●●○○"
        };
    }

    void SetInstruction(string text) { if (instructionText != null) instructionText.text = text; }

    void SetStep(int step)
    {
        if (stepIndicator == null) return;
        stepIndicator.text = step switch
        {
            0 => "● PERSONAGGIO  →  ○ PIANETA  →  ○ PARTENZA",
            1 => "✓ PERSONAGGIO  →  ○ PIANETA  →  ● PARTENZA",
            2 => "✓ PERSONAGGIO  →  ○ PIANETA  →  ✓ PARTENZA",
            _ => ""
        };
    }
}
