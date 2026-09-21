using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Rende cliccabile il MODELLO 3D del pianeta: cliccandolo attiva il bottone
/// corrispondente (es. Btn_Marte), quindi seleziona il pianeta come se avessi
/// premuto sul bottone.
///
/// Funziona su PC/Editor (OnMouseDown) e su Quest (IPointerClick, come i personaggi
/// della prima scena).
///
/// Serve:
///  1) un Collider sul pianeta (es. Sphere Collider);
///  2) un Physics Raycaster sulla telecamera (lo stesso che fa funzionare i
///     personaggi 3D nella WaitingRoom).
/// </summary>
public class PlanetClicker : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("Il bottone del pianeta da attivare (es. Btn_Marte / Btn_Saturno / Btn_Luna).")]
    public Button targetButton;

    // Quest / XR
    public void OnPointerClick(PointerEventData _) => Trigger();

    // PC / Editor
    void OnMouseDown() => Trigger();

    void Trigger()
    {
        if (targetButton != null && targetButton.interactable)
        {
            AudioManager.Instance?.PlayButtonClick();
            targetButton.onClick.Invoke();
        }
    }
}
