using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Dissolvenza in nero tra le scene, e in entrata all'avvio di ogni scena.
/// COMPLETAMENTE AUTOMATICO: si crea da solo, non serve aggiungerlo a nessun oggetto.
/// GameManager.LoadScene lo usa in automatico per sfumare prima di cambiare scena.
///
/// In VR il pannello nero e agganciato alla telecamera cosi copre entrambi gli occhi.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    public float fadeDuration = 0.6f;
    public Color fadeColor = Color.black;

    Renderer m_Quad;
    Material m_Mat;
    float    m_Alpha = 1f;

    // Si crea da solo all'avvio del gioco, prima ancora della prima scena
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("ScreenFader");
        go.AddComponent<ScreenFader>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildQuad();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void BuildQuad()
    {
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        var col = quad.GetComponent<Collider>();
        if (col != null) Destroy(col);
        quad.name = "FadeQuad";
        quad.transform.SetParent(transform, false);

        m_Quad = quad.GetComponent<Renderer>();
        // Sprites/Default: supporta trasparenza ed e a doppia faccia (Cull Off)
        var sh = Shader.Find("Sprites/Default");
        m_Mat = new Material(sh) { renderQueue = 5000 };
        m_Quad.material = m_Mat;
        SetAlpha(1f); // parte nero, poi dissolvenza in entrata
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        AttachToCamera();
        SetAlpha(1f);
        StartCoroutine(Fade(1f, 0f)); // entrata: dal nero alla scena
    }

    void AttachToCamera()
    {
        var cam = Camera.main;
        if (cam == null || m_Quad == null) return;
        var t = m_Quad.transform;
        t.SetParent(cam.transform, false);
        t.localPosition = new Vector3(0f, 0f, 0.3f);
        t.localRotation = Quaternion.identity;
        t.localScale    = new Vector3(3f, 3f, 1f); // copre abbondante il campo visivo
    }

    /// <summary>Sfuma in nero e poi carica la scena.</summary>
    public void FadeAndLoad(string sceneName)
    {
        StartCoroutine(FadeOutAndLoad(sceneName));
    }

    IEnumerator FadeOutAndLoad(string sceneName)
    {
        AttachToCamera();
        yield return Fade(m_Alpha, 1f);
        SceneManager.LoadScene(sceneName);
    }

    IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(from, to, t / fadeDuration));
            yield return null;
        }
        SetAlpha(to);
    }

    void SetAlpha(float a)
    {
        m_Alpha = a;
        if (m_Mat != null)
        {
            var c = fadeColor; c.a = a;
            m_Mat.color = c;
        }
        if (m_Quad != null) m_Quad.enabled = a > 0.001f;
    }
}
