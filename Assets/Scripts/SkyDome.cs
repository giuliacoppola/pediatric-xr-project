using UnityEngine;
using System.Collections;

/// <summary>
/// Cielo a cupola procedurale: un gradiente atmosferico (azzurro chiaro all'orizzonte ->
/// blu intenso in alto) che durante il lancio si dissolve in uno spazio nero stellato.
/// Molto piu bello di uno sfondo a tinta unita, e non serve importare nulla.
///
/// Come funziona: due cupole (sfere) intorno al giocatore. Quella dell'atmosfera e
/// opaca; quella dello spazio (con le stelle) e trasparente e compare durante la salita.
///
/// Uso: GameObject vuoto ("Cielo") messo dove sta il giocatore (di solito 0,0,0) ->
/// Add Component -> Sky Dome. La transizione parte col lancio (MissionController).
/// </summary>
[DisallowMultipleComponent]
public class SkyDome : MonoBehaviour
{
    [Header("Dimensione cupola")]
    public float radius = 120f;

    [Header("Atmosfera (gradiente cielo)")]
    public Color skyHorizon = new Color(0.60f, 0.80f, 1.00f);
    public Color skyZenith  = new Color(0.13f, 0.38f, 0.85f);

    [Header("Spazio")]
    public Color spaceHorizon = new Color(0.03f, 0.04f, 0.10f);
    public Color spaceZenith  = new Color(0.00f, 0.00f, 0.02f);
    [Range(0f, 1f)] public float starDensity = 0.03f;

    [Header("Transizione")]
    public float transitionSeconds = 5f;

    Material m_SpaceMat;

    void Awake()
    {
        BuildDome("SkyDome_Atmosphere", GradientTexture(skyHorizon, skyZenith, false),
                  radius, 1f, false);
        var space = BuildDome("SkyDome_Space", GradientTexture(spaceHorizon, spaceZenith, true),
                  radius * 0.98f, 0f, true);
        m_SpaceMat = space.GetComponent<Renderer>().material;
    }

    /// <summary>Transizione atmosfera -> spazio stellato (chiamata al decollo).</summary>
    public void BeginAscent()
    {
        StopAllCoroutines();
        StartCoroutine(FadeToSpace());
    }

    IEnumerator FadeToSpace()
    {
        float t = 0f;
        while (t < transitionSeconds)
        {
            t += Time.deltaTime;
            SetSpaceAlpha(Mathf.SmoothStep(0f, 1f, t / transitionSeconds));
            yield return null;
        }
        SetSpaceAlpha(1f);
    }

    void SetSpaceAlpha(float a)
    {
        if (m_SpaceMat == null) return;
        var c = m_SpaceMat.color; c.a = a; m_SpaceMat.color = c;
    }

    GameObject BuildDome(string name, Texture2D tex, float r, float alpha, bool transparent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * (r * 2f); // sfera unitaria: diametro 1

        var rend = go.GetComponent<Renderer>();
        // Sprites/Default: unlit, a doppia faccia (si vede da dentro), supporta alpha
        var sh = Shader.Find("Sprites/Default");
        var m = new Material(sh) { mainTexture = tex, color = new Color(1f, 1f, 1f, alpha) };
        m.renderQueue = transparent ? 3000 : 1000; // atmosfera come sfondo, spazio sopra
        rend.material = m;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        return go;
    }

    // Gradiente verticale (V della sfera = dal basso verso l'alto). Con stelle se richiesto.
    Texture2D GradientTexture(Color horizon, Color zenith, bool withStars)
    {
        int w = withStars ? 512 : 8;
        int h = 256;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float v = (float)y / (h - 1);
            // orizzonte al centro piu luminoso, zenit in alto
            float k = Mathf.Abs(v - 0.5f) * 2f;      // 0 all'orizzonte, 1 ai poli
            Color col = Color.Lerp(horizon, zenith, k);
            for (int x = 0; x < w; x++)
                px[y * w + x] = col;
        }
        if (withStars)
        {
            var rnd = new System.Random(12345);
            int count = Mathf.RoundToInt(w * h * starDensity * 0.05f);
            for (int i = 0; i < count; i++)
            {
                int x = rnd.Next(0, w);
                int y = rnd.Next(h / 2, h); // stelle nella meta alta (cielo)
                float b = 0.5f + (float)rnd.NextDouble() * 0.5f;
                px[y * w + x] = new Color(b, b, b * 1.05f, 1f);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }
}
