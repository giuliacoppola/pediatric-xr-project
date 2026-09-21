using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Bottone "olografico" a tema teal per il menu XR.
/// - Applica automaticamente il colore tema allo sfondo e al testo.
/// - Hover: il bottone si ingrandisce, lo sfondo si accende e parte un glow pulsante.
/// - Click: piccolo "press" + suono.
/// - Usa i suoni procedurali gia presenti in AudioManager (hover + click).
///
/// Come usarlo: aggiungi questo componente all'oggetto bottone (quello con l'Image
/// di sfondo). Se il testo e in un figlio TMP viene trovato da solo. Puoi assegnare
/// un'Image "Glow/Border" opzionale per un bordo luminoso separato.
/// </summary>
[DisallowMultipleComponent]
public class HoloButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("Colore tema")]
    [Tooltip("Colore principale olografico (teal di default, come il resto del gioco).")]
    public Color themeColor = new Color(0f, 0.88f, 0.82f);
    [Tooltip("Colore quando il bottone e SELEZIONATO (azzurro come la scelta personaggio della prima scena).")]
    public Color selectedColor = new Color(0.30f, 0.85f, 1.00f);

    [Header("Sfondo (fill)")]
    [Range(0f, 1f)] public float bgIdleAlpha  = 0.10f;
    [Range(0f, 1f)] public float bgHoverAlpha = 0.30f;
    [Tooltip("Opacita dello sfondo quando il bottone e SELEZIONATO (resta acceso).")]
    [Range(0f, 1f)] public float bgSelectedAlpha = 0.60f;

    [Header("Bordo / testo")]
    [Range(0f, 1f)] public float glowIdle  = 0.55f;
    [Range(0f, 1f)] public float glowHover = 1.00f;

    [Header("Animazione")]
    public float hoverScale    = 1.08f;
    public float selectedScale = 1.05f;
    public float pressScale    = 0.94f;
    public float tweenTime    = 0.12f;
    [Tooltip("Ampiezza del glow pulsante in hover.")]
    public float pulseAmount  = 0.18f;
    public float pulseSpeed   = 3.0f;

    [Header("Riferimenti (opzionali: auto-trovati)")]
    public Image        background;   // sfondo/fill del bottone
    public Graphic      glowGraphic;  // bordo o alone luminoso opzionale
    public TMP_Text     label;        // testo TMP (auto dai figli)

    Vector3   m_BaseScale;
    Coroutine m_Scale;
    bool      m_Hover;
    bool      m_Pressed;
    bool      m_Selected;

    /// <summary>Il bottone e attualmente selezionato?</summary>
    public bool IsSelected => m_Selected;

    /// <summary>Imposta lo stato "selezionato": resta acceso in teal anche senza hover.</summary>
    public void SetSelected(bool on)
    {
        m_Selected = on;
        ApplyBg();
        TweenScale(m_BaseScale * (m_Hover ? hoverScale : (on ? selectedScale : 1f)));
    }

    void Reset()  => AutoWire();
    void Awake()
    {
        AutoWire();
        m_BaseScale = transform.localScale;
        ApplyState(instant: true);
    }

    void AutoWire()
    {
        if (background == null) background = GetComponent<Image>();
        if (label == null)      label = GetComponentInChildren<TMP_Text>(true);
    }

    void Update()
    {
        // "Acceso" quando il puntatore e sopra OPPURE quando e selezionato.
        bool active = m_Hover || m_Selected;

        float pulse = active
            ? 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount
            : 1f;

        Color baseCol = m_Selected ? selectedColor : themeColor;
        float glow = (active ? glowHover : glowIdle) * pulse;
        Color c = baseCol * glow;
        c.a = 1f;

        if (label != null)       label.color = c;
        if (glowGraphic != null)
        {
            Color g = baseCol; g.a = (active ? glowHover : glowIdle) * pulse;
            glowGraphic.color = g;
        }
    }

    // ── Pointer events ────────────────────────────────
    public void OnPointerEnter(PointerEventData e)
    {
        m_Hover = true;
        AudioManager.Instance?.PlayCharacterHover();
        TweenScale(m_BaseScale * hoverScale);
        ApplyBg();
    }

    public void OnPointerExit(PointerEventData e)
    {
        m_Hover   = false;
        m_Pressed = false;
        TweenScale(m_BaseScale * (m_Selected ? selectedScale : 1f));
        ApplyBg();
    }

    public void OnPointerDown(PointerEventData e)
    {
        m_Pressed = true;
        AudioManager.Instance?.PlayButtonClick();
        TweenScale(m_BaseScale * pressScale);
    }

    public void OnPointerUp(PointerEventData e)
    {
        m_Pressed = false;
        TweenScale(m_BaseScale * (m_Hover ? hoverScale : (m_Selected ? selectedScale : 1f)));
    }

    // ── Helpers ───────────────────────────────────────
    void ApplyBg()
    {
        if (background == null) return;
        Color b = m_Selected ? selectedColor : themeColor;
        b.a = m_Selected ? bgSelectedAlpha : (m_Hover ? bgHoverAlpha : bgIdleAlpha);
        background.color = b;
    }

    void ApplyState(bool instant)
    {
        ApplyBg();
        if (label != null)
        {
            Color c = themeColor * glowIdle; c.a = 1f;
            label.color = c;
        }
    }

    void TweenScale(Vector3 target)
    {
        if (!gameObject.activeInHierarchy) { transform.localScale = target; return; }
        if (m_Scale != null) StopCoroutine(m_Scale);
        m_Scale = StartCoroutine(ScaleRoutine(target));
    }

    IEnumerator ScaleRoutine(Vector3 target)
    {
        Vector3 start = transform.localScale;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, tweenTime);
            transform.localScale = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
            yield return null;
        }
        transform.localScale = target;
    }
}
