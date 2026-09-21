using UnityEngine;
using TMPro;

/// <summary>
/// Titolo olografico con bagliore pulsante.
/// Aggiungilo a un oggetto con TextMeshPro (o TextMeshProUGUI), idealmente con
/// il font Orbitron e un material che supporta il glow (es. Orbitron_Glow).
///
/// Anima:
/// - il glow del material TMP (_GlowPower) se disponibile;
/// - in alternativa/aggiunta un leggero "respiro" di luminosita e scala.
/// </summary>
[DisallowMultipleComponent]
public class HoloTitle : MonoBehaviour
{
    [Header("Colore")]
    public Color themeColor = new Color(0f, 0.88f, 0.82f);

    [Header("Pulsazione")]
    [Tooltip("Velocita della pulsazione del bagliore.")]
    public float pulseSpeed = 1.8f;

    [Tooltip("Glow minimo e massimo (usato se il material ha _GlowPower).")]
    public float glowMin = 0.2f;
    public float glowMax = 0.9f;

    [Header("Respiro luminosita")]
    [Range(0f, 1f)] public float brightnessMin = 0.75f;
    [Range(0f, 1.5f)] public float brightnessMax = 1.15f;

    [Header("Respiro scala (opzionale)")]
    public bool  breatheScale = true;
    public float scaleAmount  = 0.02f;

    TMP_Text  m_Text;
    Material  m_Mat;
    bool      m_HasGlow;
    Vector3   m_BaseScale;

    static readonly int GlowPowerID = Shader.PropertyToID("_GlowPower");

    void Awake()
    {
        m_Text = GetComponent<TMP_Text>();
        m_BaseScale = transform.localScale;

        if (m_Text != null)
        {
            // fontMaterial crea un'istanza per-oggetto: sicuro da animare.
            m_Mat = m_Text.fontMaterial;
            m_HasGlow = m_Mat != null && m_Mat.HasProperty(GlowPowerID);
        }
    }

    void Update()
    {
        float s = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f; // 0..1

        if (m_HasGlow)
            m_Mat.SetFloat(GlowPowerID, Mathf.Lerp(glowMin, glowMax, s));

        if (m_Text != null)
        {
            float b = Mathf.Lerp(brightnessMin, brightnessMax, s);
            Color c = themeColor * b; c.a = 1f;
            m_Text.color = c;
        }

        if (breatheScale)
            transform.localScale = m_BaseScale * (1f + Mathf.Lerp(-scaleAmount, scaleAmount, s));
    }
}
