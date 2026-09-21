using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Applica lo STESSO stile a tutta la UI di una scena, per rendere le tre scene coerenti:
/// - font Orbitron su tutti i testi TextMeshPro;
/// - colore teal di accento;
/// - bottoni "olografici" (HoloButton) su tutti i Button.
///
/// Uso: aggiungi questo componente al Canvas principale della scena (o all'oggetto
/// che contiene la tua UII), assegna il font Orbitron, premi Play. Fatto.
/// Ripeti in ogni scena (WaitingRoom, SceltaPianeta, LaunchMission) per uniformarle.
/// </summary>
[DisallowMultipleComponent]
public class UITheme : MonoBehaviour
{
    [Header("Stile")]
    [Tooltip("Assegna 'Assets/Fonts/Orbitron-Bold SDF'.")]
    public TMP_FontAsset orbitronFont;
    public Color accent = new Color(0f, 0.88f, 0.82f);

    [Header("Cosa applicare")]
    public bool applyFont = true;
    public bool addHoloButtons = true;
    [Tooltip("Colora anche i testi non-bottone con l'accento teal.")]
    public bool tintAllTexts = false;

    [Header("Ambito (vuoto = questo oggetto e i suoi figli)")]
    public Transform root;

    void Start()
    {
        Transform r = root != null ? root : transform;

        // Font (e colore) su tutti i testi TMP
        if (applyFont || tintAllTexts)
        {
            foreach (var t in r.GetComponentsInChildren<TMP_Text>(true))
            {
                if (applyFont && orbitronFont != null) t.font = orbitronFont;
                if (tintAllTexts) t.color = accent;
            }
        }

        // Bottoni olografici coerenti su tutti i Button
        if (addHoloButtons)
        {
            foreach (var b in r.GetComponentsInChildren<Button>(true))
            {
                if (b.GetComponent<HoloButton>() == null)
                {
                    var h = b.gameObject.AddComponent<HoloButton>();
                    h.themeColor = accent;
                }
                // Evita che il Color Tint del Button litighi con HoloButton
                b.transition = Selectable.Transition.None;
            }
        }
    }
}
