using UnityEngine;
using System.Collections;
using TMPro;

/// <summary>
/// Barra di rifornimento 3D "figa" del razzo (il pieno di energia), pensata per il
/// momento del prelievo. L'operatore la fa salire lentamente dal tablet, poi la riempie.
///
/// Effetti (tutti procedurali, nessun asset da importare):
///  - riempimento con colore ARANCIONE -> VERDE;
///  - cornice teal luminosa;
///  - bordo di carica in cima che pulsa;
///  - particelle di energia che salgono dentro il serbatoio;
///  - scoppio/flash verde quando e pieno.
///
/// Uso: GameObject vuoto ("Barra Energia") -> Add Component -> Fuel Gauge, posizionalo
/// accanto al razzo. I comandi arrivano dal tablet (o tasti F / G nell'editor).
/// </summary>
[DisallowMultipleComponent]
public class FuelGauge : MonoBehaviour
{
    [Header("Dimensioni (metri)")]
    public float width  = 0.30f;
    public float height = 1.30f;
    public float depth  = 0.12f;

    [Header("Riempimento lento (durante il prelievo)")]
    public float slowFillSeconds = 40f;
    [Range(0f, 1f)] public float slowFillTarget = 0.9f;

    [Header("Colori")]
    public Color emptyColor = new Color(1f, 0.5f, 0.05f);   // arancione
    public Color fullColor  = new Color(0.25f, 1f, 0.35f);  // verde
    public Color frameColor = new Color(0f, 0.88f, 0.82f);  // teal

    [Header("Scritta sopra la barra (di solito spenta: il messaggio sta sopra il razzo)")]
    public bool  showLabel  = false;
    [Tooltip("Se il testo e troppo grande/piccolo, cambia questo valore.")]
    public float labelScale = 0.07f;
    [TextArea] public string loadingMessage = "CARICAMENTO SERBATOIO\nNon muoverti, sei bravissimo!";
    [TextArea] public string fullMessage    = "SERBATOIO PIENO!\nPronti al lancio!";

    TMP_Text m_Label;

    Transform      m_Fill, m_Lead;
    Renderer       m_FillR, m_LeadR;
    ParticleSystem m_Ps;
    float          m_Fill01;
    Coroutine      m_Co;
    bool           m_Flashing;

    void Awake()
    {
        Build();
        SetFill(0f);
    }

