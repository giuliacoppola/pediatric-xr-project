using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Gestisce la sala d'attesa: selezione pianeta e ruolo con feedback visivo.
// Assegna planetButtons e roleButtons nell'Inspector nello stesso ordine dei bottoni nella scena.
public class WaitingRoomUI : MonoBehaviour
{
    [Header("Bottoni")]
    [SerializeField] Button[] planetButtons; // ordine: Marte, Saturno, Luna
    [SerializeField] Button[] roleButtons;   // ordine: Comandante, Pilota, Scienziato, Esploratore
    [SerializeField] Button   startButton;

    [Header("Colori selezione")]
    [SerializeField] Color selectedColor = new Color(0.30f, 0.85f, 1.00f); // ciano
    [SerializeField] Color normalColor   = Color.white;

    string selectedPlanet;
    string selectedRole;

    void Start()
    {
        startButton.interactable = false;
        AudioManager.Instance?.PlayButtonClick();
    }

    // Collega ogni bottone pianeta con il nome come parametro stringa.
    // Es: SelectPlanet("Marte"), SelectPlanet("Saturno"), SelectPlanet("Luna")
    public void SelectPlanet(string planet)
    {
        selectedPlanet = planet;
        GameManager.Instance.SelectedPlanet = planet;
        HighlightSelected(planetButtons, planet);
        AudioManager.Instance?.PlayButtonClick();
        UpdateStartButton();
    }

    // Collega ogni bottone ruolo con il nome come parametro stringa.
    // Es: SelectRole("Comandante"), SelectRole("Pilota"), SelectRole("Scienziato"), SelectRole("Esploratore")
    public void SelectRole(string role)
    {
        selectedRole = role;
        GameManager.Instance.SelectedRole = role;
        HighlightSelected(roleButtons, role);
        AudioManager.Instance?.PlayButtonClick();
        UpdateStartButton();
    }

    public void OnStartMission()
    {
        AudioManager.Instance?.PlayButtonClick();
        GameManager.Instance.LoadScene("Posizionamento");
    }

    void UpdateStartButton()
    {
        startButton.interactable = selectedPlanet != null && selectedRole != null;
    }

    // Evidenzia il bottone selezionato e resetta gli altri al colore normale.
    // Il confronto si basa sul testo TMP del bottone.
    void HighlightSelected(Button[] buttons, string selectedValue)
    {
        foreach (Button btn in buttons)
        {
            var label = btn.GetComponentInChildren<Text>();
            bool isSelected = label != null &&
                label.text.Trim().Equals(selectedValue.Trim(), System.StringComparison.OrdinalIgnoreCase);
            Color target = isSelected ? selectedColor : normalColor;
            ColorBlock colors = btn.colors;
            colors.normalColor     = target;
            colors.selectedColor   = target;
            colors.highlightedColor = target;
            btn.colors = colors;
        }
        StartCoroutine(DeselectAfterFrame());
    }

    IEnumerator DeselectAfterFrame()
    {
        yield return null;
        EventSystem.current.SetSelectedGameObject(null);
    }
}