    void Update()
    {
        // Bordo di carica pulsante in cima al liquido
        if (m_LeadR != null && m_Fill01 > 0.001f && m_Fill01 < 0.999f)
        {
            float p = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 5f);
            Color c = Color.Lerp(CurrentColor(), Color.white, 0.4f) * p;
            c.a = 1f;
            m_LeadR.material.color = c;
            m_LeadR.enabled = true;
        }
        else if (m_LeadR != null && !m_Flashing)
        {
            m_LeadR.enabled = false;
        }
    }

    Color CurrentColor() => Color.Lerp(emptyColor, fullColor, m_Fill01);

    // ── Costruzione ───────────────────────────────────
    void Build()
    {
        // Sfondo serbatoio (semi-trasparente)
        var bg = MakeBox("Tank_BG", new Color(0.05f, 0.07f, 0.11f, 0.5f));
        bg.transform.SetParent(transform, false);
        bg.transform.localScale    = new Vector3(width, height, depth);
        bg.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);

        // Cornice teal luminosa (4 bordi)
        float t = width * 0.10f;
        MakeEdge(new Vector3(0f, height, 0f),        new Vector3(width + t, t, depth * 1.4f)); // top
        MakeEdge(new Vector3(0f, 0f, 0f),            new Vector3(width + t, t, depth * 1.4f)); // bottom
        MakeEdge(new Vector3(-width * 0.5f, height * 0.5f, 0f), new Vector3(t, height + t, depth * 1.4f)); // left
        MakeEdge(new Vector3( width * 0.5f, height * 0.5f, 0f), new Vector3(t, height + t, depth * 1.4f)); // right

        // Riempimento (cresce dal basso)
        var fill = MakeBox("Tank_Fill", emptyColor);
        fill.transform.SetParent(transform, false);
        m_Fill  = fill.transform;
        m_FillR = fill.GetComponent<Renderer>();

        // Bordo di carica (linea luminosa in cima al liquido)
        var lead = MakeBox("Tank_Lead", Color.white);
        lead.transform.SetParent(transform, false);
        m_Lead  = lead.transform;
        m_LeadR = lead.GetComponent<Renderer>();

        BuildParticles();
        if (showLabel) BuildLabel();
    }

    void BuildLabel()
    {
        var go = new GameObject("Label");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, height + 0.5f, 0f);
        go.transform.localScale    = Vector3.one * labelScale;

        m_Label = go.AddComponent<TextMeshPro>();
        m_Label.alignment          = TextAlignmentOptions.Center;
        m_Label.color              = Color.white;
        m_Label.enableWordWrapping = true;
        m_Label.enableAutoSizing   = true;   // il testo riempie il riquadro: sempre grande
        m_Label.fontSizeMin        = 0.1f;
        m_Label.fontSizeMax        = 40f;
        m_Label.rectTransform.sizeDelta = new Vector2(40f, 24f);
        m_Label.text = "";
    }

    void MakeEdge(Vector3 localPos, Vector3 scale)
    {
        var e = MakeBox("Frame", frameColor);
        e.transform.SetParent(transform, false);
        e.transform.localScale    = scale;
        e.transform.localPosition = localPos;
    }

    void BuildParticles()
    {
        var go = new GameObject("EnergyParticles");
        go.transform.SetParent(transform, false);
        m_Ps = go.AddComponent<ParticleSystem>();

        var main = m_Ps.main;
        main.loop = true;
        main.startLifetime = 1.2f;
        main.startSpeed = 0.3f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 40;
        main.startColor = emptyColor;

        var em = m_Ps.emission; em.rateOverTime = 14f;
        var sh = m_Ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(width * 0.55f, 0.01f, depth * 0.4f);
        sh.position = new Vector3(0f, 0.02f, 0f);

        var vel = m_Ps.velocityOverLifetime;
        vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = 0f; vel.y = 0.4f; vel.z = 0f;

        var col = m_Ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[]{ new GradientColorKey(Color.white,0f), new GradientColorKey(Color.white,1f) },
                  new[]{ new GradientAlphaKey(0f,0f), new GradientAlphaKey(1f,0.3f), new GradientAlphaKey(0f,1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        var s = Shader.Find("Sprites/Default");
        var mat = new Material(s) { mainTexture = SoftCircle(32) };
        r.material = mat;
        m_Ps.Play();
    }

    // ── Stato ─────────────────────────────────────────
    void SetFill(float v)
    {
        m_Fill01 = Mathf.Clamp01(v);
        float w = width * 0.82f, d = depth * 1.25f;
        float h = height * m_Fill01;

        m_Fill.localScale    = new Vector3(w, Mathf.Max(0.0001f, h), d);
        m_Fill.localPosition = new Vector3(0f, h * 0.5f, 0f);
        m_FillR.material.color = CurrentColor();
        m_FillR.enabled = m_Fill01 > 0.001f;

        // bordo di carica in cima al liquido
        m_Lead.localScale    = new Vector3(w, height * 0.03f, d * 1.05f);
        m_Lead.localPosition = new Vector3(0f, h, 0f);

        // le particelle salgono solo dentro il liquido e prendono il colore attuale
        if (m_Ps != null)
        {
            var main = m_Ps.main; main.startColor = CurrentColor();
            var sh = m_Ps.shape;
            sh.scale    = new Vector3(width * 0.55f, Mathf.Max(0.01f, h), depth * 0.4f);
            sh.position = new Vector3(0f, h * 0.5f, 0f);
            var em = m_Ps.emission; em.rateOverTime = m_Fill01 > 0.001f ? 14f : 0f;
        }

        // Scritta sopra la barra
        if (m_Label != null)
        {
            if (m_Fill01 <= 0.001f)      m_Label.text = "";
            else if (m_Fill01 >= 0.999f) m_Label.text = fullMessage;
            else m_Label.text = $"{loadingMessage}\n<b>{Mathf.RoundToInt(m_Fill01 * 100)}%</b>";
        }
    }

    public bool IsFull => m_Fill01 >= 0.999f;

    // ── Comandi ───────────────────────────────────────
    public void StartFilling()
    {
        if (m_Co != null) StopCoroutine(m_Co);
        AudioManager.Instance?.PlayFuelLoading();
        m_Co = StartCoroutine(SlowFill());
    }

    public void CompleteFilling()
    {
        if (m_Co != null) StopCoroutine(m_Co);
        m_Co = StartCoroutine(FastFull());
    }

    IEnumerator SlowFill()
    {
        float start = m_Fill01, t = 0f;
        while (t < slowFillSeconds && m_Fill01 < slowFillTarget)
        {
            t += Time.deltaTime;
            SetFill(Mathf.Lerp(start, slowFillTarget, t / slowFillSeconds));
            yield return null;
        }
    }

    IEnumerator FastFull()
    {
        AudioManager.Instance?.PlayFuelLoading();
        float start = m_Fill01, t = 0f, dur = 1.1f;
        while (t < dur)
        {
            t += Time.deltaTime;
            SetFill(Mathf.Lerp(start, 1f, t / dur));
            yield return null;
        }
        SetFill(1f);
        AudioManager.Instance?.PlayEngineStable();
        StartCoroutine(FullFlash());
        if (m_Ps != null) m_Ps.Emit(40); // scoppio di energia
    }

    // Flash verde quando pieno
    IEnumerator FullFlash()
    {
        m_Flashing = true;
        for (int i = 0; i < 3; i++)
        {
            m_LeadR.material.color = Color.white;
            m_LeadR.enabled = true;
            m_LeadR.transform.localScale = new Vector3(width * 1.1f, height * 0.06f, depth * 1.4f);
            yield return new WaitForSeconds(0.12f);
            m_LeadR.enabled = false;
            yield return new WaitForSeconds(0.12f);
        }
        m_Flashing = false;
    }

    // ── Helper ────────────────────────────────────────
    GameObject MakeBox(string name, Color c)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
        var r = go.GetComponent<Renderer>();
        var sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
        var m = new Material(sh) { color = c };
        if (c.a < 0.999f) m.renderQueue = 3000;
        r.material = m;
        return go;
    }

    Texture2D SoftCircle(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float c = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = (x - c) / c, dy = (y - c) / c;
            float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
        }
        tex.Apply();
        return tex;
    }
}
